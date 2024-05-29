using System.Collections.Concurrent;

namespace Cysharp.Runtime.Multicast;

public class KeyMappedAsyncGroup<TKey, TReceiver>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Guid> _keyMapping = new();
    private readonly IMulticastAsyncGroup<TReceiver> _underlyingGroup;

    public KeyMappedAsyncGroup(IMulticastAsyncGroup<TReceiver> underlyingGroup)
    {
        _underlyingGroup = underlyingGroup;
    }

    public TReceiver All => _underlyingGroup.All;

    public TReceiver Except(IReadOnlyList<TKey> excludes) =>
        _underlyingGroup.Except([
            ..excludes
                .Select(x => (_keyMapping.TryGetValue(x, out var receiverKey), receiverKey))
                .Where(x => x.Item1)
                .Select(x => x.receiverKey)
        ]);

    public TReceiver Only(IReadOnlyList<TKey> targets)
        => _underlyingGroup.Only([
            ..targets
                .Select(x => (_keyMapping.TryGetValue(x, out var receiverKey), receiverKey))
                .Where(x => x.Item1)
                .Select(x => x.receiverKey)
        ]);

    public TReceiver Single(TKey target)
        => _underlyingGroup.Single(_keyMapping.TryGetValue(target, out var receiverKey) ? receiverKey : Guid.Empty);

    public void Dispose()
        => _underlyingGroup.Dispose();

    public ValueTask AddAsync(TKey key, TReceiver receiver, CancellationToken cancellationToken = default)
    {
        var guid = _keyMapping[key] = Guid.NewGuid();
        return _underlyingGroup.AddAsync(guid, receiver, cancellationToken);
    }

    public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (_keyMapping.TryRemove(key, out var receiverKey))
        {
            return _underlyingGroup.RemoveAsync(receiverKey, cancellationToken);
        }

        return default;
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => _underlyingGroup.CountAsync(cancellationToken);
}

public class KeyMappedSyncGroup<TKey, TReceiver> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Guid> _keyMapping = new();
    private readonly IMulticastSyncGroup<TReceiver> _underlyingGroup;

    public KeyMappedSyncGroup(IMulticastSyncGroup<TReceiver> underlyingGroup)
    {
        _underlyingGroup = underlyingGroup;
    }

    public TReceiver All => _underlyingGroup.All;

    public TReceiver Except(IReadOnlyList<TKey> excludes) =>
        _underlyingGroup.Except([
            ..excludes
                .Select(x => (_keyMapping.TryGetValue(x, out var receiverKey), receiverKey))
                .Where(x => x.Item1)
                .Select(x => x.receiverKey)
        ]);

    public TReceiver Only(IReadOnlyList<TKey> targets)
        => _underlyingGroup.Only([
            ..targets
                .Select(x => (_keyMapping.TryGetValue(x, out var receiverKey), receiverKey))
                .Where(x => x.Item1)
                .Select(x => x.receiverKey)
        ]);

    public TReceiver Single(TKey target)
        => _underlyingGroup.Single(_keyMapping.TryGetValue(target, out var receiverKey) ? receiverKey : Guid.Empty);

    public void Dispose()
        => _underlyingGroup.Dispose();

    public void Add(TKey key, TReceiver receiver)
    {
        var guid = _keyMapping[key] = Guid.NewGuid();
        _underlyingGroup.Add(guid, receiver);
    }

    public void Remove(TKey key)
    {
        if (_keyMapping.TryRemove(key, out var receiverKey))
        {
            _underlyingGroup.Remove(receiverKey);
        }
    }

    public int Count()
        => _underlyingGroup.Count();
}
