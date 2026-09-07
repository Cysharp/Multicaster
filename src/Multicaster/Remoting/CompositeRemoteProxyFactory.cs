namespace Cysharp.Runtime.Multicast.Remoting;

/// <summary>
/// Combines multiple remote proxy factories and optionally delegates missing receiver types to a fallback factory.
/// </summary>
public sealed class CompositeRemoteProxyFactory : IRemoteProxyFactory
{
    private readonly IReadOnlyList<IRemoteProxyFactory> _factories;
    private readonly IRemoteProxyFactory? _fallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeRemoteProxyFactory"/> class.
    /// </summary>
    /// <param name="factories">Factories queried through <see cref="IRemoteProxyFactory.TryCreate{T}"/> in order.</param>
    /// <param name="fallback">An optional factory whose <c>Create</c> method is called when no queried factory supports the receiver type.</param>
    public CompositeRemoteProxyFactory(IEnumerable<IRemoteProxyFactory> factories, IRemoteProxyFactory? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(factories);
        _factories = factories.ToArray();
        _fallback = fallback;
    }

    /// <inheritdoc />
    public T Create<T>(IRemoteReceiverWriter receiver, IRemoteSerializer serializer)
    {
        if (TryCreate(receiver, serializer, out T proxy))
        {
            return proxy;
        }

        if (_fallback is not null)
        {
            return _fallback.Create<T>(receiver, serializer);
        }

        throw new InvalidOperationException(
            $"No Multicaster remote proxy factory supports receiver type '{typeof(T)}'. " +
            "Add [MulticasterGeneration(typeof(TReceiver))] to a partial class, or configure an explicit JIT-only fallback factory.");
    }

    /// <inheritdoc />
    public bool TryCreate<T>(IRemoteReceiverWriter receiver, IRemoteSerializer serializer, out T proxy)
    {
        foreach (var factory in _factories)
        {
            if (factory.TryCreate(receiver, serializer, out proxy))
            {
                return true;
            }
        }

        proxy = default!;
        return false;
    }
}
