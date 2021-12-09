using imCommon;
using imCore;
using imOrm.Option;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using imOrm;

namespace imServer
{

    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MQSetting>(Configuration.GetSection("MqSetting"));
            services.Configure<DatabaseOption>(Configuration.GetSection("DatabaseOption"));
            services.Configure<List<ConsumerSetting>>(Configuration.GetSection("MqConsumerSetting"));
            //services.AddLogging((builder) => builder
            //    .AddConfiguration(Configuration.GetSection("Logging"))
            //    .AddConsole());
            //services.InitSqlSugar();
            // 注入
            services.AddSingleton<IRabbitMQConsumerApplication, RabbitMQConsumerApplication>();
            services.AddSingleton<IRabbitMQHelper, RabbitMQHelper>();
            services.AddSingleton<IRedisServices, RedisServices>();
            // 注入rabbitmq监听
            services.AddSingleton<IRabbitMQConsumerApplication, RabbitMQConsumerApplication>();

        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");
            Console.InputEncoding = Encoding.GetEncoding("GB2312");

            app.UseDeveloperExceptionPage();
            app.UseSerilogRequestLogging();
            //ImHelper.Initialization(new ImClientOptions() { 
            //   Redis = new FreeRedis.RedisClient(Configuration["ImServerOption:RedisClient"]),
            //   Servers = Configuration["ImServerOption:Servers"].Split(";")
            //});
            var redisConnectionString = Configuration["ImServerOption:RedisClient"];
            app.UseImServer(new ImServerOptions
            {
                Redis = new FreeRedis.RedisClient(redisConnectionString),
                Servers = Configuration["ImServerOption:Servers"].Split(";"),
                Server = Configuration["ImServerOption:Server"]
            });

            ServiceLocator.Instance = app.ApplicationServices;

            // 启动MQ消费
            Task.Run(() =>
            {
                app.ApplicationServices.GetService<IRabbitMQConsumerApplication>().Start();
            });
        }
    }
}
