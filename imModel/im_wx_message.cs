using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_wx_message")]
    public class im_wx_message
    {
        [SugarColumn(IsPrimaryKey = true,IsIdentity =true)]
        public int Id { get; set; }

        public string ToUserName { get; set; }

        public string FromUserName { get; set; }

        public string CreateTime { get; set; }

        public string MsgType { get; set; }

        public string Event { get; set; }

        public string SessionFrom { get; set; }

        public string MsgId { get; set; }

        public string Content { get; set; }

        public string PicUrl { get; set; }

        public string MediaId { get; set; }

        public bool IsError { get; set; }

        public string ErrorMessage { get; set; }
    }
}
