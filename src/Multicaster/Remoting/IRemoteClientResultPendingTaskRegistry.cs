using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteClientResultPendingTaskRegistry : IDisposable
{
    void Register(PendingTask pendingTask);
    bool TryGetAndUnregisterPendingTask(Guid messageId, [NotNullWhen(true)] out PendingTask? pendingTask);
    PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, TaskCompletionSource<TResult> taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
    PendingTask CreateTask(string methodName, int methodId, Guid messageId, TaskCompletionSource taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
}

public class RemoteClientResultPendingTaskRegistry : IRemoteClientResultPendingTaskRegistry
{
    private readonly ConcurrentDictionary<Guid, (PendingTask Task, IDisposable CancelRegistration)> _pendingTasks = new();
    private readonly TimeSpan _timeout;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public int Count => _pendingTasks.Count; // for unit tests

    public RemoteClientResultPendingTaskRegistry(TimeSpan? timeout = default, TimeProvider? timeProvider = default)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, TaskCompletionSource<TResult> taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingTask.Create<TResult>(methodName, methodId, messageId, taskCompletionSource, CreateTimeoutCancellationToken(timeoutCancellationToken), serializer);

    public PendingTask CreateTask(string methodName, int methodId, Guid messageId, TaskCompletionSource taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingTask.Create(methodName, methodId, messageId, taskCompletionSource, CreateTimeoutCancellationToken(timeoutCancellationToken), serializer);

    private CancellationToken CreateTimeoutCancellationToken(CancellationToken timeoutCancellationToken)
    {
        return timeoutCancellationToken.CanBeCanceled
            ? timeoutCancellationToken
#if NET8_0_OR_GREATER
            : new CancellationTokenSource(_timeout, _timeProvider).Token;
#else
            : new CancellationTokenSource(_timeout).Token;
#endif
    }

    public void Register(PendingTask pendingTask)
    {
        if (_disposed)
        {
            pendingTask.TrySetCanceled();
            return;
        }
        
        var registration = pendingTask.TimeoutCancellationToken.Register(() =>
        {
            pendingTask.TrySetException(new TaskCanceledException("The operation has timed out.", new TimeoutException(), pendingTask.TimeoutCancellationToken));
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

    public void Dispose()
    {
        _disposed = true;

        DisposeAll:
        foreach (var pendingTask in _pendingTasks)
        {
            if (_pendingTasks.TryRemove(pendingTask.Key, out _))
            {
                pendingTask.Value.CancelRegistration.Dispose();
                pendingTask.Value.Task.TrySetCanceled();
            }
        }

        if (!_pendingTasks.IsEmpty)
        {
            goto DisposeAll;
        }
    }
}