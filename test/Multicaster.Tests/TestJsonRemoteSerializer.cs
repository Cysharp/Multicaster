﻿using System.Buffers;
using System.Text.Json;

using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

class TestJsonRemoteSerializer : IRemoteSerializer
{
    public int SerializeInvocationCallCount;

    public record SerializedInvocation(string MethodName, int MethodId, Guid? MessageId, IReadOnlyList<object?> Arguments);

    private void SerializeInvocationCore(IBufferWriter<byte> writer, IReadOnlyList<object?> args, in SerializationContext ctx)
    {
        var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, new SerializedInvocation(ctx.MethodName, ctx.MethodId, ctx.MessageId, args));
        jsonWriter.Flush();

        Interlocked.Increment(ref SerializeInvocationCallCount);
    }

    public void SerializeInvocation(IBufferWriter<byte> writer, in SerializationContext ctx)
        => SerializeInvocationCore(writer, Array.Empty<object?>(), ctx);

    public void SerializeInvocation<T1>(IBufferWriter<byte> writer, T1 arg1, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1], ctx);

    public void SerializeInvocation<T1, T2>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2], ctx);

    public void SerializeInvocation<T1, T2, T3>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3], ctx);

    public void SerializeInvocation<T1, T2, T3, T4>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13,
        in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13,
        T14 arg14, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14], ctx);

    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13,
        T14 arg14, T15 arg15, in SerializationContext ctx)
        => SerializeInvocationCore(writer, [arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15], ctx);

    public T? DeserializeResult<T>(ReadOnlySequence<byte> data, in SerializationContext ctx)
    {
        var reader = new Utf8JsonReader(data);
        return JsonSerializer.Deserialize<T>(ref reader);
    }
}