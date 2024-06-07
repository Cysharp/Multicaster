using System.Text.Json;
using System.Text.Json.Nodes;

using Cysharp.Runtime.Multicast.Remoting;

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
        var pendingTasks = new RemoteClientResultPendingTaskRegistry(TimeSpan.FromMilliseconds(500)); // Use the specified timeout period.

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Zero();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, pendingTasks.Count);

        // Wait for timeout...
        await Task.Delay(TimeSpan.FromMilliseconds(750));

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
        var cts = new CancellationTokenSource();

        // Act & Assert
        cts.CancelAfter(250);
        var task = proxy.ClientResult_Cancellation(5000, cts.Token);
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, pendingTasks.Count);

        // Wait for timeout...
        await Task.Delay(TimeSpan.FromMilliseconds(500));

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
}