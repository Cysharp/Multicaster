using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cysharp.Runtime.Multicast.InMemory;

namespace Cysharp.Runtime.Multicast.Remoting;

public class RemoteCompositeGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(Type Type, string name), object> _groups = new();
    private readonly IInMemoryProxyFactory _proxyFactory;
    private readonly IRemoteProxyFactory _remoteProxyFactory;
    private readonly IRemoteSerializer _remoteSerializer;
    private readonly IRemoteClientResultPendingTaskRegistry _pendingTasks;

    public RemoteCompositeGroupProvider(IInMemoryProxyFactory proxyFactory, IRemoteProxyFactory remoteProxyFactory, IRemoteSerializer remoteSerializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        _proxyFactory = proxyFactory;
        _remoteProxyFactory = remoteProxyFactory;
        _remoteSerializer = remoteSerializer;
        _pendingTasks = pendingTasks;
    }

    public IMulticastAsyncGroup<T> GetOrAddGroup<T>(string name)
        => (IMulticastAsyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteCompositeGroup<T>(name, _proxyFactory, _remoteProxyFactory, _remoteSerializer, _pendingTasks, Remove));

    public IMulticastSyncGroup<T> GetOrAddSynchronousGroup<T>(string name)
        => (IMulticastSyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteCompositeGroup<T>(name, _proxyFactory, _remoteProxyFactory, _remoteSerializer, _pendingTasks, Remove));

    private void Remove<T>(RemoteCompositeGroup<T> group)
        => _groups.Remove((typeof(T), group.Name), out _);
}

internal class RemoteCompositeGroup<T> : IMulticastAsyncGroup<T>, IMulticastSyncGroup<T>, IDisposable
{
    private readonly Action<RemoteCompositeGroup<T>> _disposeAction;
    private readonly IInMemoryProxyFactory _memoryProxyFactory;
    private readonly (Guid Id, InMemoryGroup<T> Group) _memoryGroup;
    private readonly (Guid Id, RemoteGroup<T> Group) _remoteGroup;
    private bool _disposed;

    internal string Name { get; }

    public T All { get; }

    public RemoteCompositeGroup(
        string name,
        IInMemoryProxyFactory memoryProxyFactory,
        IRemoteProxyFactory remoteProxyFactory,
        IRemoteSerializer remoteSerializer,
        IRemoteClientResultPendingTaskRegistry pendingTasks,
        Action<RemoteCompositeGroup<T>> disposeAction
    )
    {
        Name = name;
        _memoryProxyFactory = memoryProxyFactory;
        _disposeAction = disposeAction;

        _memoryGroup = (Guid.NewGuid(), new InMemoryGroup<T>(name, memoryProxyFactory, static _ => { }));
        _remoteGroup = (Guid.NewGuid(), new RemoteGroup<T>(name, remoteProxyFactory, remoteSerializer, pendingTasks, static _ => { }));
        All = memoryProxyFactory.Create<T>([KeyValuePair.Create(_memoryGroup.Id, _memoryGroup.Group.All), KeyValuePair.Create(_remoteGroup.Id, _remoteGroup.Group.All)]);
    }

    public T Except(ImmutableArray<Guid> excludes)
    {
        ThrowIfDisposed();
        return _memoryProxyFactory.Create([KeyValuePair.Create(_memoryGroup.Id, _memoryGroup.Group.Except(excludes)), KeyValuePair.Create(_remoteGroup.Id, _remoteGroup.Group.Except(excludes))]);
    }

    public T Only(ImmutableArray<Guid> targets)
    {
        ThrowIfDisposed();
        return _memoryProxyFactory.Create([KeyValuePair.Create(_memoryGroup.Id, _memoryGroup.Group.Only(targets)), KeyValuePair.Create(_remoteGroup.Id, _remoteGroup.Group.Only(targets))]);
    }

    public T Single(Guid target)
    {
        ThrowIfDisposed();
        return _memoryProxyFactory.Create([KeyValuePair.Create(_memoryGroup.Id, _memoryGroup.Group.Single(target)), KeyValuePair.Create(_remoteGroup.Id, _remoteGroup.Group.Single(target))], ImmutableArray<Guid>.Empty, ImmutableArray<Guid>.Empty.Add(target));
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
    {
        return ValueTask.FromResult(Count());
    }

    public void Add(Guid key, T receiver)
    {
        ThrowIfDisposed();
        if (receiver is IRemoteProxy)
        {
            _remoteGroup.Group.Add(key, receiver);
        }
        else
        {
            _memoryGroup.Group.Add(key, receiver);
        }
    }

    public void Remove(Guid key)
    {
        ThrowIfDisposed();
        _memoryGroup.Group.Remove(key);
        _remoteGroup.Group.Remove(key);
    }

    public int Count()
    {
        ThrowIfDisposed();
        var countInMemory = _memoryGroup.Group.Count();
        var countRemote = _remoteGroup.Group.Count();
        return countInMemory + countRemote;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _memoryGroup.Group.Dispose();
        //_remoteGroup.Group.Dispose();
        _disposeAction(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteCompositeGroup<T>));
    }
}