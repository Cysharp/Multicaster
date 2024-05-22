using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteCallPendingMessageQueue
{
    void Enqueue(Guid messageId, PendingMessage pendingMessage);
    bool TryGetPendingMessage(Guid messageId, [NotNullWhen(true)] out PendingMessage? pendingMessage);
}

public class RemoteCallPendingMessageQueue : IRemoteCallPendingMessageQueue
{
    private readonly ConcurrentDictionary<Guid, PendingMessage> _pendingMessages = new();

    public void Enqueue(Guid messageId, PendingMessage pendingMessage)
    {
        _pendingMessages[messageId] = pendingMessage;
    }

    public bool TryGetPendingMessage(Guid messageId, [NotNullWhen(true)] out PendingMessage? pendingMessage)
    {
        return _pendingMessages.TryGetValue(messageId, out pendingMessage);
    }
}