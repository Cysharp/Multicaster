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

    public IMulticastAsyncGroup<T> GetOrAddGroup<T>(string name)
        => (IMulticastAsyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<T>(name, _proxyFactory, _serializer, _pendingTasks, Remove));

    public IMulticastSyncGroup<T> GetOrAddSynchronousGroup<T>(string name)
        => (IMulticastSyncGroup<T>)_groups.GetOrAdd((typeof(T), name), _ => new RemoteGroup<T>(name, _proxyFactory, _serializer, _pendingTasks, Remove));

    private void Remove<T>(RemoteGroup<T> group)
        => _groups.Remove((typeof(T), group.Name), out _);
}