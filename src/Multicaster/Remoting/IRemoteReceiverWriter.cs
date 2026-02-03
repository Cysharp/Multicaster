namespace Cysharp.Runtime.Multicast.Remoting;

/// <summary>
/// Defines functionality for writing data to a remote receiver and managing pending tasks.
/// </summary>
public interface IRemoteReceiverWriter
{
    /// <summary>
    /// Writes the specified payload to the underlying receiver target.
    /// </summary>
    void Write(InvocationWriteContext context);

    /// <summary>
    /// Gets the registry of pending tasks associated with the remote client.
    /// </summary>
    IRemoteClientResultPendingTaskRegistry PendingTasks { get; }
}


/// <summary>
/// Provides information about a method invocation, including the method name and identifier.
/// </summary>
/// <param name="MethodName">Gets the name of the method being invoked.</param>
/// <param name="MethodId">Gets the identifier of the method being invoked.</param>
public readonly record struct MethodInvocationContext(string MethodName, int MethodId);

/// <summary>
/// Represents the context information required to write a method invocation, including the method name, identifier,
/// optional message identifier, and associated payload data.
/// </summary>
/// <remarks>Use this struct to encapsulate all relevant details when logging or processing a remote method
/// invocation. The optional <c>MessageId</c> can be used to correlate requests and responses, while the <c>Payload</c>
/// contains the serialized data for the invocation.</remarks>
/// <param name="MethodId">The unique identifier for the method being invoked.</param>
/// <param name="MessageId">An optional identifier for the message associated with the invocation. May be <see langword="null"/> if not applicable.</param>
/// <param name="Payload">The payload data associated with the invocation, represented as a read-only memory buffer.</param>
public readonly record struct InvocationWriteContext(int MethodId, Guid? MessageId, ReadOnlyMemory<byte> Payload)
{
    /// <summary>
    /// Initializes a new instance of the InvocationWriteContext class using the specified method invocation context,
    /// message identifier, and payload.
    /// </summary>
    public InvocationWriteContext(MethodInvocationContext methodInvocationContext, Guid? messageId, ReadOnlyMemory<byte> Payload)
        : this(methodInvocationContext.MethodId, messageId, Payload)
    {
    }
}
