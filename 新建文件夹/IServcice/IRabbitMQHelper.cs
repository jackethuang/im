#if ns20
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace imCore
{
    public interface IRabbitMQHelper
    {
        IConnection CreateMQConnection();
        void SendMsg(IConnection connection, string exchangeName, string exchangeType, string queueName, string msg, bool durable = true);
    }
}
#endif
