using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public abstract class RemoteProxyBase : IRemoteProxy
{
    private readonly IRemoteReceiverWriter _writer;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteCallPendingMessageQueue _pendingQueue;

    protected RemoteProxyBase(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
    {
        _writer = writer;
        _serializer = serializer;
        _pendingQueue = pendingQueue;
    }

    bool IRemoteProxy.TryGetDirectWriter([NotNullWhen(true)] out IRemoteReceiverWriter? receiver)
        => (receiver = (_writer as RemoteDirectWriter)?.Writer) is not null;

    protected void Invoke<T>(string name, int methodId, T arg1)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2>(string name, int methodId, T1 arg1, T2 arg2)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
    {
        var writer = new ArrayBufferWriter<byte>();
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }

    protected Task<TResult> InvokeWithResponse<T1, TResult>(string name, int methodId, T1 arg1)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, TResult>(string name, int methodId, T1 arg1, T2 arg2)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
    {
        ThrowIfNotSingleWriter();
        var writer = new ArrayBufferWriter<byte>();
        var (task, messageId) = EnqueuePendingMessage<TResult>(name, methodId);
        _serializer.SerializeArgument(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }

    private (Task<TResult> Task, Guid MessageId) EnqueuePendingMessage<TResult>(string name, int methodId)
    {
        var messageId = Guid.NewGuid();

        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingMessage = PendingMessage.Create<TResult>(name, methodId, messageId, tcs, _serializer);
        _pendingQueue.Enqueue(messageId, pendingMessage);
        return (tcs.Task, messageId);
    }

    private void ThrowIfNotSingleWriter()
    {
        if (_writer is not IRemoteSingleWriter)
        {
            throw new NotSupportedException("The client results method does not support multiple targets. Please use `Single` method.");
        }
    }

    internal class RemoteMultiWriter : IRemoteReceiverWriter
    {
        private readonly ConcurrentDictionary<Guid, IRemoteReceiverWriter> _remoteReceivers;
        private readonly ImmutableArray<Guid> _excludes;
        private readonly ImmutableArray<Guid>? _targets;

        public RemoteMultiWriter(ConcurrentDictionary<Guid, IRemoteReceiverWriter> remoteReceivers, ImmutableArray<Guid> excludes, ImmutableArray<Guid>? targets)
        {
            _remoteReceivers = remoteReceivers;
            _excludes = excludes;
            _targets = targets;
        }

        void IRemoteReceiverWriter.Write(ReadOnlyMemory<byte> payload)
        {
            foreach (var (receiverId, receiver) in _remoteReceivers)
            {
                if (_excludes.Contains(receiverId)) continue;
                if (_targets is not null && !_targets.Contains(receiverId)) continue;
                try
                {
                    receiver.Write(payload);
                }
                catch
                {
                    // Ignore
                }
            }
        }
    }

    internal class RemoteSingleWriter : IRemoteReceiverWriter, IRemoteSingleWriter
    {
        private readonly ConcurrentDictionary<Guid, IRemoteReceiverWriter> _remoteReceivers;
        private readonly Guid _target;

        public RemoteSingleWriter(ConcurrentDictionary<Guid, IRemoteReceiverWriter> remoteReceivers, Guid target)
        {
            _remoteReceivers = remoteReceivers;
            _target = target;
        }

        void IRemoteReceiverWriter.Write(ReadOnlyMemory<byte> payload)
        {
            try
            {
                _remoteReceivers.GetValueOrDefault(_target)?.Write(payload);
            }
            catch
            {
                // Ignore
            }
        }
    }

    internal class RemoteDirectWriter : IRemoteReceiverWriter, IRemoteSingleWriter
    {
        public IRemoteReceiverWriter Writer { get; }

        public RemoteDirectWriter(IRemoteReceiverWriter writer)
        {
            Writer = writer;
        }

        void IRemoteReceiverWriter.Write(ReadOnlyMemory<byte> payload)
        {
            try
            {
                Writer.Write(payload);
            }
            catch
            {
                // Ignore
            }
        }
    }
}