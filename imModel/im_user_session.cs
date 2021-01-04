using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_user_session")]
    public class im_user_session
    {
        [SugarColumn(IsPrimaryKey =true,IsIdentity =true)]
        public int id { get; set; }

        public string clientId { get; set; }

        public int userId { get; set; }

        public string fromUserName { get; set; }

        public int connId { get; set; }

        public int govId { get; set; }

        public int sourceId { get; set; }

        public DateTime createTime { get; set; }

        public DateTime lastLoginTime { get; set; }
    }
}
