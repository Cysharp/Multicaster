using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.InMemory;

public class InMemoryGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(Type KeyType, Type ReceiverType, string name), object> _groups = new();
    private readonly IInMemoryProxyFactory _proxyFactory;

    public InMemoryGroupProvider(IInMemoryProxyFactory proxyFactory)
    {
        _proxyFactory = proxyFactory;
    }

    public IMulticastAsyncGroup<TKey, T> GetOrAddGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, T>)_groups.GetOrAdd((typeof(TKey), typeof(T), name), _ => new InMemoryGroup<TKey, T>(name, _proxyFactory, Remove));

    public IMulticastSyncGroup<TKey, T> GetOrAddSynchronousGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, T>)_groups.GetOrAdd((typeof(TKey), typeof(T), name), _ => new InMemoryGroup<TKey, T>(name, _proxyFactory, Remove));

    private void Remove<TKey, T>(InMemoryGroup<TKey, T> group)
        where TKey : IEquatable<TKey>
        => _groups.Remove((typeof(TKey), typeof(T), group.Name), out _);
}

internal class InMemoryGroup<TKey, T> : IMulticastAsyncGroup<TKey, T>, IMulticastSyncGroup<TKey, T>
    where TKey : IEquatable<TKey>
{
    private readonly Action<InMemoryGroup<TKey, T>> _disposeAction;
    private readonly string _name;
    private readonly IInMemoryProxyFactory _proxyFactory;
    private readonly MutableReceiverHolder<TKey, T> _receivers = new();
    private bool _disposed;

    public T All { get; }
    internal string Name => _name;

    public InMemoryGroup(string name, IInMemoryProxyFactory proxyFactory, Action<InMemoryGroup<TKey, T>> disposeAction)
    {
        _name = name;
        _proxyFactory = proxyFactory;
        _disposeAction = disposeAction;
        All = _proxyFactory.Create(_receivers);
    }

    public T Except(IEnumerable<TKey> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Except(_receivers, [..excludes]);
    }

    public T Only(IEnumerable<TKey> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Only(_receivers, [..targets]);
    }

    public T Single(TKey target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Only(_receivers, ImmutableArray<TKey>.Empty.Add(target));
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
        => ValueTask.FromResult(_receivers.Count);

    public void Add(TKey key, T receiver)
    {
        ThrowIfDisposed();
        _receivers.Add(key, receiver);
    }

    public void Remove(TKey key)
    {
        ThrowIfDisposed();
        _receivers.Remove(key);
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
        if (_disposed) throw new ObjectDisposedException(nameof(InMemoryGroup<TKey, T>));
    }
}