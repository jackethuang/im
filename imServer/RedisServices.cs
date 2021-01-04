using FreeRedis;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace imServer
{
    public class RedisServices : IRedisServices
    {
        private string redisConnection = string.Empty;
        public RedisServices(IConfiguration configuration)
        {
            redisConnection = configuration["ImServerOption:RedisClient"];
        }
        public RedisClient GetRedisService()
        {
            return new RedisClient(redisConnection);
        }
    }
}
