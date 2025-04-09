using System.Text.Json;
using System.Text.Json.Nodes;

using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Remoting;

using Microsoft.Extensions.Time.Testing;

namespace Multicaster.Tests;

public class RemoteGroupClientResultTest
{
    [Fact]
    public async Task Result()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry(TimeSpan.FromHours(1));

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Zero();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, pendingTasks.Count);

        var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.NotNull(invocationMessage.MessageId);

        // Simulate receiving a result from the client.
        Assert.True(pendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId.Value, out var pendingTask));
        pendingTask.TrySetResult("\"Hello!\""u8.ToArray());

        // The task should be completed.
        var retVal = await task;
        Assert.Equal("Hello!", retVal);
        Assert.Equal(0, pendingTasks.Count);
    }

    [Fact]
    public async Task Throw()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry(TimeSpan.FromHours(1));

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Throw();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, pendingTasks.Count);

        var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.NotNull(invocationMessage.MessageId);

        // Simulate receiving a result from the client.
        Assert.True(pendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId.Value, out var pendingTask));
        pendingTask.TrySetException(new InvalidOperationException("Something went wrong."));

        // The task should be completed.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        Assert.Equal("Something went wrong.", ex.Message);
        Assert.Equal(0, pendingTasks.Count);
    }

    [Fact]
    public async Task Timeout()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var timeProvider = new FakeTimeProvider();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry(TimeSpan.FromMilliseconds(250), timeProvider); // Use the specified timeout period.

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Zero();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, pendingTasks.Count);

        // Wait for timeout...
        timeProvider.Advance(TimeSpan.FromMilliseconds(750));

        // The task should be canceled by timeout and removed from pending tasks.
        Assert.True(task.IsCompleted);
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.Equal(0, pendingTasks.Count);
    }

    [Fact]
    public async Task Cancellation()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);
        var timeProvider = new FakeTimeProvider();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250), timeProvider);

        // Act & Assert
        var task = proxy.ClientResult_Cancellation(5000, cts.Token);
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, pendingTasks.Count);

        // Wait for timeout...
        timeProvider.Advance(TimeSpan.FromMilliseconds(500));

        // The task should be canceled by timeout and removed from pending tasks.
        Assert.True(task.IsCompleted);
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);

        Assert.NotEmpty(receiverWriterA.Written);
        var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.Equal(nameof(ITestReceiver.ClientResult_Cancellation), invocationMessage.MethodName);
        Assert.Single(invocationMessage.Arguments);
        Assert.Equal(5000, ((JsonElement)invocationMessage.Arguments[0]!).GetInt32());

        Assert.Equal(0, pendingTasks.Count);
    }

    [Fact]
    public async Task Group_Remote()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer, pendingTasks);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.Single("Remote");
        var resultTask = target.ClientResult_Parameter_One(456);
        var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.True(pendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId!.Value, out var pendingTask));
        pendingTask.TrySetResult("\"OK\""u8.ToArray());

        var result = await resultTask;

        // Assert
        Assert.Empty(receiverInMemory.Received);
        Assert.Equal("OK", result);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(nameof(ITestReceiver.ClientResult_Parameter_One), invocationMessage.MethodName);
        Assert.Equal("456", invocationMessage.Arguments[0]?.ToString());
    }

    [Fact]
    public async Task Group_InMemory()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer, pendingTasks);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks));
        group.Add("InMemory", receiverInMemory);

        // Act
        var target = group.Single("InMemory");
        var result = await target.ClientResult_Parameter_One(123);

        // Assert
        Assert.Empty(receiverWriterA.Written);
        Assert.Single(receiverInMemory.Received);
        Assert.Equal((nameof(ITestReceiver.ClientResult_Parameter_One), 123), receiverInMemory.Received[0]);
        Assert.Equal($"{nameof(ITestReceiver.ClientResult_Parameter_One)}:123", result);
    }

    [Fact]
    public async Task Group_NotSingle_NotSupported()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer, pendingTasks);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.All;
        await Assert.ThrowsAsync<NotSupportedException>(async () => await target.ClientResult_Parameter_One(123));
    }

    [Fact]
    public async Task Group_Only_One_NotSupported()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer, pendingTasks);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.Only(["InMemory"]);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await target.ClientResult_Parameter_One(123));
    }
}