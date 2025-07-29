namespace Cysharp.Runtime.Multicast.Remoting;

/// <summary>
/// Defines functionality for writing data to a remote receiver and managing pending tasks.
/// </summary>
public interface IRemoteReceiverWriter
{
    /// <summary>
    /// Writes the specified payload to the underlying receiver target.
    /// </summary>
    void Write(ReadOnlyMemory<byte> payload);

    /// <summary>
    /// Gets the registry of pending tasks associated with the remote client.
    /// </summary>
    IRemoteClientResultPendingTaskRegistry PendingTasks { get; }
}
