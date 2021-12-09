using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using imCommon;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace imServer
{
    public class QyIMMessageConsumer
    {
        /// <summary>
        /// Logger
        /// </summary>
        IConfiguration _configuration;
        IRedisServices _redis;

        public QyIMMessageConsumer(IConfiguration configuration, IRedisServices redis)
        {
            _redis = redis;
            _configuration = configuration;
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

                Log.Information(res);
            }
            catch (Exception ex)
            {
                isSuccess = false;
                Log.Error(res, "发送失败", ex.Message);
            }
            return isSuccess;
        }

        private bool ProduceMessage(string msgCode, ref string errMsg)
        {
            var isSuccess = true;
            try
            {
                var url = _configuration.GetValue<String>("ApiServicesUrl") + msgCode;
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
                Log.Error($"QyIMMessageConsumer-ProduceMessage-{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.FFF")}:{ex.Message}");
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