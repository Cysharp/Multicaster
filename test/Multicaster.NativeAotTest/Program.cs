using System.Buffers;
using System.Collections.Immutable;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.NativeAotTest;

[MulticasterGeneration(typeof(INativeAotReceiver))]
internal partial class NativeAotGeneratedMulticaster
{
}

internal interface IIntResultReceiver
{
    [MethodId(201)]
    Task<int> QueryAsync();
}

internal interface IStringResultReceiver
{
    [MethodId(202)]
    Task<string> QueryAsync();
}

internal sealed class MethodIdAttribute(int methodId) : Attribute
{
    public int MethodId { get; } = methodId;
}

internal interface INativeAotReceiver : IIntResultReceiver, IStringResultReceiver
{
    void Notify(int value);

    void SendDynamic(dynamic value);

    void TKey();

    Task AcknowledgeAsync(string value, CancellationToken cancellationToken);

    Task<int> QueryAsync(int value);
}

internal sealed class NativeAotReceiver : INativeAotReceiver
{
    public List<int> Notifications { get; } = new();

    public object? LastDynamicValue { get; private set; }

    public bool KeyCalled { get; private set; }

    public void Notify(int value) => Notifications.Add(value);

    public void SendDynamic(dynamic value) => LastDynamicValue = value;

    public void TKey() => KeyCalled = true;

    public Task AcknowledgeAsync(string value, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<int> QueryAsync(int value) => Task.FromResult(value);

    Task<int> IIntResultReceiver.QueryAsync() => Task.FromResult(42);

    Task<string> IStringResultReceiver.QueryAsync() => Task.FromResult("AOT result");
}

internal static class Program
{
    public static async Task Main()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var first = new NativeAotReceiver();
        var second = new NativeAotReceiver();
        var holder = new ImmutableReceiverHolder<Guid, INativeAotReceiver>([(firstId, first), (secondId, second)]);

        NativeAotGeneratedMulticaster.InMemoryProxyFactory.Create(holder).Notify(1);
        NativeAotGeneratedMulticaster.InMemoryProxyFactory.Only(holder, ImmutableArray.Create(firstId)).Notify(2);
        NativeAotGeneratedMulticaster.InMemoryProxyFactory.Except(holder, ImmutableArray.Create(firstId)).Notify(3);

        Ensure(first.Notifications.SequenceEqual([1, 2]), "Generated in-memory Only behavior differs.");
        Ensure(second.Notifications.SequenceEqual([1, 3]), "Generated in-memory Except behavior differs.");

        var single = NativeAotGeneratedMulticaster.InMemoryProxyFactory.Only(holder, ImmutableArray.Create(firstId));
        single.SendDynamic(7);
        single.TKey();
        Ensure(Equals(first.LastDynamicValue, 7) && second.LastDynamicValue is null, "Dynamic argument was not dispatched statically.");
        Ensure(first.KeyCalled && !second.KeyCalled, "Receiver method conflicts with generated type parameter.");
        Ensure(await ((IIntResultReceiver)single).QueryAsync() == 42, "Inherited integer method was not dispatched.");
        Ensure(await ((IStringResultReceiver)single).QueryAsync() == "AOT result", "Inherited string method was not dispatched.");

        using var pendingTasks = new RemoteClientResultPendingTaskRegistry();
        var serializer = new NativeAotSerializer();
        var writer = new CompletingRemoteWriter(serializer, pendingTasks);
        var remote = NativeAotGeneratedMulticaster.RemoteProxyFactory.CreateDirect<INativeAotReceiver>(writer, serializer);

        remote.Notify(4);
        await remote.AcknowledgeAsync("AOT", CancellationToken.None);
        var result = await remote.QueryAsync(5);

        Ensure(writer.WriteCount == 3, "Generated remote proxy did not write every invocation.");
        Ensure(result == 42, "Generated remote Task<T> client-result path failed.");
        remote.SendDynamic(8);
        Ensure(await ((IIntResultReceiver)remote).QueryAsync() == 42 && serializer.LastContext.MethodId == 201, "Remote inherited integer method used the wrong ID or result type.");
        Ensure(await ((IStringResultReceiver)remote).QueryAsync() == "AOT result" && serializer.LastContext.MethodId == 202,
            "Remote inherited string method used the wrong ID or result type.");
        Ensure(writer.WriteCount == 6, "Additional remote regression invocations were not written.");
        Console.WriteLine("Multicaster NativeAOT smoke test passed.");
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal sealed class CompletingRemoteWriter : IRemoteReceiverWriter
{
    private readonly NativeAotSerializer _serializer;

    public CompletingRemoteWriter(NativeAotSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        _serializer = serializer;
        PendingTasks = pendingTasks;
    }

    public int WriteCount { get; private set; }

    public IRemoteClientResultPendingTaskRegistry PendingTasks { get; }

    public void Write(ReadOnlyMemory<byte> payload)
    {
        WriteCount++;
        var messageId = _serializer.LastContext.MessageId;
        if (messageId.HasValue && PendingTasks.TryGetAndUnregisterPendingTask(messageId.Value, out var pendingTask))
        {
            pendingTask.TrySetResult(ReadOnlyMemory<byte>.Empty);
        }
    }
}

internal sealed class NativeAotSerializer : IRemoteSerializer
{
    public SerializationContext LastContext { get; private set; }

    public void SerializeInvocation(IBufferWriter<byte> writer, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1>(IBufferWriter<byte> writer, T1 arg1, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, in SerializationContext context) => Write(writer, context);
    public void SerializeInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(IBufferWriter<byte> writer, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, in SerializationContext context) => Write(writer, context);

    public T? DeserializeResult<T>(ReadOnlySequence<byte> data, in SerializationContext context)
        => typeof(T) == typeof(int) ? (T)(object)42 : typeof(T) == typeof(string) ? (T)(object)"AOT result" : default;

    private void Write(IBufferWriter<byte> writer, in SerializationContext context)
    {
        LastContext = context;
        writer.GetSpan(1)[0] = 1;
        writer.Advance(1);
    }
}
