namespace Cysharp.Runtime.Multicast;

/// <summary>
/// Represents a multicast group that allows sending messages to multiple receivers based on specified inclusion or
/// exclusion criteria.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify receivers. Must implement <see cref="IEquatable{T}"/>.</typeparam>
/// <typeparam name="TReceiver">The type of the receiver that messages are sent to.</typeparam>
public interface IMulticastGroup<TKey, TReceiver>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets a receiver that processes all messages without filtering.
    /// </summary>
    TReceiver All { get; }

    /// <summary>
    /// Returns a new instance of the receiver with elements excluded based on the specified keys.
    /// </summary>
    TReceiver Except(IEnumerable<TKey> excludes);

    /// <summary>
    /// Filters the current set of targets to include only the specified ones.
    /// </summary>
    TReceiver Only(IEnumerable<TKey> targets);

    /// <summary>
    /// Retrieves a single instance of <typeparamref name="TReceiver"/> associated with the specified <typeparamref name="TKey"/>.
    /// </summary>
    TReceiver Single(TKey target);
}

/// <summary>
/// Represents a multicast group that supports asynchronous operations for managing receivers.
/// </summary>
public interface IMulticastAsyncGroup<TKey, TReceiver> : IMulticastGroup<TKey, TReceiver>, IDisposable
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Adds a key and its associated receiver to the group asynchronously.
    /// </summary>
    ValueTask AddAsync(TKey key, TReceiver receiver, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the item associated with the specified key from the group asynchronously.
    /// </summary>
    ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the total number of items in the group asynchronously.
    /// </summary>
    ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a multicast group that supports synchronous operations for managing receivers.
/// </summary>
public interface IMulticastSyncGroup<TKey, TReceiver> : IMulticastGroup<TKey, TReceiver>, IDisposable
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Adds a key and its associated receiver to the group.
    /// </summary>
    void Add(TKey key, TReceiver receiver);

    /// <summary>
    /// Removes the item associated with the specified key from the group.
    /// </summary>
    void Remove(TKey key);

    /// <summary>
    /// Counts the total number of items in the group.
    /// </summary>
    int Count();
}

/// <summary>
/// Provides extension methods for working with multicast groups.
/// </summary>
public static class MulticastGroupExtensions
{
    /// <summary>
    /// Returns a new instance of the receiver with elements excluded based on the specified key.
    /// </summary>
    public static TReceiver Except<TKey, TReceiver>(this IMulticastGroup<TKey, TReceiver> group, TKey exclude)
        where TKey : IEquatable<TKey>
        => group.Except([exclude]);
}
