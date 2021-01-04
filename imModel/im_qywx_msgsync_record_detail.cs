using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_qywx_msgsync_record_detail")]
    public class im_qywx_msgsync_record_detail
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id { get; set; }

        public string msgid { get; set; }


        public string open_kfid { get; set; }


        public string external_userid { get; set; }


        public string send_time { get; set; }


        public int origin { get; set; }


        public string msgtype { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string msgContent { get; set; }


        public string mediaId { get; set; }

        /// <summary>
        /// 消息是否已经处理 0：未处理  1:处理成功  2：处理失败
        /// </summary>
        public int isProcessing { get; set; } = 0;

        public DateTime processingTime { get; set; }

        public int errCode { get; set; } = 0;

        public string errMsg { get; set; } = "ok";

        public int processingCount { get; set; }
        /// <summary>
        /// 所属同步批次
        /// </summary>
        public string syscBatchNo { get; set; }
    }

    /// <summary>
    /// 消息处理状态枚举
    /// </summary>
    public enum MessageProcessingEnum
    {
        未处理 = 0,
        处理成功 = 1,
        处理失败 = 2,
        忽略不处理 = 3,
        未进入队列处理 = 99
    }
}
