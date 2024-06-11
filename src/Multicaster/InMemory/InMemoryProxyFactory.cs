using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

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
    ReadOnlySpan<ReceiverRegistration<TKey, T>> AsSpan();
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

    public ReadOnlySpan<ReceiverRegistration<TKey, T>> AsSpan()
        => _receivers.AsSpan();

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
    private readonly object _lock = new();
    private ReceiverRegistration<TKey, T>[] _receivers;

    public ReadOnlySpan<ReceiverRegistration<TKey, T>> AsSpan()
        => _receivers.AsSpan();

    public MutableReceiverHolder(IEnumerable<(TKey, T)>? receivers = default)
    {
        _receivers = receivers?.Select(x => new ReceiverRegistration<TKey, T>(x.Item1, x.Item2, HasKey: true)).ToArray() ?? Array.Empty<ReceiverRegistration<TKey, T>>();
    }

    public void Add(TKey key, T receiver)
    {
        lock (_lock)
        {
            var newReceivers = new ReceiverRegistration<TKey, T>[_receivers.Length + 1];
            _receivers.CopyTo(newReceivers, 0);
            newReceivers[^1] = new ReceiverRegistration<TKey, T>(key, receiver, HasKey: true);

            _receivers = newReceivers;
        }
    }

    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            var index = Array.FindIndex(_receivers, x => x.Key!.Equals(key));
            if (index == -1) return false;

            var newReceivers = new ReceiverRegistration<TKey, T>[_receivers.Length - 1];
            _receivers.AsSpan(0, index).CopyTo(newReceivers);
            if (index != _receivers.Length)
            {
                _receivers.AsSpan(index + 1).CopyTo(newReceivers.AsSpan(index));
            }
            _receivers = newReceivers;

            return true;
        }
    }

    public int Count
        => _receivers.Length;
}