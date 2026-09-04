namespace Cysharp.Runtime.Multicast;

/// <summary>
/// Specifies a receiver interface for which Multicaster source-generated proxies should be created.
/// </summary>
/// <remarks>
/// Apply this attribute one or more times to a non-generic, top-level partial class that is not file-local. The source
/// generator adds <c>InMemoryProxyFactory</c> and <c>RemoteProxyFactory</c> properties to that class.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MulticasterGenerationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MulticasterGenerationAttribute"/> class.
    /// </summary>
    /// <param name="receiverType">The receiver interface to generate proxies for.</param>
    public MulticasterGenerationAttribute(Type receiverType)
    {
        ReceiverType = receiverType;
    }

    /// <summary>
    /// Gets the receiver interface to generate proxies for.
    /// </summary>
    public Type ReceiverType { get; }
}
