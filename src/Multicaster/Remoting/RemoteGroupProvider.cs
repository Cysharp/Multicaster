using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.Remoting;

public class RemoteGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(Type Type, string name), object> _groups = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteClientResultPendingTaskRegistry _pendingTasks;

    public RemoteGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _pendingTasks = pendingTasks;
    }

    public IMulticastAsyncGroup<T> GetOrAddGroup<T>(string name)
        => (IMulticastAsyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<T>(name, _proxyFactory, _serializer, _pendingTasks, Remove));

    public IMulticastSyncGroup<T> GetOrAddSynchronousGroup<T>(string name)
        => (IMulticastSyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<T>(name, _proxyFactory, _serializer, _pendingTasks, Remove));

    private void Remove<T>(RemoteGroup<T> group)
        => _groups.Remove((typeof(T), group.Name), out _);
}

internal class RemoteGroup<T> : IMulticastAsyncGroup<T>, IMulticastSyncGroup<T>, IDisposable
{
    private readonly ConcurrentDictionary<Guid, IRemoteReceiverWriter> _receivers = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteClientResultPendingTaskRegistry _pendingTasks;
    private readonly Action<RemoteGroup<T>> _disposeAction;
    private bool _disposed;

    public T All { get; }

    internal string Name { get; }

    public RemoteGroup(string name, IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks, Action<RemoteGroup<T>> disposeAction)
    {
        Name = name;
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _pendingTasks = pendingTasks;
        _disposeAction = disposeAction;

        All = _proxyFactory.Create<T>(_receivers, _serializer, _pendingTasks);
    }

    public ValueTask AddAsync(Guid key, T receiver, CancellationToken cancellationToken = default)
    {
        Add(key, receiver);
        return default;
    }

    public ValueTask RemoveAsync(Guid key, CancellationToken cancellationToken = default)
    {
        Remove(key);
        return default;
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Count());

    public void Add(Guid key, T receiver)
    {
        ThrowIfDisposed();
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
        ThrowIfDisposed();
        _receivers.Remove(key, out _);
    }

    public int Count()
    {
        ThrowIfDisposed();
        return _receivers.Count;
    }

    public T Except(ImmutableArray<Guid> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Except<T>(_receivers, excludes, _serializer, _pendingTasks);
    }

    public T Only(ImmutableArray<Guid> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Only<T>(_receivers, targets, _serializer, _pendingTasks);
    }

    public T Single(Guid target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Single<T>(_receivers, target, _serializer, _pendingTasks);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposeAction(this);
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteGroup<T>));
    }
}