using System.Collections.Concurrent;

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

    public IMulticastAsyncGroup<TKey, T> GetOrAddGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastAsyncGroup<TKey, T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<TKey, T>(name, _proxyFactory, _serializer, _pendingTasks, Remove));

    public IMulticastSyncGroup<TKey, T> GetOrAddSynchronousGroup<TKey, T>(string name)
        where TKey : IEquatable<TKey>
        => (IMulticastSyncGroup<TKey, T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<TKey, T>(name, _proxyFactory, _serializer, _pendingTasks, Remove));

    private void Remove<TKey, T>(RemoteGroup<TKey, T> group)
        where TKey : IEquatable<TKey>
        => _groups.Remove((typeof(T), group.Name), out _);
}