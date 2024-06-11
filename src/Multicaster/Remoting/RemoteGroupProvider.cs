using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cysharp.Runtime.Multicast.InMemory;

namespace Cysharp.Runtime.Multicast.Remoting;

public class RemoteGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(Type KeyType, Type ReceiverType, string name), object> _groups = new();
    private readonly IInMemoryProxyFactory _proxyFactory;
    private readonly IRemoteProxyFactory _remoteProxyFactory;
    private readonly IRemoteSerializer _remoteSerializer;
    private readonly IRemoteClientResultPendingTaskRegistry _pendingTasks;

    public RemoteGroupProvider(IInMemoryProxyFactory proxyFactory, IRemoteProxyFactory remoteProxyFactory, IRemoteSerializer remoteSerializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        _proxyFactory = proxyFactory;
        _remoteProxyFactory = remoteProxyFactory;
        _remoteSerializer = remoteSerializer;
        _pendingTasks = pendingTasks;
    }

    public IMulticastAsyncGroup<TKey, T> GetOrAddGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, T>)_groups.GetOrAdd((typeof(TKey), typeof(T), name), _ => new RemoteCompositeGroup<TKey, T>(name, _proxyFactory, _remoteProxyFactory, _remoteSerializer, _pendingTasks, Remove));

    public IMulticastSyncGroup<TKey, T> GetOrAddSynchronousGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, T>)_groups.GetOrAdd((typeof(TKey), typeof(T), name), _ => new RemoteCompositeGroup<TKey, T>(name, _proxyFactory, _remoteProxyFactory, _remoteSerializer, _pendingTasks, Remove));

    private void Remove<TKey, T>(RemoteCompositeGroup<TKey, T> group)
        where TKey : IEquatable<TKey>
        => _groups.Remove((typeof(TKey), typeof(T), group.Name), out _);
}

internal class RemoteCompositeGroup<TKey, T> : IMulticastAsyncGroup<TKey, T>, IMulticastSyncGroup<TKey, T>, IDisposable
    where TKey : IEquatable<TKey>
{
    private readonly Action<RemoteCompositeGroup<TKey, T>> _disposeAction;
    private readonly IInMemoryProxyFactory _memoryProxyFactory;
    private readonly InMemoryGroup<TKey, T> _memoryGroup;
    private readonly RemoteGroup<TKey, T> _remoteGroup;
    private bool _disposed;

    internal string Name { get; }

    public T All { get; }

    public RemoteCompositeGroup(
        string name,
        IInMemoryProxyFactory memoryProxyFactory,
        IRemoteProxyFactory remoteProxyFactory,
        IRemoteSerializer remoteSerializer,
        IRemoteClientResultPendingTaskRegistry pendingTasks,
        Action<RemoteCompositeGroup<TKey, T>> disposeAction
    )
    {
        Name = name;
        _memoryProxyFactory = memoryProxyFactory;
        _disposeAction = disposeAction;

        _memoryGroup = new InMemoryGroup<TKey, T>(name, memoryProxyFactory, static _ => { });
        _remoteGroup = new RemoteGroup<TKey, T>(name, remoteProxyFactory, remoteSerializer, pendingTasks, static _ => { });
        All = memoryProxyFactory.Create<TKey, T>(ReceiverHolder.CreateImmutable<TKey, T>([_memoryGroup.All, _remoteGroup.All]));
    }

    public T Except(ImmutableArray<TKey> excludes)
    {
        ThrowIfDisposed();
        return _memoryProxyFactory.Create(ReceiverHolder.CreateImmutable<TKey, T>([_memoryGroup.Except(excludes), _remoteGroup.Except(excludes)]));
    }

    public T Only(ImmutableArray<TKey> targets)
    {
        ThrowIfDisposed();
        return _memoryProxyFactory.Create(ReceiverHolder.CreateImmutable<TKey, T>([_memoryGroup.Only(targets), _remoteGroup.Only(targets)]));
    }

    public T Single(TKey target)
    {
        ThrowIfDisposed();
        return _memoryProxyFactory.Create(ReceiverHolder.CreateImmutable<TKey, T>([_memoryGroup.Single(target), _remoteGroup.Single(target)]));
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
    {
        return ValueTask.FromResult(Count());
    }

    public void Add(TKey key, T receiver)
    {
        ThrowIfDisposed();
        if (receiver is IRemoteProxy)
        {
            _remoteGroup.Add(key, receiver);
        }
        else
        {
            _memoryGroup.Add(key, receiver);
        }
    }

    public void Remove(TKey key)
    {
        ThrowIfDisposed();
        _memoryGroup.Remove(key);
        _remoteGroup.Remove(key);
    }

    public int Count()
    {
        ThrowIfDisposed();
        var countInMemory = _memoryGroup.Count();
        var countRemote = _remoteGroup.Count();
        return countInMemory + countRemote;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _memoryGroup.Dispose();
        _remoteGroup.Dispose();
        _disposeAction(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteCompositeGroup<TKey, T>));
    }
}