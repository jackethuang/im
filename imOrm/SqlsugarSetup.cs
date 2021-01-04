using imCommon;
using imOrm.Option;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using SqlSugar.IOC;
using System;
using System.Threading.Tasks;

namespace imOrm
{
    public static class SqlsugarSetup
    {
        public static void InitSqlSugar(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var dataBaseOption = ServiceLocator.Instance.GetService<DatabaseOption>();
            
            services.AddSqlSugar(new IocConfig()
            {
                ConfigId = dataBaseOption.DBName.ToLower(),
                ConnectionString = dataBaseOption.ConnectionString,
                //DbType = IocDbType.SqlServer,
                DbType = (IocDbType)dataBaseOption.DBType,
                IsAutoCloseConnection = true
            });
            services.ConfigurationSugar(db =>
            {
                db.GetConnection(dataBaseOption.DBName.ToLower()).Aop.OnLogExecuting = (sql, p) =>
                {
                    Parallel.For(0, 1, e => {
                        //MiniProfiler.Current.CustomTiming("SQL：", GetParas(p) + "【SQL语句】：" + sql);
                        //LogLock.OutSql2Log("SqlLog", new string[] { GetParas(p), "【SQL语句】：" + sql });
                    });
                };
            });
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="pars"></param>
        /// <returns></returns>
        private static string GetParas(SugarParameter[] pars)
        {
            string key = "【SQL参数】：";
            foreach (var param in pars)
            {
                key += $"{param.ParameterName}:{param.Value}\n";
            }

            return key;
        }
    }
}
