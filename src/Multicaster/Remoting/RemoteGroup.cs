using System.Collections.Concurrent;
using System.Collections.Immutable;

using static System.Net.Mime.MediaTypeNames;

namespace Cysharp.Runtime.Multicast.Remoting;

internal class RemoteGroup<TKey, T> : IMulticastAsyncGroup<TKey, T>, IMulticastSyncGroup<TKey, T>, IDisposable
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _receivers = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteClientResultPendingTaskRegistry _pendingTasks;
    private readonly Action<RemoteGroup<TKey, T>> _disposeAction;
    private bool _disposed;

    public T All { get; }

    internal string Name { get; }

    public RemoteGroup(string name, IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks, Action<RemoteGroup<TKey, T>> disposeAction)
    {
        Name = name;
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _pendingTasks = pendingTasks;
        _disposeAction = disposeAction;

        All = _proxyFactory.Create<TKey, T>(_receivers, _serializer, _pendingTasks);
    }

    public ValueTask AddAsync(TKey key, T receiver, CancellationToken cancellationToken = default)
    {
        Add(key, receiver);
        return default;
    }

    public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        Remove(key);
        return default;
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Count());

    public void Add(TKey key, T receiver)
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

    public void Remove(TKey key)
    {
        ThrowIfDisposed();
        _receivers.Remove(key, out _);
    }

    public int Count()
    {
        ThrowIfDisposed();
        return _receivers.Count;
    }

    public T Except(IEnumerable<TKey> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Except<TKey, T>(_receivers, [..excludes], _serializer, _pendingTasks);
    }

    public T Only(IEnumerable<TKey> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Only<TKey, T>(_receivers, [..targets], _serializer, _pendingTasks);
    }

    public T Single(TKey target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Single<TKey, T>(_receivers, target, _serializer, _pendingTasks);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposeAction(this);
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteGroup<TKey, T>));
    }
}