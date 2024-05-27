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
        => (IMulticastAsyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new InMemoryGroup<T>(name, _proxyFactory, Remove));

    public IMulticastSyncGroup<T> GetOrAddSynchronousGroup<T>(string name)
        => (IMulticastSyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new InMemoryGroup<T>(name, _proxyFactory, Remove));

    private void Remove<T>(InMemoryGroup<T> group)
        => _groups.Remove((typeof(T), group.Name), out _);
}

internal class InMemoryGroup<T> : IMulticastAsyncGroup<T>, IMulticastSyncGroup<T>
{
    private readonly Action<InMemoryGroup<T>> _disposeAction;
    private readonly string _name;
    private readonly IInMemoryProxyFactory _proxyFactory;
    private readonly ConcurrentDictionary<Guid, T> _receivers = new();
    private bool _disposed;

    public T All { get; }

    internal string Name => _name;

    public InMemoryGroup(string name, IInMemoryProxyFactory proxyFactory, Action<InMemoryGroup<T>> disposeAction)
    {
        _name = name;
        _proxyFactory = proxyFactory;
        _disposeAction = disposeAction;
        All = _proxyFactory.Create(_receivers);
    }

    public T Except(ImmutableArray<Guid> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Except(_receivers, excludes);
    }

    public T Only(ImmutableArray<Guid> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Only(_receivers, targets);
    }

    public T Single(Guid target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Only(_receivers, ImmutableArray<Guid>.Empty.Add(target));
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
        => ValueTask.FromResult(_receivers.Count);

    public void Add(Guid key, T receiver)
    {
        ThrowIfDisposed();
        _receivers.TryAdd(key, receiver);
    }

    public void Remove(Guid key)
    {
        ThrowIfDisposed();
        _receivers.TryRemove(key, out _);
    }

    public int Count()
    {
        ThrowIfDisposed();
        return _receivers.Count;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposeAction(this);
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryGroup<T>));
    }
}