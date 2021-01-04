using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace imCommon
{
    public class MQSetting
    {
        /// <summary>
        /// MQ服务器地址
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }
    }
}