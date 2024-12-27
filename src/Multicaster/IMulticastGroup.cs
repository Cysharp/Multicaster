namespace Cysharp.Runtime.Multicast;

public interface IMulticastGroup<TKey, TReceiver>
    where TKey : IEquatable<TKey>
{
    TReceiver All { get; }
    TReceiver Except(IEnumerable<TKey> excludes);
    TReceiver Only(IEnumerable<TKey> targets);
    TReceiver Single(TKey target);
}

public interface IMulticastAsyncGroup<TKey, TReceiver> : IMulticastGroup<TKey, TReceiver>, IDisposable
    where TKey : IEquatable<TKey>
{
    ValueTask AddAsync(TKey key, TReceiver receiver, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);
    ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IMulticastSyncGroup<TKey, TReceiver> : IMulticastGroup<TKey, TReceiver>, IDisposable
    where TKey : IEquatable<TKey>
{
    void Add(TKey key, TReceiver receiver);
    void Remove(TKey key);
    int Count();
}

public static class MulticastGroupExtensions
{
    public static TReceiver Except<TKey, TReceiver>(this IMulticastGroup<TKey, TReceiver> group, TKey exclude)
        where TKey : IEquatable<TKey>
        => group.Except([exclude]);
}