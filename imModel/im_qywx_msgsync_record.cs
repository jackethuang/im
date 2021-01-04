using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    /// <summary>
    /// 企业微信消息同步记录
    /// </summary>
    [SugarTable("im_qywx_msgsync_record")]
    public class im_qywx_msgsync_record
    {
        [SugarColumn(IsPrimaryKey =true,IsIdentity =true)]
        public int id { get; set; }

        public DateTime syncTime { get; set; }

        public string syncBatchNo { get; set; }

        public string syncCursor { get; set; }

        public int errorCode { get; set; }

        public string errMsg { get; set; }

        public int hasMore { get; set; }

        public int pageIndex { get; set; }
    }
}
