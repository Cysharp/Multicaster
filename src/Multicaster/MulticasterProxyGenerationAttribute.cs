using System;

namespace Cysharp.Runtime.Multicast;

/// <summary>
/// Marks a partial class for source generation of Multicaster proxy factories.
/// The generated class will implement IInMemoryProxyFactory and IRemoteProxyFactory
/// for the specified receiver interface types.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used to enable AOT (Ahead-of-Time) compilation support by generating
/// static proxy implementations at compile time instead of using dynamic code generation.
/// </para>
/// <para>
/// Usage example:
/// <code>
/// [MulticasterProxyGeneration(typeof(IChatReceiver), typeof(IGameReceiver))]
/// public partial class MyProxyFactory { }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MulticasterProxyGenerationAttribute : Attribute
{
    /// <summary>
    /// Gets the receiver interface types for which proxies will be generated.
    /// </summary>
    public Type[] ReceiverTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MulticasterProxyGenerationAttribute"/> class.
    /// </summary>
    /// <param name="receiverTypes">The receiver interface types for which proxies will be generated.</param>
    public MulticasterProxyGenerationAttribute(params Type[] receiverTypes)
    {
        ReceiverTypes = receiverTypes ?? Array.Empty<Type>();
    }
}
