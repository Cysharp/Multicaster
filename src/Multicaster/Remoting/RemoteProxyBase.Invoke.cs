#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Cysharp.Runtime.Multicast.Internal;

namespace Cysharp.Runtime.Multicast.Remoting;

public partial class RemoteProxyBase
{
    protected void Invoke<T1>(MethodInvocationContext ctx, T1 arg1)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2>(MethodInvocationContext ctx, T1 arg1, T2 arg2)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, new SerializationContext(ctx.MethodName, ctx.MethodId, null));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
    }

    protected Task InvokeWithResultNoReturnValue<T1>(MethodInvocationContext ctx, T1 arg1, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2>(MethodInvocationContext ctx, T1 arg1, T2 arg2, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }
    protected Task InvokeWithResultNoReturnValue<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, null, writer.WrittenMemory));
        return task;
    }

    protected Task<TResult> InvokeWithResult<T1, TResult>(MethodInvocationContext ctx, T1 arg1, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
    protected Task<TResult> InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(MethodInvocationContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, CancellationToken cancellationToken)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(ctx.MethodName, ctx.MethodId, cancellationToken);
        _serializer.SerializeInvocation(writer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, new SerializationContext(ctx.MethodName, ctx.MethodId, messageId));
        _writer.Write(new InvocationWriteContext(ctx, messageId, writer.WrittenMemory));
        return task;
    }
}
