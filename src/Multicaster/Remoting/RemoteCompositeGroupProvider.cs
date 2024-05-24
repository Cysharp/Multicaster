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
        => (IMulticastAsyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteCompositeGroup<T>(_proxyFactory, _remoteProxyFactory, _remoteSerializer, _pendingTasks));

    public IMulticastSyncGroup<T> GetOrAddSynchronousGroup<T>(string name)
        => (IMulticastSyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteCompositeGroup<T>(_proxyFactory, _remoteProxyFactory, _remoteSerializer, _pendingTasks));
}

internal class RemoteCompositeGroup<T> : IMulticastAsyncGroup<T>, IMulticastSyncGroup<T>
{
    private readonly IInMemoryProxyFactory _memoryProxyFactory;
    private readonly (Guid Id, InMemoryGroup<T> Group) _memoryGroup;
    private readonly (Guid Id, RemoteGroup<T> Group) _remoteGroup;

    public T All { get; }

    public RemoteCompositeGroup(IInMemoryProxyFactory memoryProxyFactory, IRemoteProxyFactory remoteProxyFactory, IRemoteSerializer remoteSerializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        _memoryProxyFactory = memoryProxyFactory;

        _memoryGroup = (Guid.NewGuid(), new InMemoryGroup<T>(memoryProxyFactory));
        _remoteGroup = (Guid.NewGuid(), new RemoteGroup<T>(remoteProxyFactory, remoteSerializer, pendingTasks));
        All = memoryProxyFactory.Create<T>([KeyValuePair.Create(_memoryGroup.Id, _memoryGroup.Group.All), KeyValuePair.Create(_remoteGroup.Id, _remoteGroup.Group.All)]);
    }

    public T Except(ImmutableArray<Guid> excludes)
    {
        return _memoryProxyFactory.Create([KeyValuePair.Create(_memoryGroup.Id, _memoryGroup.Group.Except(excludes)), KeyValuePair.Create(_remoteGroup.Id, _remoteGroup.Group.Except(excludes))]);
    }

    public T Only(ImmutableArray<Guid> targets)
    {
        return _memoryProxyFactory.Create([KeyValuePair.Create(_memoryGroup.Id, _memoryGroup.Group.Only(targets)), KeyValuePair.Create(_remoteGroup.Id, _remoteGroup.Group.Only(targets))]);
    }

    public T Single(Guid target)
    {
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
        _memoryGroup.Group.Remove(key);
        _remoteGroup.Group.Remove(key);
    }

    public int Count()
    {
        var countInMemory = _memoryGroup.Group.Count();
        var countRemote = _remoteGroup.Group.Count();
        return countInMemory + countRemote;
    }
}