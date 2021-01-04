using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace imModel
{
    [SugarTable("im_users")]
    public class im_users
    {
        [SugarColumn(IsIdentity =true,IsPrimaryKey =true)]
        public int id { get; set; }

        public string clientId { get; set; }

        /// <summary>
        /// 客服小程序openId
        /// </summary>
        public string imOpenId { get; set; }

        /// <summary>
        /// 客服公众号OpenId
        /// </summary>
        public string pubOpenId { get; set; }

        /// <summary>
        /// 企业微信客户Id
        /// </summary>
        public string external_userid { get; set; }

        /// <summary>
        /// 绑定的客服Id
        /// </summary>
        public string open_kfid { get; set; }
        /// <summary>
        /// 用户类型 0：普通用户 1：医生
        /// </summary>
        public int userType { get; set; }

        /// <summary>
        /// 是否启用 1：已启用  0：未启用
        /// </summary>
        public int userStatus { get; set; } = 1;

        /// <summary>
        /// 用户Id
        /// </summary>
        public int userId { get; set; }


        public string photo { get; set; }


        public string nickName { get; set; }


        public DateTime createTime { get; set; }

        /// <summary>
        /// 注册来源 0：小程序  1：企业微信
        /// </summary>
        public int registerFrom { get; set; }


    }

    public enum IMUserType
    { 
       普通用户,
       医生
    }
}
