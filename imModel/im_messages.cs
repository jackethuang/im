using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;
namespace imModel
{
    [SugarTable("im_messages")]
    public class im_messages
    {
        [SugarColumn(IsPrimaryKey =true,IsIdentity =true)]
        public int id { get; set; }

        public int connectionId { get; set; }

        /// <summary>
        /// 会话编号
        /// </summary>
        public string connectionBatchCode { get; set; }

        public string msgType { get; set; }

        public string msgEvent { get; set; }

        public string msgFile { get; set; }

        public string msgContent { get; set; }

        public string fromClientId { get; set; }

        public string toClientId { get; set; }

        public DateTime sendTime { get; set; }

        public int msgStatus { get; set; }

        public bool isMe { get; set; }

        public string wxMsgID { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string errMsg { get; set; }
    }

    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageTypeEnum
    { 
       文本,
       图片,
       视频,
       转移记录
    }

    /// <summary>
    /// 消息状态枚举
    /// </summary>
    public enum MessageStatusEnum
    { 
       待发送,
       发送中,
       已发送,
       已接收,
       发送失败,
    }
}
