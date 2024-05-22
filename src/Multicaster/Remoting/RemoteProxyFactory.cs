using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteProxyFactory
{
    T Create<T>(IRemoteReceiverWriter receiver, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue);
}

public static class RemoteProxyFactory
{
    public static T CreateDirect<T>(this IRemoteProxyFactory factory, IRemoteReceiverWriter receiver, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
        => factory.Create<T>(new RemoteProxyBase.RemoteDirectWriter(receiver), serializer, pendingQueue);

    public static T Create<T>(this IRemoteProxyFactory factory, ConcurrentDictionary<Guid, IRemoteReceiverWriter> receivers, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter(receivers, ImmutableArray<Guid>.Empty, null), serializer, pendingQueue);

    public static T Except<T>(this IRemoteProxyFactory factory, ConcurrentDictionary<Guid, IRemoteReceiverWriter> receivers, ImmutableArray<Guid> excludes, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter(receivers, excludes, null), serializer, pendingQueue);

    public static T Only<T>(this IRemoteProxyFactory factory, ConcurrentDictionary<Guid, IRemoteReceiverWriter> receivers, ImmutableArray<Guid> targets, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter(receivers, ImmutableArray<Guid>.Empty, targets), serializer, pendingQueue);

    public static T Single<T>(this IRemoteProxyFactory factory, ConcurrentDictionary<Guid, IRemoteReceiverWriter> receivers, Guid target, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
        => factory.Create<T>(new RemoteProxyBase.RemoteSingleWriter(receivers, target), serializer, pendingQueue);
}