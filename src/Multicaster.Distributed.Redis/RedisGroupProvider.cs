using System.Collections.Concurrent;
using System.Xml.Linq;

using Cysharp.Runtime.Multicast.Remoting;

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace Cysharp.Runtime.Multicast.Distributed.Redis;

/// <summary>
/// Provides functionality for managing multicast groups using Redis as the underlying communication mechanism.
/// </summary>
/// <remarks>This class allows the creation and management of asynchronous and synchronous multicast groups, where
/// messages can be sent to multiple receivers. It uses Redis channels for communication and supports custom
/// serialization and deserialization of keys and messages.</remarks>
public class RedisGroupProvider : IMulticastGroupProvider, IDisposable
{
    private readonly ConcurrentDictionary<(string Name, Type KeyType, Type ReceiverType), object> _groups = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly ConnectionMultiplexer? _createdConnectionMultiplexer;
    private readonly ISubscriber _subscriber;
    private readonly string? _prefix;
    private readonly MessagePackSerializerOptions _messagePackSerializerOptionsForKey;
    private readonly Func<string, RedisChannel> _channelFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisGroupProvider"/> class with the specified proxy factory,
    /// serializer, and configuration options.
    /// </summary>
    /// <param name="proxyFactory">The factory used to create remote proxies for interacting with Redis.</param>
    /// <param name="serializer">The serializer used to serialize and deserialize data for Redis operations.</param>
    /// <param name="options">The configuration options for the Redis group provider.</param>
    public RedisGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IOptions<RedisGroupOptions> options)
        : this(proxyFactory, serializer, options.Value)
    {}

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisGroupProvider"/> class, which provides functionality for
    /// managing Redis-based group communication using a specified remote proxy factory, serializer, and configuration
    /// options.
    /// </summary>
    /// <remarks>If the <see cref="RedisGroupOptions.ConnectionMultiplexer"/> property in <paramref
    /// name="options"/> is <see langword="null"/>, a new connection multiplexer is created using the connection string
    /// specified in <see cref="RedisGroupOptions.ConnectionString"/>. Otherwise, the provided connection multiplexer is
    /// used.</remarks>
    /// <param name="proxyFactory">The factory used to create remote proxies for communication. This parameter cannot be <see langword="null"/>.</param>
    /// <param name="serializer">The serializer used to serialize and deserialize messages. This parameter cannot be <see langword="null"/>.</param>
    /// <param name="options">The configuration options for the Redis group provider, including connection settings, channel factory, and
    /// serialization options. This parameter cannot be <see langword="null"/>.</param>
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
        _channelFactory = options.ChannelFactory;

        var messagePackSerializerOptions = (options.MessagePackSerializerOptionsForKey ?? MessagePackSerializer.DefaultOptions);
        _messagePackSerializerOptionsForKey = messagePackSerializerOptions.WithResolver(
            CompositeResolver.Create([NativeGuidFormatter.Instance], [messagePackSerializerOptions.Resolver])
        );
    }

    /// <inheritdoc />
    public IMulticastAsyncGroup<TKey, TReceiver> GetOrAddGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new RedisGroup<TKey, TReceiver>(_prefix + name, _channelFactory, _subscriber, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey, Remove));

    /// <inheritdoc />
    public IMulticastSyncGroup<TKey, TReceiver> GetOrAddSynchronousGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new RedisGroup<TKey, TReceiver>(_prefix + name, _channelFactory, _subscriber, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey, Remove));

    private void Remove<TKey, TReceiver>(RedisGroup<TKey, TReceiver> group)
        where TKey : IEquatable<TKey>
    {
        _groups.TryRemove((group.Name, typeof(TKey), typeof(TReceiver)), out _);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _createdConnectionMultiplexer?.Dispose();
    }
}
