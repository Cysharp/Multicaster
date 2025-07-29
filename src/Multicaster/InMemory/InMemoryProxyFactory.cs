using System.Collections.Immutable;
using System.Runtime.InteropServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cysharp.Runtime.Multicast.InMemory;

public interface IInMemoryProxyFactory
{
    T Create<TKey, T>(IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
        where TKey : IEquatable<TKey>;
}

public static class InMemoryProxyFactory
{
    public static T Create<TKey, T>(this IInMemoryProxyFactory proxyFactory, IReceiverHolder<TKey, T> receivers)
        where TKey : IEquatable<TKey>
        => proxyFactory.Create(receivers, ImmutableArray<TKey>.Empty, null);

    public static T Except<TKey, T>(this IInMemoryProxyFactory proxyFactory, IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> excludes)
        where TKey : IEquatable<TKey>
        => proxyFactory.Create(receivers, excludes, null);

    public static T Only<TKey, T>(this IInMemoryProxyFactory proxyFactory, IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> targets)
        where TKey : IEquatable<TKey>
        => proxyFactory.Create(receivers, ImmutableArray<TKey>.Empty, targets);
}

public interface IReceiverHolder<TKey, T>
    where TKey : IEquatable<TKey>
{
    ReceiversSnapshot<ReceiverRegistration<TKey, T>> AsSnapshot();
}

public static class ReceiverHolder
{
    public static ImmutableReceiverHolder<TKey, T> CreateImmutable<TKey, T>(IEnumerable<T> receivers)
        where TKey : IEquatable<TKey>
        => new(receivers);

    public static ImmutableReceiverHolder<TKey, T> CreateImmutable<TKey, T>(T receiver1, T receiver2)
        where TKey : IEquatable<TKey>
        => new(receiver1, receiver2);

    public static MutableReceiverHolder<TKey, T> CreateMutable<TKey, T>()
        where TKey : IEquatable<TKey>
        => new();

    public static MutableReceiverHolder<TKey, T> CreateMutableWithInitialReceivers<TKey, T>(IEnumerable<(TKey, T)> receivers)
        where TKey : IEquatable<TKey>
        => new(receivers);
}

public readonly record struct ReceiverRegistration<TKey, T>(TKey? Key, T Receiver, bool HasKey)
    where TKey : IEquatable<TKey>;

public class ImmutableReceiverHolder<TKey, T> : IReceiverHolder<TKey, T>
    where TKey : IEquatable<TKey>
{
    private readonly ReceiverRegistration<TKey, T>[] _receivers;

    public ReceiversSnapshot<ReceiverRegistration<TKey, T>> AsSnapshot()
        => new(_receivers.AsSpan(), static _ => { }, default);

    public ImmutableReceiverHolder(IEnumerable<(TKey Key, T Receiver)> receivers)
        => _receivers = receivers.Select(x => new ReceiverRegistration<TKey, T>(x.Key, x.Receiver, HasKey: true)).ToArray();

    public ImmutableReceiverHolder(IEnumerable<T> receivers)
        => _receivers = receivers.Select(x => new ReceiverRegistration<TKey, T>(default, x, HasKey: false)).ToArray();

    public ImmutableReceiverHolder(T receiver1, T receiver2)
        => _receivers = [new ReceiverRegistration<TKey, T>(default, receiver1, HasKey: false), new ReceiverRegistration<TKey, T>(default, receiver2, HasKey: false)];
}

public class MutableReceiverHolder<TKey, T> : IReceiverHolder<TKey, T>
    where TKey : IEquatable<TKey>
{
    private readonly List<ReceiverRegistration<TKey, T>> _receivers = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public ReceiversSnapshot<ReceiverRegistration<TKey, T>> AsSnapshot()
    {
        _lock.EnterReadLock();
        return new ReceiversSnapshot<ReceiverRegistration<TKey, T>>(CollectionsMarshal.AsSpan(_receivers), static state => ((ReaderWriterLockSlim)state!).ExitReadLock(), _lock);
    }

    public MutableReceiverHolder(IEnumerable<(TKey, T)>? receivers = default)
    {
        _receivers.AddRange(receivers?.Select(x => new ReceiverRegistration<TKey, T>(x.Item1, x.Item2, HasKey: true)) ?? Array.Empty<ReceiverRegistration<TKey, T>>());
    }

    public void Add(TKey key, T receiver)
    {
        _lock.EnterWriteLock();
        try
        {
            _receivers.Add(new ReceiverRegistration<TKey, T>(key, receiver, HasKey: true));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Remove(TKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            _receivers.RemoveAll(x => x.Key!.Equals(key));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        return true;
    }

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _receivers.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}

public readonly ref struct ReceiversSnapshot<T>
{
    private readonly ReadOnlySpan<T> _items;
    private readonly Action<object?> _onDispose;
    private readonly object? _state;

    public ReadOnlySpan<T> AsSpan() => _items;

    public ReceiversSnapshot(ReadOnlySpan<T> items, Action<object?> onDispose, object? state)
    {
        _items = items;
        _onDispose = onDispose;
        _state = state;
    }

    public void Dispose()
    {
        _onDispose(_state);
    }
}
