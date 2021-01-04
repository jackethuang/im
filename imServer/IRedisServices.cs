using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeRedis;

namespace imServer
{
    public interface IRedisServices
    {
        RedisClient GetRedisService();
    }
}
