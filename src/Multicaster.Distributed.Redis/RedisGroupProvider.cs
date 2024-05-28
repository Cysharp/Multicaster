using System.Collections.Concurrent;
using Cysharp.Runtime.Multicast.Remoting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Cysharp.Runtime.Multicast.Distributed.Redis;

public class RedisGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(string Name, Type Type), object> _groups = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly ISubscriber _subscriber;
    private readonly string? _prefix;

    public RedisGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IOptions<RedisGroupOptions> options)
        : this(proxyFactory, serializer, options.Value)
    {}

    public RedisGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, RedisGroupOptions options)
    {
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _subscriber = ConnectionMultiplexer.Connect(options.ConnectionString).GetSubscriber();
        _prefix = options.Prefix;
    }

    public IMulticastAsyncGroup<TReceiver> GetOrAddGroup<TReceiver>(string name)
        => (IMulticastAsyncGroup<TReceiver>)_groups.GetOrAdd((name, typeof(TReceiver)), _ => new RedisGroup<TReceiver>(_prefix + name, _subscriber, _proxyFactory, _serializer));

    public IMulticastSyncGroup<TReceiver> GetOrAddSynchronousGroup<TReceiver>(string name)
        => (IMulticastSyncGroup<TReceiver>)_groups.GetOrAdd((name, typeof(TReceiver)), _ => new RedisGroup<TReceiver>(_prefix + name, _subscriber, _proxyFactory, _serializer));
}

public class RedisGroupOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string? Prefix { get; set; }
}