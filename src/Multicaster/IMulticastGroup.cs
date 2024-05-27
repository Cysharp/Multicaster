using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast;

public interface IMulticastGroup<TReceiver>
{
    TReceiver All { get; }
    TReceiver Except(ImmutableArray<Guid> excludes);
    TReceiver Only(ImmutableArray<Guid> targets);
    TReceiver Single(Guid target);
}

public interface IMulticastAsyncGroup<TReceiver> : IMulticastGroup<TReceiver>, IDisposable
{
    ValueTask AddAsync(Guid key, TReceiver receiver, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(Guid key, CancellationToken cancellationToken = default);
    ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IMulticastSyncGroup<TReceiver> : IMulticastGroup<TReceiver>, IDisposable
{
    void Add(Guid key, TReceiver receiver);
    void Remove(Guid key);
    int Count();
}

public static class MulticastGroupExtensions
{
    public static TReceiver Except<TReceiver>(this IMulticastGroup<TReceiver> group, IReadOnlyList<Guid> excludes)
        => group.Except([..excludes]);
    public static TReceiver Only<TReceiver>(this IMulticastGroup<TReceiver> group, IReadOnlyList<Guid> targets)
        => group.Only([..targets]);
}