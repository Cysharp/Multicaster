using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteClientResultPendingTaskRegistry
{
    void Register(PendingTask pendingTask);
    bool TryGetAndUnregisterPendingTask(Guid messageId, [NotNullWhen(true)] out PendingTask? pendingTask);
    PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
    PendingTask CreateTask(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
}

public class RemoteClientResultPendingTaskRegistry : IRemoteClientResultPendingTaskRegistry
{
    private readonly ConcurrentDictionary<Guid, (PendingTask Task, IDisposable CancelRegistration)> _pendingTasks = new();
    private readonly TimeSpan _timeout;

    public int Count => _pendingTasks.Count; // for unit tests

    public RemoteClientResultPendingTaskRegistry(TimeSpan? timeout = default)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingTask.Create<TResult>(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken.CanBeCanceled ? timeoutCancellationToken : new CancellationTokenSource(_timeout).Token, serializer);

    public PendingTask CreateTask(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingTask.Create(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken.CanBeCanceled ? timeoutCancellationToken : new CancellationTokenSource(_timeout).Token, serializer);

    public void Register(PendingTask pendingTask)
    {
        var registration = pendingTask.TimeoutCancellationToken.Register(() =>
        {
            pendingTask.TrySetCanceled(pendingTask.TimeoutCancellationToken);
            _ = TryGetAndUnregisterPendingTask(pendingTask.MessageId, out _);
        });
        _pendingTasks[pendingTask.MessageId] = (pendingTask, registration);
    }

    public bool TryGetAndUnregisterPendingTask(Guid messageId, [NotNullWhen(true)] out PendingTask? pendingTask)
    {
        var removed = _pendingTasks.TryRemove(messageId, out var taskAndCancelRegistration);
        if (removed)
        {
            taskAndCancelRegistration.CancelRegistration.Dispose();
            pendingTask = taskAndCancelRegistration.Task;
        }
        else
        {
            pendingTask = null;
        }
        return removed;
    }
}