using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteProxyFactory
{
    T Create<T>(IRemoteReceiverWriter receiver, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks);
}

public static class RemoteProxyFactory
{
    public static T CreateDirect<T>(this IRemoteProxyFactory factory, IRemoteReceiverWriter receiver, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
        => factory.Create<T>(new RemoteProxyBase.RemoteDirectWriter(receiver), serializer, pendingTasks);

    public static T Create<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter<TKey>(receivers, ImmutableArray<TKey>.Empty, null), serializer, pendingTasks);

    public static T Except<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, ImmutableArray<TKey> excludes, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter<TKey>(receivers, excludes, null), serializer, pendingTasks);

    public static T Only<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, ImmutableArray<TKey> targets, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter<TKey>(receivers, ImmutableArray<TKey>.Empty, targets), serializer, pendingTasks);

    public static T Single<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, TKey target, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteSingleWriter<TKey>(receivers, target), serializer, pendingTasks);
}