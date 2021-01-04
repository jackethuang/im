using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace imCommon
{
    public class QueueSetting
    {
        /// <summary>
        /// 交换机名称
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// 持久化队列
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// 独占队列
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// 自动删除
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// 参数
        /// </summary>
        public IDictionary<string, object> Arguments = null;
    }
}
