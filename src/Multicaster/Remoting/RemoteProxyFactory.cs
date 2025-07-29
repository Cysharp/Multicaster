using System.Collections.Concurrent;
using System.Collections.Immutable;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cysharp.Runtime.Multicast.Remoting;

/// <summary>
/// Defines a factory for creating remote proxy instances that communicate with a remote receiver.
/// </summary>
public interface IRemoteProxyFactory
{
    /// <summary>
    /// Creates an instance of the specified type <typeparamref name="T"/> using the provided remote receiver and serializer.
    /// </summary>
    T Create<T>(IRemoteReceiverWriter receiver, IRemoteSerializer serializer);
}

public static class RemoteProxyFactory
{
    public static T CreateDirect<T>(this IRemoteProxyFactory factory, IRemoteReceiverWriter receiver, IRemoteSerializer serializer)
        => factory.Create<T>(new RemoteProxyBase.RemoteDirectWriter(receiver), serializer);

    public static T Create<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, IRemoteSerializer serializer)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter<TKey>(receivers, ImmutableArray<TKey>.Empty, null), serializer);

    public static T Except<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, ImmutableArray<TKey> excludes, IRemoteSerializer serializer)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter<TKey>(receivers, excludes, null), serializer);

    public static T Only<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, ImmutableArray<TKey> targets, IRemoteSerializer serializer)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteMultiWriter<TKey>(receivers, ImmutableArray<TKey>.Empty, targets), serializer);

    public static T Single<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, TKey target, IRemoteSerializer serializer)
        where TKey : IEquatable<TKey>
        => factory.Create<T>(new RemoteProxyBase.RemoteSingleWriter<TKey>(receivers, target), serializer);
}
