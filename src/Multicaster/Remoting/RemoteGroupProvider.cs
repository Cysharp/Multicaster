using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.Remoting;

public class RemoteGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(Type Type, string name), object> _groups = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteCallPendingMessageQueue _pendingQueue;

    public RemoteGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
    {
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _pendingQueue = pendingQueue;
    }

    public IMulticastAsyncGroup<T> GetOrAddGroup<T>(string name)
        => (IMulticastAsyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<T>(_proxyFactory, _serializer, _pendingQueue));

    public IMulticastSyncGroup<T> GetOrAddSynchronousGroup<T>(string name)
        => (IMulticastSyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<T>(_proxyFactory, _serializer, _pendingQueue));
}

internal class RemoteGroup<T> : IMulticastAsyncGroup<T>, IMulticastSyncGroup<T>
{
    private readonly ConcurrentDictionary<Guid, IRemoteReceiverWriter> _receivers = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteCallPendingMessageQueue _pendingQueue;

    public T All { get; }

    public RemoteGroup(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
    {
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _pendingQueue = pendingQueue;

        All = _proxyFactory.Create<T>(_receivers, _serializer, _pendingQueue);
    }

    public ValueTask AddAsync(Guid key, T receiver)
    {
        Add(key, receiver);
        return default;
    }

    public ValueTask RemoveAsync(Guid key)
    {
        Remove(key);
        return default;
    }

    public ValueTask<int> CountAsync()
        => ValueTask.FromResult(Count());

    public void Add(Guid key, T receiver)
    {
        if (receiver is not IRemoteProxy directRemoteReceiverAccessor)
        {
            throw new ArgumentException($"A receiver must implement {nameof(IRemoteProxy)} interface.", nameof(receiver));
        }
        if (!directRemoteReceiverAccessor.TryGetDirectWriter(out var singleReceiver))
        {
            throw new ArgumentException("There must be only one receiver writer. The receiver has zero or no-single receiver writers.", nameof(receiver));
        }

        _receivers[key] = singleReceiver;
    }

    public void Remove(Guid key)
    {
        _receivers.Remove(key, out _);
    }

    public int Count()
    {
        return _receivers.Count;
    }

    public T Except(ImmutableArray<Guid> excludes)
        => _proxyFactory.Except<T>(_receivers, excludes, _serializer, _pendingQueue);

    public T Only(ImmutableArray<Guid> targets)
        => _proxyFactory.Only<T>(_receivers, targets, _serializer, _pendingQueue);

    public T Single(Guid target)
        => _proxyFactory.Single<T>(_receivers, target, _serializer, _pendingQueue);
}