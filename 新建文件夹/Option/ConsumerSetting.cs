#if ns20
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace imCore
{
    public class ConsumerSetting
    {
        /// <summary>
        /// 程序集名称
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// 方法名
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 是否自动应答
        /// </summary>
        public bool AutoAck { get; set; } = true;

        /// <summary>
        /// 是否自动应答为 false 时，若发生异常，是否重新回队列。
        /// true：重回队列；false：直接丢弃；
        /// </summary>
        public bool Requeue { get; set; } = false;

        /// <summary>
        /// 消费者数量
        /// </summary>
        public int ConsumerCount { get; set; } = 1;

        /// <summary>
        /// 队列设置
        /// </summary>
        public QueueSetting QueueSetting { get; set; }
    }
}

#endif