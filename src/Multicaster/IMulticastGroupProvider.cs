namespace Cysharp.Runtime.Multicast;

/// <summary>
/// Provides functionality to manage and retrieve multicast groups, supporting both asynchronous and synchronous communication patterns.
/// </summary>
public interface IMulticastGroupProvider
{
    /// <summary>
    /// Retrieves an existing multicast group by name or creates a new one if it does not exist.
    /// </summary>
    IMulticastAsyncGroup<TKey, TReceiver> GetOrAddGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>;

    /// <summary>
    /// Retrieves an existing synchronous multicast group by name or creates a new one if it does not exist.
    /// </summary>
    IMulticastSyncGroup<TKey, TReceiver> GetOrAddSynchronousGroup<TKey, TReceiver>(string name)
        where TKey : IEquatable<TKey>;
}
