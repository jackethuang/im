using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_user_colleagues")]
    public class im_user_colleagues
    {
        [SugarColumn(IsPrimaryKey =true,IsIdentity =true)]
        public int id { get; set; }

        public string clientId { get; set; }

        /// <summary>
        /// 同事的clientId
        /// </summary>
        public string colleagueClientId { get; set; }

        /// <summary>
        /// 创建日期
        /// </summary>
        public DateTime createTime { get; set; }
    }
}
