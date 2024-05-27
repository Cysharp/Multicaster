using System.Diagnostics.CodeAnalysis;

using Cysharp.Runtime.Multicast.Remoting;

namespace Cysharp.Runtime.Multicast.Distributed.Redis;

public class NotSupportedRemoteClientResultPendingTaskRegistry : IRemoteClientResultPendingTaskRegistry
{
    public static IRemoteClientResultPendingTaskRegistry Instance { get; } = new NotSupportedRemoteClientResultPendingTaskRegistry();

    public void Register(PendingTask pendingTask)
        => throw new NotSupportedException($"{nameof(RedisGroupProvider)} does not support client results.");

    public bool TryGetAndUnregisterPendingTask(Guid messageId, [NotNullWhen(true)] out PendingTask? pendingTask)
        => throw new NotSupportedException($"{nameof(RedisGroupProvider)} does not support client results.");

    public PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => throw new NotSupportedException($"{nameof(RedisGroupProvider)} does not support client results.");

    public PendingTask CreateTask(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => throw new NotSupportedException($"{nameof(RedisGroupProvider)} does not support client results.");
}