using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_autoresponse")]
    public class im_autoresponse
    {
        public int id { get; set; }

        public int govId { get; set; }

        public string question { get; set; }

        public string keywords { get; set; }

        public string response { get; set; }

        //回复类型 0：默认  1:引导页问题（初次打开会话框）
        public int responseType { get; set; } = 0;

        /// <summary>
        /// 回复状态： 1 启用 0 停用
        /// </summary>
        public int responseStatus { get; set; } = 1;

        public int sortNum { get; set; }
    }
}
