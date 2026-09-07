using System.Collections.Immutable;

namespace Cysharp.Runtime.Multicast.InMemory;

/// <summary>
/// Combines multiple in-memory proxy factories and optionally delegates missing receiver types to a fallback factory.
/// </summary>
public sealed class CompositeInMemoryProxyFactory : IInMemoryProxyFactory
{
    private readonly IReadOnlyList<IInMemoryProxyFactory> _factories;
    private readonly IInMemoryProxyFactory? _fallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeInMemoryProxyFactory"/> class.
    /// </summary>
    /// <param name="factories">Factories queried through <see cref="IInMemoryProxyFactory.TryCreate{TKey,T}"/> in order.</param>
    /// <param name="fallback">An optional factory whose <c>Create</c> method is called when no queried factory supports the receiver type.</param>
    public CompositeInMemoryProxyFactory(IEnumerable<IInMemoryProxyFactory> factories, IInMemoryProxyFactory? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(factories);
        _factories = factories.ToArray();
        _fallback = fallback;
    }

    /// <inheritdoc />
    public T Create<TKey, T>(IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets) where TKey : IEquatable<TKey>
    {
        if (TryCreate(receivers, excludes, targets, out T proxy))
        {
            return proxy;
        }

        if (_fallback is not null)
        {
            return _fallback.Create(receivers, excludes, targets);
        }

        throw new InvalidOperationException(
            $"No Multicaster in-memory proxy factory supports receiver type '{typeof(T)}'. " +
            "Add [MulticasterGeneration(typeof(TReceiver))] to a partial class, or configure an explicit JIT-only fallback factory.");
    }

    /// <inheritdoc />
    public bool TryCreate<TKey, T>(IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets, out T proxy) where TKey : IEquatable<TKey>
    {
        foreach (var factory in _factories)
        {
            if (factory.TryCreate(receivers, excludes, targets, out proxy))
            {
                return true;
            }
        }

        proxy = default!;
        return false;
    }
}
