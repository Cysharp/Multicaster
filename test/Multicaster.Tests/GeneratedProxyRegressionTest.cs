using System.Collections.Immutable;
using System.Text.Json;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests.Regression;

[MulticasterGeneration(typeof(IRegressionReceiver))]
internal partial class RegressionFactories
{
}

/// <summary>Supplies an integer client result through an inherited contract.</summary>
public interface IIntResultReceiver
{
    /// <summary>Returns the integer result.</summary>
    [MethodId(101)]
    Task<int> Query();
}

/// <summary>Supplies a string client result through an inherited contract.</summary>
public interface IStringResultReceiver
{
    /// <summary>Returns the string result.</summary>
    [MethodId(102)]
    Task<string> Query();
}

/// <summary>Exercises names and signatures that must preserve dynamic proxy behavior.</summary>
public interface IRegressionReceiver : IIntResultReceiver, IStringResultReceiver
{
    /// <summary>Records a notification.</summary>
    void Notify();

    /// <summary>Exercises a name shared with an in-memory base method.</summary>
    [MethodId(201)]
    void Invoke(Action<IRegressionReceiver> action);

    /// <summary>Exercises a signature shared with a remote base method.</summary>
    [MethodId(202)]
    void Invoke(string name, int methodId);

    /// <summary>Exercises a name shared with the proxy's type parameter.</summary>
    void TKey();

    /// <summary>Passes a dynamically annotated object.</summary>
    void SendDynamic(dynamic value);

    /// <summary>Exercises an argument name shared with a base method.</summary>
    void Apply(Action Invoke);

    /// <summary>Uses the named method ID instead of the constructor value.</summary>
    [MethodId(1, MethodId = 42)]
    void NamedId();

    private void Helper() { }
}

/// <summary>Defines a settable method ID for parity testing.</summary>
public sealed class MethodIdAttribute(int methodId) : Attribute
{
    /// <summary>Gets or sets the method ID.</summary>
    public int MethodId { get; set; } = methodId;
}

internal sealed class RegressionReceiver : IRegressionReceiver
{
    public int Notifications { get; private set; }
    public int InvokeCalls { get; private set; }
    public int KeyCalls { get; private set; }
    public object? LastValue { get; private set; }

    public void Notify() => Notifications++;
    public void Invoke(Action<IRegressionReceiver> action) => InvokeCalls++;
    public void Invoke(string name, int methodId) => InvokeCalls++;
    public void TKey() => KeyCalls++;
    public void SendDynamic(dynamic value) => LastValue = value;
    public void Apply(Action Invoke) => Invoke();
    public void NamedId() { }
    Task<int> IIntResultReceiver.Query() => Task.FromResult(42);
    Task<string> IStringResultReceiver.Query() => Task.FromResult("result");
}

public class GeneratedProxyRegressionTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InMemoryDispatchPreservesReceiverMethods(bool generated)
    {
        var factory = generated ? RegressionFactories.InMemoryProxyFactory : DynamicInMemoryProxyFactory.Instance;
        var receiver = new RegressionReceiver();
        var holder = new ImmutableReceiverHolder<int, IRegressionReceiver>([(1, receiver)]);
        var proxy = factory.Only(holder, ImmutableArray.Create(1));
        var applied = false;

        proxy.Notify();
        proxy.TKey();
        proxy.SendDynamic("value");
        proxy.Apply(() => applied = true);

        Assert.Equal(1, receiver.Notifications);
        Assert.Equal(0, receiver.InvokeCalls);
        Assert.Equal(1, receiver.KeyCalls);
        Assert.Equal("value", receiver.LastValue);
        Assert.True(applied);
        Assert.Equal(42, await ((IIntResultReceiver)proxy).Query());
        Assert.Equal("result", await ((IStringResultReceiver)proxy).Query());
    }

    [Fact]
    public void RemoteInvocationMetadataMatchesDynamicFactory()
    {
        var serializer = new TestJsonRemoteSerializer();
        var generatedWriter = new TestRemoteReceiverWriter();
        var dynamicWriter = new TestRemoteReceiverWriter();
        var generated = RegressionFactories.RemoteProxyFactory.CreateDirect<IRegressionReceiver>(generatedWriter, serializer);
        var dynamic = DynamicRemoteProxyFactory.Instance.CreateDirect<IRegressionReceiver>(dynamicWriter, serializer);

        foreach (var proxy in new[] { generated, dynamic })
        {
            proxy.Notify();
            proxy.TKey();
            proxy.SendDynamic("value");
            proxy.NamedId();
        }

        Assert.Equal(dynamicWriter.Written, generatedWriter.Written);
        var first = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(generatedWriter.Written.First())!;
        var last = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(generatedWriter.Written.Last())!;
        Assert.Equal("Notify", first.MethodName);
        Assert.Equal(42, last.MethodId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemoteInheritedMethodsUseTheirOwnIdsAndResultTypes(bool generated)
    {
        var factory = generated ? RegressionFactories.RemoteProxyFactory : DynamicRemoteProxyFactory.Instance;
        var writer = new TestRemoteReceiverWriter();
        var proxy = factory.CreateDirect<IRegressionReceiver>(writer, new TestJsonRemoteSerializer());

        var intTask = ((IIntResultReceiver)proxy).Query();
        CompleteResult(writer, 0, 101, "42"u8.ToArray());
        Assert.Equal(42, await intTask);

        var stringTask = ((IStringResultReceiver)proxy).Query();
        CompleteResult(writer, 1, 102, "\"result\""u8.ToArray());
        Assert.Equal("result", await stringTask);
    }

    private static void CompleteResult(TestRemoteReceiverWriter writer, int index, int methodId, byte[] result)
    {
        var invocation = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(writer.Written[index])!;
        Assert.Equal(methodId, invocation.MethodId);
        Assert.True(writer.PendingTasks.TryGetAndUnregisterPendingTask(invocation.MessageId!.Value, out var pending));
        pending.TrySetResult(result);
    }
}
