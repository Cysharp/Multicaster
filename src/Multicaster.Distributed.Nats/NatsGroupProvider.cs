using System.Collections.Concurrent;
using Cysharp.Runtime.Multicast.Remoting;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace Cysharp.Runtime.Multicast.Distributed.Nats;

public class NatsGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(string Name, Type Type), object> _groups = new();
    private readonly NatsConnection _connection;
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;

    public NatsGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IOptions<NatsGroupOptions> options)
        : this(proxyFactory, serializer, options.Value)
    {}

    public NatsGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, NatsGroupOptions options)
    {
        _connection = new NatsConnection(NatsOpts.Default with { Url = options.Url });
        _proxyFactory = proxyFactory;
        _serializer = serializer;
    }

    public IMulticastAsyncGroup<TReceiver> GetOrAddGroup<TReceiver>(string name)
        => (IMulticastAsyncGroup<TReceiver>)_groups.GetOrAdd((name, typeof(TReceiver)), _ => new NatsGroup<TReceiver>(name, _connection, _proxyFactory, _serializer));

    public IMulticastSyncGroup<TReceiver> GetOrAddSynchronousGroup<TReceiver>(string name)
        => (IMulticastSyncGroup<TReceiver>)_groups.GetOrAdd((name, typeof(TReceiver)), _ => new NatsGroup<TReceiver>(name, _connection, _proxyFactory, _serializer));
}

public class NatsGroupOptions
{
    public string Url { get; set; } = "nats://localhost:4222";
}