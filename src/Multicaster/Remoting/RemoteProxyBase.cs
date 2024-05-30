using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Cysharp.Runtime.Multicast.Internal;

namespace Cysharp.Runtime.Multicast.Remoting;

public abstract class RemoteProxyBase : IRemoteProxy
{
    private readonly IRemoteReceiverWriter _writer;
    private readonly IRemoteSerializer _serializer;
    private readonly IRemoteClientResultPendingTaskRegistry _pendingTasks;

    protected RemoteProxyBase(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        _writer = writer;
        _serializer = serializer;
        _pendingTasks = pendingTasks;
    }

    bool IRemoteProxy.TryGetDirectWriter([NotNullWhen(true)] out IRemoteReceiverWriter? receiver)
        => (receiver = (_writer as RemoteDirectWriter)?.Writer) is not null;

    protected void Invoke(string name, int methodId)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }

    protected void Invoke<T1>(string name, int methodId, T1 arg1)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2>(string name, int methodId, T1 arg1, T2 arg2)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }

    protected Task InvokeWithResultNoReturnValue(string name, int methodId)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1>(string name, int methodId, T1 arg1)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2>(string name, int methodId, T1 arg1, T2 arg2)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }

    protected Task<TResult> InvokeWithResult<TResult>(string name, int methodId)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, TResult>(string name, int methodId, T1 arg1)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, TResult>(string name, int methodId, T1 arg1, T2 arg2)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(string name, int methodId, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }

    private (Task Task, Guid MessageId) EnqueuePendingTask(string name, int methodId, CancellationToken timeoutCancellationToken = default)
    {
        var messageId = Guid.NewGuid();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingTask = _pendingTasks.CreateTask(name, methodId, messageId, tcs, timeoutCancellationToken, _serializer);
        _pendingTasks.Register(pendingTask);
        return (tcs.Task, messageId);
    }

    private (Task<TResult> Task, Guid MessageId) EnqueuePendingTask<TResult>(string name, int methodId, CancellationToken timeoutCancellationToken = default)
    {
        var messageId = Guid.NewGuid();

        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingTask = _pendingTasks.CreateTask<TResult>(name, methodId, messageId, tcs, timeoutCancellationToken, _serializer);
        _pendingTasks.Register(pendingTask);
        return (tcs.Task, messageId);
    }

    private void ThrowIfNotSingleWriter()
    {
        if (_writer is not IRemoteSingleWriter)
        {
            throw new NotSupportedException("The client results method does not support multiple targets. Please use `Single` method.");
        }
    }

    internal class RemoteMultiWriter<TKey> : IRemoteReceiverWriter
        where TKey : IEquatable<TKey>
    {
        private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _remoteReceivers;
        private readonly ImmutableArray<TKey> _excludes;
        private readonly ImmutableArray<TKey>? _targets;

        public RemoteMultiWriter(ConcurrentDictionary<TKey, IRemoteReceiverWriter> remoteReceivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
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

    internal class RemoteSingleWriter<TKey> : IRemoteReceiverWriter, IRemoteSingleWriter
        where TKey : IEquatable<TKey>
    {
        private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _remoteReceivers;
        private readonly TKey _target;

        public RemoteSingleWriter(ConcurrentDictionary<TKey, IRemoteReceiverWriter> remoteReceivers, TKey target)
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