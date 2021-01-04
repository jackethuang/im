using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imCommon;

namespace imCore
{
    public class RabbitMQHelper : IRabbitMQHelper
    {
        /// <summary>
        /// Logger
        /// </summary>
        ILogger _logger;

        /// <summary>
        /// MQ设置
        /// </summary>
        MQSetting _mqSetting;

        public RabbitMQHelper(ILoggerFactory loggerFactory, IOptions<MQSetting> mqSetting)
        {
            _logger = loggerFactory.CreateLogger(nameof(RabbitMQHelper));
            _mqSetting = mqSetting.Value;
        }

        public IConnection CreateMQConnection()
        {
            var factory = CrateFactory();
            factory.AutomaticRecoveryEnabled = true;//自动重连
            var connection = factory.CreateConnection();
            //connection.AutoClose = false;
            return connection;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="connection">消息队列连接对象</param>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="queueName">队列名称</param>
        /// <param name="durable">是否持久化</param>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        public void SendMsg(IConnection connection, string exchangeName, string exchangeType, string queueName, string msg, bool durable = true)
        {
            try
            {
                using (var channel = connection.CreateModel())//建立通讯信道
                {

                    //ExchangeType.Direct
                    channel.ExchangeDeclare(exchangeName, exchangeType, durable: durable, autoDelete: false, arguments: null);
                    channel.QueueDeclare(queueName, durable: true, autoDelete: false, exclusive: false, arguments: null);
                    channel.QueueBind(queueName, exchangeName, routingKey: queueName);

                    //channel.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: durable, autoDelete: false, null); //"fanout"

                    // 参数从前面开始分别意思为：队列名称，是否持久化，独占的队列，不使用时是否自动删除，其他参数
                    //channel.QueueDeclare(queueName, durable, false, false, null);

                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2;//1表示不持久,2.表示持久化
                    properties.Persistent = true;

                    if (!durable)
                        properties = null;

                    var body = Encoding.UTF8.GetBytes(msg);

                    //channel.BasicPublish(exchange: exchangeName, routingKey: "", basicProperties: properties, body: body);
                    channel.BasicPublish(exchange: exchangeName, routingKey: queueName, basicProperties: properties, body: body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("RabbitMQHelper=>SendMsg Error", ex.Message);
            }
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <returns></returns>
        private ConnectionFactory CrateFactory()
        {
            var connectionfactory = new ConnectionFactory
            {
                HostName = _mqSetting.HostName,
                UserName = _mqSetting.UserName,
                Password = _mqSetting.Password
            };
            return connectionfactory;
        }
    }
}
