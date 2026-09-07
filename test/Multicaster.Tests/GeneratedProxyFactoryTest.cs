using System.Collections.Immutable;
using System.Text.Json;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

[MulticasterGeneration(typeof(ITestReceiver))]
public partial class TestGeneratedMulticaster
{
}

[MulticasterGeneration(typeof(ITestInheritedReceiver3))]
public partial class TestGeneratedInheritedMulticaster
{
}

public class GeneratedProxyTest
{
    [Fact]
    public void InMemoryProxyMatchesDynamicInvocationBehavior()
    {
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var holder = new ImmutableReceiverHolder<Guid, ITestReceiver>([(idA, receiverA), (idB, receiverB)]);

        var all = TestGeneratedMulticaster.InMemoryProxyFactory.Create(holder);
        var onlyB = TestGeneratedMulticaster.InMemoryProxyFactory.Only(holder, ImmutableArray.Create(idB));

        all.Parameter_One(10);
        onlyB.Parameter_One(20);

        Assert.Equal([(nameof(ITestReceiver.Parameter_One), (object?)10)], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_One), (object?)10), (nameof(ITestReceiver.Parameter_One), (object?)20)], receiverB.Received);
    }

    [Fact]
    public void RemoteProxyUsesTheSameMethodIdAndArgumentsAsDynamicProxy()
    {
        var serializer = new TestJsonRemoteSerializer();
        var writer = new TestRemoteReceiverWriter();
        var proxy = TestGeneratedMulticaster.RemoteProxyFactory.CreateDirect<ITestReceiver>(writer, serializer);

        proxy.Parameter_Many(1234, "Hello", true, 1234567890L);
        proxy.CustomMethodId();

        Assert.Equal(
            [
                """{"MethodName":"Parameter_Many","MethodId":1287160778,"MessageId":null,"Arguments":[1234,"Hello",true,1234567890]}""",
                """{"MethodName":"CustomMethodId","MethodId":12345,"MessageId":null,"Arguments":[]}""",
            ],
            writer.Written);
    }

    [Fact]
    public async Task RemoteProxySupportsClientResults()
    {
        var serializer = new TestJsonRemoteSerializer();
        var writer = new TestRemoteReceiverWriter();
        var proxy = TestGeneratedMulticaster.RemoteProxyFactory.CreateDirect<ITestReceiver>(writer, serializer);

        var task = proxy.ClientResult_Parameter_One(1234);
        var serialized = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(writer.Written.Single())!;
        Assert.True(writer.PendingTasks.TryGetAndUnregisterPendingTask(serialized.MessageId!.Value, out var pendingTask));
        pendingTask.TrySetResult("\"Generated\""u8.ToArray());

        Assert.Equal("Generated", await task);
    }

    [Fact]
    public void MissingProxyHasActionableError()
    {
        var holder = new ImmutableReceiverHolder<Guid, IDisposable>(Array.Empty<IDisposable>());

        var exception = Assert.Throws<InvalidOperationException>(() => TestGeneratedMulticaster.InMemoryProxyFactory.Create(holder));

        Assert.Contains("MulticasterGeneration", exception.Message);
        Assert.Contains(typeof(IDisposable).FullName!, exception.Message);
    }

    [Fact]
    public void CompositeFactoryUsesTheFirstFactoryThatSupportsTheReceiver()
    {
        var composite = new CompositeInMemoryProxyFactory([TestGeneratedMulticaster.InMemoryProxyFactory, TestGeneratedInheritedMulticaster.InMemoryProxyFactory]);
        var receiver = new TestInheritedReceiver();
        var holder = new ImmutableReceiverHolder<Guid, ITestInheritedReceiver3>([receiver]);

        var proxy = composite.Create(holder);
        proxy.Parameter_One(1234);

        Assert.Equal([(nameof(ITestInheritedReceiver2.Parameter_One), (object?)1234)], receiver.Received);
    }

    [Fact]
    public void CompositeRemoteFactoryUsesTheFirstFactoryThatSupportsTheReceiver()
    {
        var composite = new CompositeRemoteProxyFactory([TestGeneratedMulticaster.RemoteProxyFactory, TestGeneratedInheritedMulticaster.RemoteProxyFactory]);
        var serializer = new TestJsonRemoteSerializer();
        var writer = new TestRemoteReceiverWriter();

        composite.CreateDirect<ITestInheritedReceiver3>(writer, serializer).Parameter_One(1234);

        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], writer.Written);
    }
}
