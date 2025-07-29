using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cysharp.Runtime.Multicast.Remoting;

/// <summary>
/// Represents a registry for managing pending tasks related to remote client results that is not supported.
/// </summary>
public class NotSupportedRemoteClientResultPendingTaskRegistry : IRemoteClientResultPendingTaskRegistry
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="IRemoteClientResultPendingTaskRegistry"/> implementation.
    /// </summary>
    public static IRemoteClientResultPendingTaskRegistry Instance { get; } = new NotSupportedRemoteClientResultPendingTaskRegistry();

    public void Register(PendingTask pendingTask)
        => throw new NotSupportedException("The group does not support client results.");

    public bool TryGetAndUnregisterPendingTask(Guid messageId, [NotNullWhen(true)] out PendingTask? pendingTask)
        => throw new NotSupportedException("The group does not support client results.");

    public PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, TaskCompletionSource<TResult> taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => throw new NotSupportedException("The group does not support client results.");

    public PendingTask CreateTask(string methodName, int methodId, Guid messageId, TaskCompletionSource taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => throw new NotSupportedException("The group does not support client results.");

    public void Dispose()
    {
    }
}
