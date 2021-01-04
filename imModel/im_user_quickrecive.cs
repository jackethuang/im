using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_user_quickrecive")]
    public class im_user_quickrecive
    {
        [SugarColumn(IsIdentity =true,IsPrimaryKey =true)]
        public int id { get; set; }

        public string clientId { get; set; }

        public string message { get; set; }

        public string title { get; set; }

        public DateTime createTime { get; set; }

        public int sort { get; set; } = 99;
    }
}
