using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace imServer
{
    public class IMMessageConsumer
    {
        /// <summary>
        /// Logger
        /// </summary>
        ILogger _logger;
        IConfiguration _configuration;
        IRedisServices _redis;

        public IMMessageConsumer(ILoggerFactory loggerFactory, IConfiguration configuration, IRedisServices redis)
        {
            _redis = redis;
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(nameof(IMMessageConsumer));
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message"></param>
        public bool HandleMessage(string message, string queenName)
        {
            bool isSuccess = true;
            try
            {
                var (senderClientId, receiveClientId, content, receipt) = JsonConvert.DeserializeObject<(string senderClientId, string[] receiveClientId, string content, bool receipt)>(message as string);
                var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));
                foreach (var clientId in receiveClientId)
                {
                    if (ImServer._clients.TryGetValue(clientId, out var wslist) == false)
                    {
                        //Console.WriteLine($"websocket{clientId} 离线了，{data.content}" + (data.receipt ? "【需回执】" : ""));
                        if (!string.IsNullOrWhiteSpace(senderClientId) && clientId != senderClientId && receipt)
                            ImHelper.SendMessage(clientId, new[] { senderClientId }, queenName, new
                            {
                                content,
                                receipt = "用户不在线"
                            });
                        continue;
                    }

                    ImServer.ImServerClient[] sockarray = wslist.Values.ToArray();

                    //如果接收消息人是发送者，并且接收者只有1个以下，则不发送
                    //只有接收者为多端时，才转发消息通知其他端
                    if (clientId == senderClientId && sockarray.Length <= 1) continue;

                    foreach (var sh in sockarray)
                        sh.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);

                    if (string.IsNullOrWhiteSpace(senderClientId) && clientId != senderClientId && receipt)
                        ImHelper.SendMessage(clientId, new[] { senderClientId }, queenName, new
                        {
                            content,
                            receipt = "发送成功"
                        });
                    _logger.LogInformation(message);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _logger.LogError(message, "发送失败", ex.Message);
            }
            return isSuccess;
        }
    }
}