﻿using imCommon;
using imCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace imServer
{
    public class RabbitMQConsumerApplication : IRabbitMQConsumerApplication
    {
        /// <summary>
        /// MQ设置
        /// </summary>
        MQSetting _mqSetting;

        /// <summary>
        /// MQ消费者设置
        /// </summary>
        List<ConsumerSetting> _consumerSettings;

        IConfiguration _configuration;
        IRedisServices _redis;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitMQConsumerApplication(
            IOptions<MQSetting> mqSetting,
            IOptions<List<ConsumerSetting>> consumerSettings,
            IConfiguration configuration,
            IRedisServices redis
            )
        {
            _redis = redis;
            _mqSetting = mqSetting.Value;
            _consumerSettings = consumerSettings.Value;
            _configuration = configuration;
        }

        /// <summary>
        /// 启动RabbitMQ消费程序
        /// </summary>
        public void Start()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _mqSetting.HostName,
                UserName = _mqSetting.UserName,
                Password = _mqSetting.Password
            };

            using (var connection = factory.CreateConnection())
            {
                foreach (var consumerSetting in _consumerSettings)
                {
                    for (int i = 0; i < consumerSetting.ConsumerCount; i++)
                    {
                        Task.Run(() =>
                        {
                            using var channel = connection.CreateModel();
                            try
                            {
                                var exchangeName = consumerSetting.QueueSetting.ExchangeName;
                                //channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);
                                var queueName = consumerSetting.QueueSetting.QueueName;

                                channel.QueueDeclare(
                                    queue: consumerSetting.QueueSetting.QueueName,
                                    durable: consumerSetting.QueueSetting.Durable,
                                    exclusive: consumerSetting.QueueSetting.Exclusive,
                                    autoDelete: consumerSetting.QueueSetting.AutoDelete,
                                    arguments: consumerSetting.QueueSetting.Arguments);

                                channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: queueName);

                                channel.BasicQos(0, 1, false);

                                Log.Information($"启动消费端 : {JsonConvert.SerializeObject(consumerSetting)}");

                                var consumer = new EventingBasicConsumer(channel);

                                consumer.Received += (currentConsumer, ea) =>
                                {
                                    try
                                    {
                                        Thread.Sleep(new Random().Next(10, 60));
                                        var body = ea.Body;
                                        var message = Encoding.UTF8.GetString(body.ToArray());
                                        Log.Information($"开始处理消息 ( message: {message},queenName:{queueName}  )");
                                        var assembly = Assembly.Load(consumerSetting.AssemblyName);
                                        var type = assembly.GetType(consumerSetting.ClassName);
                                        var method = type.GetMethod(consumerSetting.MethodName);
                                        using (_redis.GetRedisService().Lock(message.Trim(), 3))
                                        {
                                            var isSuccess = (bool)method.Invoke(Activator.CreateInstance(type, _configuration, _redis), new object[] { message.Trim(), consumerSetting.QueueSetting.QueueName });
                                            if (!isSuccess)
                                                throw new Exception("处理失败");
                                            Log.Information("处理成功");
                                            if (!consumerSetting.AutoAck)
                                            {
                                                channel.BasicAck(ea.DeliveryTag, false);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "消息处理失败");
                                        if (!consumerSetting.AutoAck)
                                        {
                                            channel.BasicReject(ea.DeliveryTag, false); //consumerSetting.Requeue
                                        }
                                    }
                                };

                                channel.BasicConsume(queue: consumerSetting.QueueSetting.QueueName,
                                                    autoAck: consumerSetting.AutoAck,
                                                    consumer: consumer);

                                Log.Information($"消费端启动成功");

                                Thread.Sleep(Timeout.Infinite);
                            }
                            catch (Exception ex)
                            {

                                Log.Information($"消费端启动失败,失败原因：" + ex.Message);
                            }
                        });
                    }
                }

                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
