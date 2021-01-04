
#if ns20

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class ImServerExtenssions
{
    static bool isUseWebSockets = false;

    /// <summary>
    /// 启用 ImServer 服务端
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseImServer(this IApplicationBuilder app, ImServerOptions options)
    {
        app.Map(options.PathMatch, appcur =>
        {
            var imserv = new ImServer(options);
            if (isUseWebSockets == false)
            {
                try
                {
                    var webSocketOptions = new WebSocketOptions()
                    {
                        KeepAliveInterval = TimeSpan.FromSeconds(10),  //向客户端发送“ping”帧的频率，以确保代理保持连接处于打开状态
                        ReceiveBufferSize = 4 * 1024   //用于接收数据的缓冲区的大小。 只有高级用户才需要对其进行更改，以便根据数据大小调整性能。
                    };
                    isUseWebSockets = true;
                    appcur.UseWebSockets(); // appcur.UseWebSockets(webSocketOptions);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UseWebSockets：{ex.Message}");
                }
            }
            appcur.Use((ctx, next) =>
                imserv.Acceptor(ctx, next));
        });
        return app;
    }
}

/// <summary>
/// im 核心类实现的配置所需
/// </summary>
public class ImServerOptions : ImClientOptions
{
    /// <summary>
    /// 设置服务名称，它应该是 servers 内的一个
    /// </summary>
    public string Server { get; set; }
}

public class ImServer : ImClient
{
    protected string _server { get; set; }

    public ImServer(ImServerOptions options) : base(options)
    {
        _server = options.Server;
    }

    const int BufferSize = 4096;
    public static ConcurrentDictionary<string, ConcurrentDictionary<string, ImServerClient>> _clients = new ConcurrentDictionary<string, ConcurrentDictionary<string, ImServerClient>>();

    public class ImServerClient
    {
        public WebSocket socket;
        public string clientId;

        public ImServerClient(WebSocket socket, string clientId)
        {
            this.socket = socket;
            this.clientId = clientId;
        }
    }

    class MsgTemplate
    {
        public string type { get; set; }

        public int currentSence { get; set; }
        public string clientId { get; set; }

        public string connectionBatchCode { get; set; }
        
    }
    internal async Task Acceptor(HttpContext context, Func<Task> next)
    {
        if (!context.WebSockets.IsWebSocketRequest) return;

        string token = context.Request.Query["token"];

        if (string.IsNullOrEmpty(token)) return;
        var token_value = _redis.Get($"{_redisPrefix}Token{token}");
        if (string.IsNullOrEmpty(token_value))
            throw new Exception("授权错误：用户需通过 ImHelper.PrevConnectServer 获得包含 token 的连接");
        var data = JsonConvert.DeserializeObject<(string clientId, string clientMetaData)>(token_value);

        CancellationToken ct = context.RequestAborted;
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var cli = new ImServerClient(socket, data.clientId);
        var newid = Guid.NewGuid().ToString();

        var wslist = _clients.GetOrAdd(data.clientId, cliid => new ConcurrentDictionary<string, ImServerClient>());
        wslist.TryAdd(newid, cli);
        using (var pipe = _redis.StartPipe())
        {
            pipe.HIncrBy($"{_redisPrefix}Online", data.clientId.ToString(), 1);
            pipe.Publish($"evt_{_redisPrefix}Online", token_value);
            pipe.EndPipe();
        }

        var buffer = new byte[BufferSize];
        var seg = new ArraySegment<byte>(buffer);
        try
        {
            while (socket.State == WebSocketState.Open && _clients.ContainsKey(data.clientId))
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }
                string response = await ReceiveStringAsync(socket, ct);
                
                if (string.IsNullOrEmpty(response))
                {
                    continue;
                }
                MsgTemplate msg = null;
                try
                {
                    msg = JsonConvert.DeserializeObject<MsgTemplate>(response);
                    ImServerClient currentSocket = wslist.Values.First(u => u.clientId == msg.clientId);
                    if (currentSocket == null)
                    {
                        continue;
                    }
                    if (msg.type == "ping")
                    {
                        if (msg.currentSence == 0)
                        {
                            await SendStringAsync(currentSocket.socket, JsonConvert.SerializeObject(msg), ct);
                        }
                        else if (msg.currentSence == 1)
                        {
                            await _redis.SetAsync($"KEY_IM_DOCITOR_CONNECTION_SESSION:{msg.clientId}", msg.connectionBatchCode, 3);
                        }
                    }
                }
                catch
                {

                }
            }
            socket.Abort();
            //await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
            //socket.Dispose();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Socket异常:{ex.Message}");
        }
        wslist.TryRemove(newid, out var oldcli);
        if (wslist.Any() == false) _clients.TryRemove(data.clientId, out var oldwslist);
        _redis.Eval($"if redis.call('HINCRBY', KEYS[1], '{data.clientId}', '-1') <= 0 then redis.call('HDEL', KEYS[1], '{data.clientId}') end return 1", new[] { $"{_redisPrefix}Online" });
        LeaveChan(data.clientId, GetChanListByClientId(data.clientId));
        //_redis.Publish($"evt_{_redisPrefix}Offline", token_value);
    }

    private static Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
    {
        var buffer = Encoding.UTF8.GetBytes(data);
        var segment = new ArraySegment<byte>(buffer);
        return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
    }

    private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
    {
        var buffer = new ArraySegment<byte>(new byte[BufferSize]);
        using (var ms = new MemoryStream())
        {
            WebSocketReceiveResult result;
            do
            {
                ct.ThrowIfCancellationRequested();
                result = await socket.ReceiveAsync(buffer, ct);
                ms.Write(buffer.Array, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);
            if (result.MessageType != WebSocketMessageType.Text)
            {
                return null;
            }

            using (var reader = new StreamReader(ms, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}

#endif
