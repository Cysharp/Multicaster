using System.Collections.Concurrent;
using Cysharp.Runtime.Multicast.Remoting;

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

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
    private readonly MessagePackSerializerOptions _messagePackSerializerOptionsForKey;

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

        var messagePackSerializerOptions = (options.MessagePackSerializerOptionsForKey ?? MessagePackSerializer.DefaultOptions);
        _messagePackSerializerOptionsForKey = messagePackSerializerOptions.WithResolver(
            CompositeResolver.Create([NativeGuidFormatter.Instance], [messagePackSerializerOptions.Resolver])
        );
    }

    public IMulticastAsyncGroup<TKey, TReceiver> GetOrAddGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new RedisGroup<TKey, TReceiver>(_prefix + name, _subscriber, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey, Remove));

    public IMulticastSyncGroup<TKey, TReceiver> GetOrAddSynchronousGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new RedisGroup<TKey, TReceiver>(_prefix + name, _subscriber, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey, Remove));

    private void Remove<TKey, TReceiver>(RedisGroup<TKey, TReceiver> group)
        where TKey : IEquatable<TKey>
    {
        _groups.TryRemove((group.Name, typeof(TKey), typeof(TReceiver)), out _);
    }

    public void Dispose()
    {
        _createdConnectionMultiplexer?.Dispose();
    }
}

public class RedisGroupOptions
{
    /// <summary>
    /// Gets or sets the connection string to connect to Redis. If <see cref="ConnectionMultiplexer"/> property is not set, this will be used.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets a ConnectionMultiplexer instance to connect to Redis. If this is set, <see cref="ConnectionString"/> property will be ignored.
    /// </summary>
    public ConnectionMultiplexer? ConnectionMultiplexer { get; set; }

    /// <summary>
    /// Gets or sets a prefix for the Redis key.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets a MessagePackSerializerOptions used for serializing the key.
    /// </summary>
    public MessagePackSerializerOptions? MessagePackSerializerOptionsForKey { get; set; }
}