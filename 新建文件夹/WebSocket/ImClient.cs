#if ns20
using FreeRedis;
using imCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// im 核心类实现的配置所需
/// </summary>
public class ImClientOptions
{
    /// <summary>
    /// CSRedis 对象，用于存储数据和发送消息
    /// </summary>
    public RedisClient Redis { get; set; }
    /// <summary>
    /// 负载的服务端
    /// </summary>
    public string[] Servers { get; set; }
    /// <summary>
    /// websocket请求的路径，默认值：/ws
    /// </summary>
    public string PathMatch { get; set; } = "/ws";
}

public class ImSendEventArgs : EventArgs
{
    /// <summary>
    /// 发送者的客户端id
    /// </summary>
    public string SenderClientId { get; }
    /// <summary>
    /// 接收者的客户端id
    /// </summary>
    public List<string> ReceiveClientId { get; } = new List<string>();
    /// <summary>
    /// imServer 服务器节点
    /// </summary>
    public string Server { get; }
    /// <summary>
    /// 消息
    /// </summary>
    public object Message { get; }
    /// <summary>
    /// 是否回执
    /// </summary>
    public bool Receipt { get; }

    internal ImSendEventArgs(string server, string senderClientId, object message, bool receipt = false)
    {
        this.Server = server;
        this.SenderClientId = senderClientId;
        this.Message = message;
        this.Receipt = receipt;
    }
}

/// <summary>
/// im 核心类实现
/// </summary>
public class ImClient
{
    protected RedisClient _redis;
    protected string[] _servers;
    protected string _redisPrefix;
    protected string _pathMatch;

    /// <summary>
    /// 推送消息的事件，可审查推向哪个Server节点
    /// </summary>
    public EventHandler<ImSendEventArgs> OnSend;

    /// <summary>
    /// 初始化 imclient
    /// </summary>
    /// <param name="options"></param>
    public ImClient(ImClientOptions options)
    {
        if (options.Redis == null) throw new ArgumentException("ImClientOptions.Redis 参数不能为空");
        if (options.Servers.Any() == false) throw new ArgumentException("ImClientOptions.Servers 参数不能为空");
        _redis = options.Redis;
        _servers = options.Servers;
        _redisPrefix = $"wsim{options.PathMatch.Replace('/', '_')}";
        _pathMatch = options.PathMatch ?? "/ws";
    }

    /// <summary>
    /// 负载分区规则：取clientId后四位字符，转成10进制数字0-65535，求模
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <returns></returns>
    protected string SelectServer(string clientId)
    {
        var servers_idx = int.Parse(clientId.Substring(28), NumberStyles.HexNumber) % _servers.Length;
        if (servers_idx >= _servers.Length) servers_idx = 0;
        return _servers[servers_idx];
    }

    /// <summary>
    /// ImServer 连接前的负载、授权，返回 ws 目标地址，使用该地址连接 websocket 服务端
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="clientMetaData">客户端相关信息，比如ip</param>
    /// <returns>websocket 地址：ws://xxxx/ws?token=xxx</returns>
    public string PrevConnectServer(string clientId, string clientMetaData)
    {
        var server = SelectServer(clientId);
        var token = $"{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}{Guid.NewGuid()}".Replace("-", "");
        _redis.Set($"{_redisPrefix}Token{token}", JsonConvert.SerializeObject((clientId, clientMetaData)), 1000);
        return $"ws://{server}{_pathMatch}?token={token}";
    }

    /// <summary>
    /// 向指定的多个客户端id发送消息
    /// </summary>
    /// <param name="senderClientId">发送者的客户端id</param>
    /// <param name="receiveClientId">接收者的客户端id</param>
    /// <param name="message">消息</param>
    /// <param name="receipt">是否回执</param>
    //public void SendMessage(string senderClientId, IEnumerable<string> receiveClientId, object message, bool receipt = false)
    //{
    //    receiveClientId = receiveClientId.Distinct().ToArray();
    //    Dictionary<string, ImSendEventArgs> redata = new Dictionary<string, ImSendEventArgs>();

    //    foreach (var uid in receiveClientId)
    //    {
    //        string server = SelectServer(uid);
    //        if (redata.ContainsKey(server) == false) redata.Add(server, new ImSendEventArgs(server, senderClientId, message, receipt));
    //        redata[server].ReceiveClientId.Add(uid);
    //    }
    //    var messageJson = JsonConvert.SerializeObject(message);
    //    foreach (var sendArgs in redata.Values)
    //    {
    //        OnSend?.Invoke(this, sendArgs);
    //        _redis.Publish($"{_redisPrefix}Server{sendArgs.Server}",
    //            JsonConvert.SerializeObject((senderClientId, sendArgs.ReceiveClientId, messageJson, sendArgs.Receipt)));
    //    }
    //}

    /// <summary>
    /// 向指定的多个客户端id发送消息
    /// </summary>
    /// <param name="senderClientId">发送者的客户端id</param>
    /// <param name="receiveClientId">接收者的客户端id</param>
    /// <param name="message">消息</param>
    /// <param name="receipt">是否回执</param>
    public void SendMessage(string senderClientId, IEnumerable<string> receiveClientId, string queenName,object message, bool receipt = false)
    {
        receiveClientId = receiveClientId.Distinct().ToArray();
        Dictionary<string, ImSendEventArgs> redata = new Dictionary<string, ImSendEventArgs>();

        foreach (var uid in receiveClientId)
        {
            string server = SelectServer(uid);
            if (redata.ContainsKey(server) == false) redata.Add(server, new ImSendEventArgs(server, senderClientId, message, receipt));
            redata[server].ReceiveClientId.Add(uid);
        }
        var messageJson = JsonConvert.SerializeObject(message);
        foreach (var sendArgs in redata.Values)
        {
            try
            {
                OnSend?.Invoke(this, sendArgs);
                
                //交换机名称
                string exchangeName = $"{_redisPrefix}Server{sendArgs.Server}";
                var msg = JsonConvert.SerializeObject((senderClientId, sendArgs.ReceiveClientId, messageJson, sendArgs.Receipt));
                IRabbitMQHelper rabbmqHelper = ServiceLocator.Instance.GetService<IRabbitMQHelper>();
                var connection = rabbmqHelper.CreateMQConnection();
                rabbmqHelper.SendMsg(connection, exchangeName, ExchangeType.Direct, queenName, msg, true);
            }
            catch
            {
                continue;
            }
        }
    }

    /// <summary>
    /// 获取所在线客户端id
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetClientListByOnline()
    {
        return _redis.HKeys($"{_redisPrefix}Online").Where(a => !string.IsNullOrEmpty(a));
    }

    /// <summary>
    /// 判断客户端是否在线
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public bool HasOnline(string clientId)
    {
        return _redis.HGet<int>($"{_redisPrefix}Online", clientId) > 0;
    }

    /// <summary>
    /// 事件订阅
    /// </summary>
    /// <param name="online">上线</param>
    /// <param name="offline">下线</param>
    public void EventBus(
        Action<(string clientId, string clientMetaData)> online,
        Action<(string clientId, string clientMetaData)> offline)
    {
        //var chanOnline = $"evt_{_redisPrefix}Online";
        //var chanOffline = $"evt_{_redisPrefix}Offline";
        //_redis.Subscribe(new[] { chanOnline, chanOffline }, (chan, msg) =>
        //{
        //    if (chan == chanOnline) online(JsonConvert.DeserializeObject<(string clientId, string clientMetaData)>(msg as string));
        //    if (chan == chanOffline) offline(JsonConvert.DeserializeObject<(string clientId, string clientMetaData)>(msg as string));
        //});
    }

    #region 群聊频道，每次上线都必须重新加入

    /// <summary>
    /// 加入群聊频道，每次上线都必须重新加入
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="chan">群聊频道名</param>
    public void JoinChan(string clientId, string chan)
    {
        using (var pipe = _redis.StartPipe())
        {
            pipe.HSet($"{_redisPrefix}Chan{chan}", clientId.ToString(), 0);
            pipe.HSet($"{_redisPrefix}Client{clientId}", chan, 0);
            pipe.HIncrBy($"{_redisPrefix}ListChan", chan, 1);
            pipe.EndPipe();
        }
    }
    /// <summary>
    /// 离开群聊频道
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <param name="chans">群聊频道名</param>
    public void LeaveChan(string clientId, params string[] chans)
    {
        if (chans?.Any() != true) return;
        using (var pipe = _redis.StartPipe())
        {
            foreach (var chan in chans)
            {
                pipe.HDel($"{_redisPrefix}Chan{chan}", clientId.ToString());
                pipe.HDel($"{_redisPrefix}Client{clientId}", chan);
                pipe.Eval($"if redis.call('HINCRBY', KEYS[1], '{chan}', '-1') <= 0 then redis.call('HDEL', KEYS[1], '{chan}') end return 1", new[] { $"{_redisPrefix}ListChan" });
            }
            pipe.EndPipe();
        }
    }
    /// <summary>
    /// 获取群聊频道所有客户端id（测试）
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    /// <returns></returns>
    public string[] GetChanClientList(string chan)
    {
        return _redis.HKeys($"{_redisPrefix}Chan{chan}").Select(a => a).ToArray();
    }
    /// <summary>
    /// 清理群聊频道的离线客户端（测试）
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    public void ClearChanClient(string chan)
    {
        var websocketIds = _redis.HKeys($"{_redisPrefix}Chan{chan}");
        var offline = new List<string>();
        var span = websocketIds.AsSpan();
        var start = span.Length;
        while (start > 0)
        {
            start = start - 10;
            var length = 10;
            if (start < 0)
            {
                length = start + 10;
                start = 0;
            }
            var slice = span.Slice(start, length);
            var hvals = _redis.HMGet($"{_redisPrefix}Online", slice.ToArray().Select(b => b.ToString()).ToArray());
            for (var a = length - 1; a >= 0; a--)
            {
                if (string.IsNullOrEmpty(hvals[a]))
                {
                    offline.Add(span[start + a]);
                    span[start + a] = null;
                }
            }
        }
        //删除离线订阅
        if (offline.Any()) _redis.HDel($"{_redisPrefix}Chan{chan}", offline.ToArray());
    }

    /// <summary>
    /// 获取所有群聊频道和在线人数
    /// </summary>
    /// <returns>频道名和在线人数</returns>
    public IEnumerable<(string chan, long online)> GetChanList()
    {
        var ret = _redis.HGetAll<long>($"{_redisPrefix}ListChan");
        return ret.Select(a => (a.Key, a.Value));
    }
    /// <summary>
    /// 获取用户参与的所有群聊频道
    /// </summary>
    /// <param name="clientId">客户端id</param>
    /// <returns></returns>
    public string[] GetChanListByClientId(string clientId)
    {
        return _redis.HKeys($"{_redisPrefix}Client{clientId}");
    }
    /// <summary>
    /// 获取群聊频道的在线人数
    /// </summary>
    /// <param name="chan">群聊频道名</param>
    /// <returns>在线人数</returns>
    public long GetChanOnline(string chan)
    {
        return _redis.HGet<long>($"{_redisPrefix}ListChan", chan);
    }

    /// <summary>
    /// 发送群聊消息，所有在线的用户将收到消息
    /// </summary>
    /// <param name="senderClientId">发送者的客户端id</param>
    /// <param name="chan">群聊频道名</param>
    /// <param name="message">消息</param>
	public void SendChanMessage(string senderClientId, string chan, object message)
    {
        var websocketIds = _redis.HKeys($"{_redisPrefix}Chan{chan}");
        SendMessage(string.Empty, websocketIds.Where(a => !string.IsNullOrEmpty(a)).ToArray(),"test.channel.message", message);
    }

    #endregion
}

#endif
