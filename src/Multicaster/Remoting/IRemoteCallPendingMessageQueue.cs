using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteCallPendingMessageQueue
{
    void Enqueue(PendingMessage pendingMessage);
    bool TryDequeuePendingMessage(Guid messageId, [NotNullWhen(true)] out PendingMessage? pendingMessage);
    PendingMessage CreateMessage<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
    PendingMessage CreateMessage(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer);
}

public class RemoteCallPendingMessageQueue : IRemoteCallPendingMessageQueue
{
    private readonly ConcurrentDictionary<Guid, (PendingMessage Message, IDisposable CancelRegistration)> _pendingMessages = new();
    private readonly TimeSpan _timeout;

    public int Count => _pendingMessages.Count; // for unit tests

    public RemoteCallPendingMessageQueue(TimeSpan? timeout = default)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public PendingMessage CreateMessage<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingMessage.Create<TResult>(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken.CanBeCanceled ? timeoutCancellationToken : new CancellationTokenSource(_timeout).Token, serializer);

    public PendingMessage CreateMessage(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => PendingMessage.Create(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken.CanBeCanceled ? timeoutCancellationToken : new CancellationTokenSource(_timeout).Token, serializer);

    public void Enqueue(PendingMessage pendingMessage)
    {
        var registration = pendingMessage.TimeoutCancellationToken.Register(() =>
        {
            pendingMessage.TrySetCanceled(pendingMessage.TimeoutCancellationToken);
            _ = TryDequeuePendingMessage(pendingMessage.MessageId, out _);
        });
        _pendingMessages[pendingMessage.MessageId] = (pendingMessage, registration);
    }

    public bool TryDequeuePendingMessage(Guid messageId, [NotNullWhen(true)] out PendingMessage? pendingMessage)
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