using System.Buffers;

namespace Cysharp.Runtime.Multicast.Remoting;

public class PendingMessage
{
    private readonly Action<PendingMessage, IRemoteSerializer, ReadOnlyMemory<byte>> _trySetResult;
    private readonly Action<PendingMessage, Exception> _trySetException;
    private readonly IRemoteSerializer _serializer;

    public string MethodName { get; }
    public int MethodId { get; }
    public Guid MessageId { get; }
    public object TaskCompletionSource { get; }

    private PendingMessage(string methodName, int methodId, Guid messageId, object taskCompletionSource, IRemoteSerializer serializer, Action<PendingMessage, IRemoteSerializer, ReadOnlyMemory<byte>> trySetResult, Action<PendingMessage, Exception> trySetException)
    {
        MethodName = methodName;
        MethodId = methodId;
        MessageId = messageId;
        TaskCompletionSource = taskCompletionSource;
        _serializer = serializer;
        _trySetResult = trySetResult;
        _trySetException = trySetException;
    }

    public void TrySetResult(ReadOnlyMemory<byte> resultPayload)
        => _trySetResult(this, _serializer, resultPayload);

    public void TrySetException(Exception ex)
        => _trySetException(this, ex);

    public static PendingMessage Create<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, IRemoteSerializer serializer)
        => new(methodName, methodId, messageId, taskCompletionSource, serializer, Setter<TResult>.SetResult, Setter<TResult>.SetException);

    static class Setter<TResult>
    {
        public static readonly Action<PendingMessage, IRemoteSerializer, ReadOnlyMemory<byte>> SetResult
            = static (message, serializer, data) => ((TaskCompletionSource<TResult>)message.TaskCompletionSource).TrySetResult(
                serializer.DeserializeResponse<TResult>(new ReadOnlySequence<byte>(data), new SerializationContext(message.MethodName, message.MethodId, message.MessageId)));

        public static readonly Action<PendingMessage, Exception> SetException
            = static (message, ex) => ((TaskCompletionSource<TResult>)message.TaskCompletionSource).TrySetException(ex);
    }
};