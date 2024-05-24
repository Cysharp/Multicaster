using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteClientResultPendingTaskRegistry
{
    void Register(PendingTask pendingMessage);
    bool TryGetAndUnregisterPendingTask(Guid messageId, [NotNullWhen(true)] out PendingTask? pendingMessage);
    PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
    PendingTask CreateTask(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
}

public class RemoteClientResultPendingTaskRegistry : IRemoteClientResultPendingTaskRegistry
{
    private readonly ConcurrentDictionary<Guid, (PendingTask Message, IDisposable CancelRegistration)> _pendingMessages = new();
    private readonly TimeSpan _timeout;

    public int Count => _pendingMessages.Count; // for unit tests

    public RemoteClientResultPendingTaskRegistry(TimeSpan? timeout = default)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public PendingTask CreateTask<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingTask.Create<TResult>(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken.CanBeCanceled ? timeoutCancellationToken : new CancellationTokenSource(_timeout).Token, serializer);

    public PendingTask CreateTask(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingTask.Create(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken.CanBeCanceled ? timeoutCancellationToken : new CancellationTokenSource(_timeout).Token, serializer);

    public void Register(PendingTask pendingMessage)
    {
        var registration = pendingMessage.TimeoutCancellationToken.Register(() =>
        {
            pendingMessage.TrySetCanceled(pendingMessage.TimeoutCancellationToken);
            _ = TryGetAndUnregisterPendingTask(pendingMessage.MessageId, out _);
        });
        _pendingMessages[pendingMessage.MessageId] = (pendingMessage, registration);
    }

    public bool TryGetAndUnregisterPendingTask(Guid messageId, [NotNullWhen(true)] out PendingTask? pendingMessage)
    {
        var removed = _pendingMessages.TryRemove(messageId, out var messageAndCancelRegistration);
        if (removed)
        {
            messageAndCancelRegistration.CancelRegistration.Dispose();
            pendingMessage = messageAndCancelRegistration.Message;
        }
        else
        {
            pendingMessage = null;
        }
        return removed;
    }
}