using System.Collections.Concurrent;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cysharp.Runtime.Multicast.Remoting;

public class RemoteOnlyGroupProvider : IMulticastGroupProvider
{
    private readonly ConcurrentDictionary<(Type Type, string name), object> _groups = new();
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteClientResultPendingTaskRegistry _pendingTasks;

    public RemoteOnlyGroupProvider(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _pendingTasks = pendingTasks;
    }

    /// <inheritdoc />
    public IMulticastAsyncGroup<TKey, T> GetOrAddGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<TKey, T>(name, _proxyFactory, _serializer, Remove));

    /// <inheritdoc />
    public IMulticastSyncGroup<TKey, T> GetOrAddSynchronousGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<TKey, T>(name, _proxyFactory, _serializer, Remove));

    private void Remove<TKey, T>(RemoteGroup<TKey, T> group)
        where TKey : IEquatable<TKey>
        => _groups.Remove((typeof(T), group.Name), out _);
}
