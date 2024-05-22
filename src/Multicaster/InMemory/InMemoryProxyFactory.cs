using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.InMemory;

public interface IInMemoryProxyFactory
{
    T Create<T>(IEnumerable<KeyValuePair<Guid, T>> receivers, ImmutableArray<Guid> excludes, ImmutableArray<Guid>? targets);
}

public static class InMemoryProxyFactory
{
    public static T Create<T>(this IInMemoryProxyFactory proxyFactory, IEnumerable<KeyValuePair<Guid, T>> receivers)
        => proxyFactory.Create(receivers, ImmutableArray<Guid>.Empty, null);

    public static T Except<T>(this IInMemoryProxyFactory proxyFactory, IEnumerable<KeyValuePair<Guid, T>> receivers, ImmutableArray<Guid> excludes)
        => proxyFactory.Create(receivers, excludes, null);

    public static T Only<T>(this IInMemoryProxyFactory proxyFactory, IEnumerable<KeyValuePair<Guid, T>> receivers, ImmutableArray<Guid> targets)
        => proxyFactory.Create(receivers, ImmutableArray<Guid>.Empty, targets);
}