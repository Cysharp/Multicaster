using System.Collections.Concurrent;
using Cysharp.Runtime.Multicast.Remoting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Cysharp.Runtime.Multicast.Distributed.Redis;

public class RedisGroupProvider : IMulticastGroupProvider, IDisposable
{
    private readonly ConcurrentDictionary<(string Name, Type KeyType, Type ReceiverType), object> _groups = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly ConnectionMultiplexer? _createdConnectionMultiplexer;
    private readonly ISubscriber _subscriber;
    private readonly string? _prefix;

    public RedisGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IOptions<RedisGroupOptions> options)
        : this(proxyFactory, serializer, options.Value)
    {}

    public RedisGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, RedisGroupOptions options)
    {
        _proxyFactory = proxyFactory;
        _serializer = serializer;

        if (options.ConnectionMultiplexer is null)
        {
            _createdConnectionMultiplexer = ConnectionMultiplexer.Connect(options.ConnectionString);
            _subscriber = _createdConnectionMultiplexer.GetSubscriber();
        }
        else
        {
            _subscriber = options.ConnectionMultiplexer.GetSubscriber();
        }
        _prefix = options.Prefix;
    }

    public IMulticastAsyncGroup<TKey, TReceiver> GetOrAddGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new RedisGroup<TKey, TReceiver>(_prefix + name, _subscriber, _proxyFactory, _serializer));

    public IMulticastSyncGroup<TKey, TReceiver> GetOrAddSynchronousGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new RedisGroup<TKey, TReceiver>(_prefix + name, _subscriber, _proxyFactory, _serializer));

    public void Dispose()
    {
        _createdConnectionMultiplexer?.Dispose();
    }
}

public class RedisGroupOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public ConnectionMultiplexer? ConnectionMultiplexer { get; set; }
    public string? Prefix { get; set; }
}