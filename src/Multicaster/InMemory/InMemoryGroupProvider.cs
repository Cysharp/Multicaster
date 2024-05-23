using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.InMemory;

public class InMemoryGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(Type Type, string name), object> _groups = new();
    private readonly IInMemoryProxyFactory _proxyFactory;

    public InMemoryGroupProvider(IInMemoryProxyFactory proxyFactory)
    {
        _proxyFactory = proxyFactory;
    }

    public IMulticastAsyncGroup<T> GetOrAddGroup<T>(string name)
        => (IMulticastAsyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new InMemoryGroup<T>(_proxyFactory));

    public IMulticastSyncGroup<T> GetOrAddSynchronousGroup<T>(string name)
        => (IMulticastSyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new InMemoryGroup<T>(_proxyFactory));
}

internal class InMemoryGroup<T> : IMulticastAsyncGroup<T>, IMulticastSyncGroup<T>
{
    private readonly ConcurrentDictionary<Guid, T> _receivers = new();
    private readonly IInMemoryProxyFactory _proxyFactory;

    public T All { get; }

    public InMemoryGroup(IInMemoryProxyFactory proxyFactory)
    {
        _proxyFactory = proxyFactory;
        All = _proxyFactory.Create(_receivers);
    }

    public T Except(ImmutableArray<Guid> excludes)
    {
        return _proxyFactory.Except(_receivers, excludes);
    }

    public T Only(ImmutableArray<Guid> targets)
    {
        return _proxyFactory.Only(_receivers, targets);
    }

    public T Single(Guid target)
    {
        return _proxyFactory.Only(_receivers, ImmutableArray<Guid>.Empty.Add(target));
    }

    public ValueTask AddAsync(Guid key, T receiver, CancellationToken cancellationToken = default)
    {
        _receivers[key] = receiver;
        return default;
    }

    public ValueTask RemoveAsync(Guid key, CancellationToken cancellationToken = default)
    {
        _receivers.Remove(key, out _);
        return default;
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_receivers.Count);

    public void Add(Guid key, T receiver)
        => _receivers.TryAdd(key, receiver);

    public void Remove(Guid key)
        => _receivers.TryRemove(key, out _);

    public int Count()
        => _receivers.Count;
}