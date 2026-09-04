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

    /// <summary>
    /// Attempts to create a remote proxy for the specified receiver interface.
    /// </summary>
    /// <typeparam name="T">The receiver interface type.</typeparam>
    /// <param name="receiver">The remote receiver writer.</param>
    /// <param name="serializer">The remote invocation serializer.</param>
    /// <param name="proxy">The created proxy when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when this factory supports <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
    bool TryCreate<T>(IRemoteReceiverWriter receiver, IRemoteSerializer serializer, out T proxy)
    {
        proxy = default!;
        return false;
    }
}

public static class RemoteProxyFactory
{
    public static T CreateDirect<T>(this IRemoteProxyFactory factory, IRemoteReceiverWriter receiver, IRemoteSerializer serializer)
        => factory.Create<T>(new RemoteProxyBase.RemoteDirectWriter(receiver), serializer);

    public static T Create<TKey, T>(this IRemoteProxyFactory factory, ConcurrentDictionary<TKey, IRemoteReceiverWriter> receivers, IRemoteSerializer serializer) where TKey : IEquatable<TKey>
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
