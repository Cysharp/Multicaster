using System.Collections.Concurrent;
using System.Text.Json;

using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

public class DynamicRemoteProxyFactoryTest
{
    [Fact]
    public void CreateDirect()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act
        proxy.Parameter_Many(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal(["""{"MethodName":"Parameter_Many","MethodId":1287160778,"MessageId":null,"Arguments":[1234,"Hello",true,1234567890]}"""], receiverWriterA.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Create_Multiple_Parameter_Zero()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverWriterB = new TestRemoteReceiverWriter();
        var receivers = new ConcurrentDictionary<Guid, IRemoteReceiverWriter>();
        receivers.TryAdd(Guid.NewGuid(), receiverWriterA);
        receivers.TryAdd(Guid.NewGuid(), receiverWriterB);

        var proxy = proxyFactory.Create<ITestReceiver>(receivers, serializer, pendingTasks);

        // Act
        proxy.Parameter_Zero();

        // Assert
        Assert.Equal(["""{"MethodName":"Parameter_Zero","MethodId":1994667803,"MessageId":null,"Arguments":[]}"""], receiverWriterA.Written);
        Assert.Equal(["""{"MethodName":"Parameter_Zero","MethodId":1994667803,"MessageId":null,"Arguments":[]}"""], receiverWriterB.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Create_Multiple_Parameter_One()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverWriterB = new TestRemoteReceiverWriter();
        var receivers = new ConcurrentDictionary<Guid, IRemoteReceiverWriter>();
        receivers.TryAdd(Guid.NewGuid(), receiverWriterA);
        receivers.TryAdd(Guid.NewGuid(), receiverWriterB);

        var proxy = proxyFactory.Create<ITestReceiver>(receivers, serializer, pendingTasks);

        // Act
        proxy.Parameter_One(1234);

        // Assert
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverWriterA.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverWriterB.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Create_Multiple_Parameter_Many()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverWriterB = new TestRemoteReceiverWriter();
        var receivers = new ConcurrentDictionary<Guid, IRemoteReceiverWriter>();
        receivers.TryAdd(Guid.NewGuid(), receiverWriterA);
        receivers.TryAdd(Guid.NewGuid(), receiverWriterB);

        var proxy = proxyFactory.Create<ITestReceiver>(receivers, serializer, pendingTasks);

        // Act
        proxy.Parameter_Many(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal(["""{"MethodName":"Parameter_Many","MethodId":1287160778,"MessageId":null,"Arguments":[1234,"Hello",true,1234567890]}"""], receiverWriterA.Written);
        Assert.Equal(["""{"MethodName":"Parameter_Many","MethodId":1287160778,"MessageId":null,"Arguments":[1234,"Hello",true,1234567890]}"""], receiverWriterB.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task CreateDirect_ClientResult_Parameter_Zero_NoReturnValue()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Zero_NoReturnValue();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        var serialized = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.Equal("ClientResult_Parameter_Zero_NoReturnValue", serialized.MethodName);
        Assert.NotNull(serialized.MessageId);
        Assert.Equal([], serialized.Arguments);

        Assert.Equal(1, pendingTasks.Count);
        Assert.True(pendingTasks.TryGetAndUnregisterPendingTask(serialized.MessageId.Value, out var pendingMessage));
        Assert.False(task.IsCompleted);
        pendingMessage.TrySetResult("[]"u8.ToArray());
        await Task.Delay(100);
        Assert.True(task.IsCompleted);

        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task CreateDirect_ClientResult_Parameter_Many_NoReturnValue()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Many_NoReturnValue(1234, "Hello", true, 1234567890L);
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        var serialized = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.Equal("ClientResult_Parameter_Many_NoReturnValue", serialized.MethodName);
        Assert.NotNull(serialized.MessageId);
        Assert.Equal(1234, ((JsonElement)serialized.Arguments[0]!).GetInt32());
        Assert.Equal("Hello", ((JsonElement)serialized.Arguments[1]!).GetString());
        Assert.True(((JsonElement)serialized.Arguments[2]!).GetBoolean());
        Assert.Equal(1234567890L, ((JsonElement)serialized.Arguments[3]!).GetInt64());

        Assert.Equal(1, pendingTasks.Count);
        Assert.True(pendingTasks.TryGetAndUnregisterPendingTask(serialized.MessageId.Value, out var pendingMessage));
        Assert.False(task.IsCompleted);
        pendingMessage.TrySetResult("[]"u8.ToArray());
        await Task.Delay(100);
        Assert.True(task.IsCompleted);

        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task CreateDirect_ClientResult_Parameter_Zero()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Zero();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        var serialized = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.Equal("ClientResult_Parameter_Zero", serialized.MethodName);
        Assert.NotNull(serialized.MessageId);
        Assert.Equal([], serialized.Arguments);

        Assert.Equal(1, pendingTasks.Count);
        Assert.True(pendingTasks.TryGetAndUnregisterPendingTask(serialized.MessageId.Value, out var pendingMessage));
        Assert.False(task.IsCompleted);
        pendingMessage.TrySetResult("\"Hello!\""u8.ToArray());
        var result = await task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(task.IsCompleted);
        Assert.Equal("Hello!", result);

        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task CreateDirect_ClientResult_Parameter_Many()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Many(1234, "Hello", true, 1234567890L);
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        var serialized = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.Equal("ClientResult_Parameter_Many", serialized.MethodName);
        Assert.NotNull(serialized.MessageId);
        Assert.Equal(1234, ((JsonElement)serialized.Arguments[0]!).GetInt32());
        Assert.Equal("Hello", ((JsonElement)serialized.Arguments[1]!).GetString());
        Assert.True(((JsonElement)serialized.Arguments[2]!).GetBoolean());
        Assert.Equal(1234567890L, ((JsonElement)serialized.Arguments[3]!).GetInt64());

        Assert.Equal(1, pendingTasks.Count);
        Assert.True(pendingTasks.TryGetAndUnregisterPendingTask(serialized.MessageId.Value, out var pendingMessage));
        Assert.False(task.IsCompleted);
        pendingMessage.TrySetResult("\"Hello!\""u8.ToArray());
        var result = await task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(task.IsCompleted);
        Assert.Equal("Hello!", result);

        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }
}