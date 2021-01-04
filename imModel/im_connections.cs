using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_connections")]
    public class im_connections
    {
        [SugarColumn(IsPrimaryKey =true,IsIdentity =true)]
        public int id { get; set; }

        /// <summary>
        /// 会话记录编码
        /// </summary>
        public string connectionBatchCode { get; set; }
        /// <summary>
        /// 继承自哪个会话记录
        /// </summary>
        public string parentBatchCode { get; set; } = string.Empty;


        public string clientId { get; set; }


        public string relationClientId { get; set; }

        /// <summary>
        /// 连接状态 -1 0：接待中  1：已转移 2：已结束
        /// </summary>
        public int connectionStatus { get; set; }


        public string lastMsg { get; set; }


        public DateTime lastTime { get; set; }


        public int govId { get; set; }


        public int sourceId { get; set; }


        public DateTime createTime { get; set; }


        public bool isActive { get; set; }


        public int isFocus { get; set; } = 0;


        public DateTime focusTime { get; set; } = DateTime.Now;

        public string memo { get; set; } = string.Empty;

        public DateTime completeData { get; set; }

    }

    /// <summary>
    /// 对话状态
    /// </summary>
    public enum IMConnectionStatus
    { 
       待接待 = -1,
       正常 =0,
       已转移 =1,
       已结束 =2,
    }
}
