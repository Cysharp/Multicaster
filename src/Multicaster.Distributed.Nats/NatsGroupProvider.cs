using System.Collections.Concurrent;
using Cysharp.Runtime.Multicast.Remoting;

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace Cysharp.Runtime.Multicast.Distributed.Nats;

/// <summary>
/// Provides functionality for managing multicast groups over a NATS (NATS.io) connection.
/// </summary>
public class NatsGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(string Name, Type KeyType, Type ReceiverType), object> _groups = new();
    private readonly NatsConnection _connection;
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly MessagePackSerializerOptions _messagePackSerializerOptionsForKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsGroupProvider"/> class with the specified proxy factory, serializer, and configuration options.
    /// </summary>
    public NatsGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IOptions<NatsGroupOptions> options)
        : this(proxyFactory, serializer, options.Value)
    {}

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsGroupProvider"/> class, which provides functionality for managing groups using NATS messaging.
    /// </summary>
    public NatsGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, NatsGroupOptions options)
    {
        _connection = new NatsConnection(NatsOpts.Default with { Url = options.Url });
        _proxyFactory = proxyFactory;
        _serializer = serializer;

        var messagePackSerializerOptions = (options.MessagePackSerializerOptionsForKey ?? MessagePackSerializer.DefaultOptions);
        _messagePackSerializerOptionsForKey = messagePackSerializerOptions.WithResolver(
            CompositeResolver.Create([NativeGuidFormatter.Instance], [messagePackSerializerOptions.Resolver])
        );
    }

    /// <inheritdoc />
    public IMulticastAsyncGroup<TKey, TReceiver> GetOrAddGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new NatsGroup<TKey, TReceiver>(name, _connection, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey, Remove));

    /// <inheritdoc />
    public IMulticastSyncGroup<TKey, TReceiver> GetOrAddSynchronousGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new NatsGroup<TKey, TReceiver>(name, _connection, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey, Remove));

    private void Remove<TKey, TReceiver>(NatsGroup<TKey, TReceiver> group)
        where TKey : IEquatable<TKey>
    {
        _groups.TryRemove((group.Name, typeof(TKey), typeof(TReceiver)), out _);
    }
}

/// <summary>
/// Represents configuration options for a NATS group, including server connection details and serialization settings.
/// </summary>
public class NatsGroupOptions
{
    /// <summary>
    /// Gets or sets the NATS server URL.
    /// </summary>
    public string Url { get; set; } = "nats://localhost:4222";

    /// <summary>
    /// Gets or sets a MessagePackSerializerOptions used for serializing the key.
    /// </summary>
    public MessagePackSerializerOptions MessagePackSerializerOptionsForKey { get; set; } = MessagePackSerializer.DefaultOptions;
}
