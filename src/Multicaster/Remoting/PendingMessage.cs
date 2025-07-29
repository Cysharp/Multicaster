using System.Buffers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cysharp.Runtime.Multicast.Remoting;

/// <summary>
/// Represents a task that is pending completion, typically used for managing asynchronous operations in a remote
/// communication context.
/// </summary>
public class PendingTask
{
    private readonly Func<PendingTask, IRemoteSerializer, ReadOnlyMemory<byte>, bool> _trySetResult;
    private readonly Func<PendingTask, Exception, bool> _trySetException;
    private readonly Func<PendingTask, CancellationToken, bool> _trySetCanceled;
    private readonly IRemoteSerializer _serializer;

    public string MethodName { get; }
    public int MethodId { get; }
    public Guid MessageId { get; }
    public object TaskCompletionSource { get; }
    public CancellationToken TimeoutCancellationToken { get; }

    private PendingTask(
        string methodName,
        int methodId,
        Guid messageId,
        object taskCompletionSource,
        CancellationToken timeoutCancellationToken,
        IRemoteSerializer serializer,
        Func<PendingTask, IRemoteSerializer, ReadOnlyMemory<byte>, bool> trySetResult,
        Func<PendingTask, Exception, bool> trySetException,
        Func<PendingTask, CancellationToken, bool> trySetCanceled)
    {
        MethodName = methodName;
        MethodId = methodId;
        MessageId = messageId;
        TaskCompletionSource = taskCompletionSource;
        TimeoutCancellationToken = timeoutCancellationToken;
        _serializer = serializer;
        _trySetResult = trySetResult;
        _trySetException = trySetException;
        _trySetCanceled = trySetCanceled;
    }

    public void TrySetResult(ReadOnlyMemory<byte> resultPayload)
        => _trySetResult(this, _serializer, resultPayload);

    public void TrySetException(Exception ex)
        => _trySetException(this, ex);

    public void TrySetCanceled(CancellationToken cancellationToken = default)
        => _trySetCanceled(this, cancellationToken);

    public static PendingTask Create<TResult>(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => new(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken, serializer, Setter<TResult>.TrySetResult, Setter<TResult>.TrySetException, Setter<TResult>.TrySetCanceled);

    public static PendingTask Create(string methodName, int methodId, Guid messageId, object taskCompletionSource, CancellationToken timeoutCancellationToken, IRemoteSerializer serializer)
        => new(methodName, methodId, messageId, taskCompletionSource, timeoutCancellationToken, serializer, Setter.TrySetResult, Setter.TrySetException, Setter.TrySetCanceled);

    static class Setter
    {
        public static readonly Func<PendingTask, IRemoteSerializer, ReadOnlyMemory<byte>, bool> TrySetResult
            = static (message, serializer, data) => ((TaskCompletionSource)message.TaskCompletionSource).TrySetResult();

        public static readonly Func<PendingTask, Exception, bool> TrySetException
            = static (message, ex) => ((TaskCompletionSource)message.TaskCompletionSource).TrySetException(ex);

        public static readonly Func<PendingTask, CancellationToken, bool> TrySetCanceled
            = static (message, ct) => ((TaskCompletionSource)message.TaskCompletionSource).TrySetCanceled(ct);
    }

    static class Setter<TResult>
    {
        public static readonly Func<PendingTask, IRemoteSerializer, ReadOnlyMemory<byte>, bool> TrySetResult
            = static (message, serializer, data) => ((TaskCompletionSource<TResult>)message.TaskCompletionSource).TrySetResult(
                serializer.DeserializeResult<TResult>(new ReadOnlySequence<byte>(data), new SerializationContext(message.MethodName, message.MethodId, message.MessageId))!);

        public static readonly Func<PendingTask, Exception, bool> TrySetException
            = static (message, ex) => ((TaskCompletionSource<TResult>)message.TaskCompletionSource).TrySetException(ex);

        public static readonly Func<PendingTask, CancellationToken, bool> TrySetCanceled
            = static (message, ct) => ((TaskCompletionSource<TResult>)message.TaskCompletionSource).TrySetCanceled(ct);
    }
};
