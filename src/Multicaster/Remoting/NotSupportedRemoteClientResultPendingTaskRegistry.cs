using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public class NotSupportedRemoteClientResultPendingTaskRegistry : IRemoteClientResultPendingTaskRegistry
{
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