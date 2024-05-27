using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.Internal;
using Cysharp.Runtime.Multicast.Remoting;

using static Multicaster.Tests.TestJsonRemoteSerializer;

namespace Multicaster.Tests;

public class RemoteGroupTest
{
    [Fact]
    public void Add()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.All.Parameter_One(1234);

        // Assert
        Assert.Equal(2, group.Count());
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverA.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverB.Writer.Written);
    }

    [Fact]
    public void Remove()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Remove(receiverB.Id);
        group.Remove(receiverD.Id);
        group.All.Parameter_One(1234);

        // Assert
        Assert.Equal(2, group.Count());
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
    }

    [Fact]
    public async Task Concurrent_Add()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receivers = Enumerable.Range(0, 10000)
            .Select(x => TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks))
            .ToArray();

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Act
        var receiversQueue = new ConcurrentQueue<(TestRemoteReceiverWriter, ITestReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(_ =>
                Task.Run(() =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (writer, receiver, receiverId) = receiverAndId;
                        group.Add(receiverId, receiver);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10000, group.Count());
    }

    [Fact]
    public void Parameter_Zero()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);

        // Act
        group.All.Parameter_Zero();

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverB.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Parameter_Many()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);

        // Act
        group.All.Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Group_Separation()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var groupA = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupA");
        var groupB = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupB");
        groupA.Add(receiverA.Id, receiverA.Proxy);
        groupA.Add(receiverB.Id, receiverB.Proxy);
        groupB.Add(receiverC.Id, receiverC.Proxy);
        groupB.Add(receiverD.Id, receiverD.Proxy);

        // Act
        groupA.All.Parameter_Many(1234, "Hello", true, 9876543210L);
        groupB.All.Parameter_Two(4321, "Konnichiwa");

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [4321, "Konnichiwa"]))], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [4321, "Konnichiwa"]))], receiverD.Writer.Written);
        Assert.Equal(2, serializer.SerializeInvocationCallCount);
    }


    [Fact]
    public void IgnoreExceptions()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var groupA = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupA");
        groupA.Add(receiverA.Id, receiverA.Proxy);
        groupA.Add(receiverB.Id, receiverB.Proxy);
        groupA.Add(receiverC.Id, receiverC.Proxy);
        groupA.Add(receiverD.Id, receiverD.Proxy);

        // Act
        var ex = Record.Exception(() => groupA.All.Throw());

        // Assert
        Assert.Null(ex);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Except()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);

        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Except([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Only()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);

        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Only([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Single()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Single(receiverB.Id).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Single_NotContains()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Single(Guid.NewGuid()).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }
}

class TestRemoteReceiverWriter :IRemoteReceiverWriter
{
    public List<string> Written { get; } = new();

    public void Write(ReadOnlyMemory<byte> payload)
    {
        Written.Add(Encoding.UTF8.GetString(payload.Span));
    }
}

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