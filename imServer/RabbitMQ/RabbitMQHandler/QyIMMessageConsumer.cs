using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using imModel;
using SqlSugar.IOC;
using imCommon;
using Microsoft.Extensions.Configuration;

namespace imServer
{
    public class QyIMMessageConsumer
    {
        /// <summary>
        /// Logger
        /// </summary>
        ILogger _logger;
        IConfiguration _configuration;
        IRedisServices _redis;

        public QyIMMessageConsumer(ILoggerFactory loggerFactory, IConfiguration configuration, IRedisServices redis)
        {
            _redis = redis;
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(nameof(QyIMMessageConsumer));
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message"></param>
        public bool HandleMessage(object message, string queenName)
        {
            bool isSuccess = true;
            var res = string.Empty;
            try
            {
                var msgCode = (message as string).Replace("\"", "");
                if (!ProduceMessage(msgCode, ref res))
                    throw new Exception(res);

                _logger.LogInformation(res);
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _logger.LogError(res, "发送失败", ex.Message);
            }
            return isSuccess;
        }

        private bool ProduceMessage(string msgCode,ref string errMsg)
        {
            var isSuccess = true;
            try
            {
                var url = _configuration.GetValue<String>("ApiServicesUrl")+msgCode;
                var response = HttpMethods.HttpPost(url);
                var resObj = JsonConvert.DeserializeObject<WebApiResponse>(response);
                if (resObj.code != 0)
                    throw new Exception(resObj.msg);
                isSuccess = resObj.data;
                errMsg = resObj.msg;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                isSuccess = false;
            }
            return isSuccess;
        }
    }

    internal class WebApiResponse
    { 
        public int code { get; set; }

        public bool data { get; set; }

        public string msg { get; set; }
    }
}