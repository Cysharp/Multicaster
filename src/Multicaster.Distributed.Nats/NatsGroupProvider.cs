using System.Collections.Concurrent;
using Cysharp.Runtime.Multicast.Remoting;

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace Cysharp.Runtime.Multicast.Distributed.Nats;

public class NatsGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(string Name, Type KeyType, Type ReceiverType), object> _groups = new();
    private readonly NatsConnection _connection;
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly MessagePackSerializerOptions _messagePackSerializerOptionsForKey;

    public NatsGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IOptions<NatsGroupOptions> options)
        : this(proxyFactory, serializer, options.Value)
    {}

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

    public IMulticastAsyncGroup<TKey, TReceiver> GetOrAddGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new NatsGroup<TKey, TReceiver>(name, _connection, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey));

    public IMulticastSyncGroup<TKey, TReceiver> GetOrAddSynchronousGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, TReceiver>)_groups.GetOrAdd((name, typeof(TKey), typeof(TReceiver)), _ => new NatsGroup<TKey, TReceiver>(name, _connection, _proxyFactory, _serializer, _messagePackSerializerOptionsForKey));
}

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