using System.Text.Json;

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

        var receiverWriterA = new TestRemoteReceiverWriter(pendingTasks);
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer);

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

        var receiverWriterA = new TestRemoteReceiverWriter(pendingTasks);
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer);

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

        var receiverWriterA = new TestRemoteReceiverWriter(pendingTasks);
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer);

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

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer);
        var timeProvider = new FakeTimeProvider();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250), timeProvider);

        // Act & Assert
        var task = proxy.ClientResult_Cancellation(5000, cts.Token);
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, receiverWriterA.PendingTasks.Count);

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

        Assert.Equal(0, receiverWriterA.PendingTasks.Count);
    }

    [Fact]
    public async Task Group_Add_Call_Remove_Call()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverWriterB = new TestRemoteReceiverWriter();
        var receiverWriterC = new TestRemoteReceiverWriter();
        var receiverInMemoryA = new TestInMemoryReceiver();
        var receiverInMemoryB = new TestInMemoryReceiver();
        var receiverInMemoryC = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("RemoteA", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
        group.Add("RemoteB", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterB, serializer));
        group.Add("InMemoryA", receiverInMemoryA);
        group.Add("InMemoryB", receiverInMemoryB);

        // Act & Assert
        // Call the method on RemoteA
        {
            var resultTask = group.Single("RemoteA").ClientResult_Parameter_Zero();
            var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
            Assert.True(receiverWriterA.PendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId!.Value, out var pendingTask));
            pendingTask.TrySetResult("\"OK\""u8.ToArray());

            var result = await resultTask;
            Assert.Empty(receiverInMemoryA.Received);
            Assert.Empty(receiverInMemoryB.Received);
            Assert.NotEmpty(receiverWriterA.Written);
            Assert.Empty(receiverWriterB.Written);
            Assert.Equal("OK", result);
            Assert.Equal(nameof(ITestReceiver.ClientResult_Parameter_Zero), invocationMessage.MethodName);
            Assert.Empty(invocationMessage.Arguments);
        }

        // Remove RemoteA from the group
        group.Remove("RemoteA");
        receiverWriterA.Written.Clear();

        // Call the method on InMemoryA
        {
            var result = await group.Single("InMemoryA").ClientResult_Parameter_Zero();
            Assert.Empty(receiverWriterA.Written);
            Assert.Empty(receiverWriterB.Written);
            Assert.Single(receiverInMemoryA.Received);
            Assert.Empty(receiverInMemoryB.Received);
            Assert.Equal((nameof(ITestReceiver.ClientResult_Parameter_Zero), TestInMemoryReceiver.ParameterZeroArgument), receiverInMemoryA.Received[0]);
            Assert.Equal($"{nameof(ITestReceiver.ClientResult_Parameter_Zero)}", result);
        }

        // Remove InMemoryA from the group
        group.Remove("InMemoryA");
        receiverInMemoryA.Received.Clear();

        // Call the method on RemoteB
        {
            var resultTask = group.Single("RemoteB").ClientResult_Parameter_Zero();
            var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterB.Written[0])!;
            Assert.True(receiverWriterB.PendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId!.Value, out var pendingTask));
            pendingTask.TrySetResult("\"OK123\""u8.ToArray());

            var result = await resultTask;
            Assert.Empty(receiverInMemoryA.Received);
            Assert.Empty(receiverInMemoryB.Received);
            Assert.Empty(receiverWriterA.Written);
            Assert.NotEmpty(receiverWriterB.Written);
            Assert.Equal("OK123", result);
            Assert.Equal(nameof(ITestReceiver.ClientResult_Parameter_Zero), invocationMessage.MethodName);
            Assert.Empty(invocationMessage.Arguments);
        }

        // Remove RemoteB from the group
        group.Remove("RemoteB");
        receiverWriterB.Written.Clear();

        // Call the method on InMemoryA
        {
            var result = await group.Single("InMemoryB").ClientResult_Parameter_Zero();
            Assert.Empty(receiverWriterA.Written);
            Assert.Empty(receiverWriterB.Written);
            Assert.Empty(receiverInMemoryA.Received);
            Assert.Single(receiverInMemoryB.Received);
            Assert.Equal((nameof(ITestReceiver.ClientResult_Parameter_Zero), TestInMemoryReceiver.ParameterZeroArgument), receiverInMemoryB.Received[0]);
            Assert.Equal($"{nameof(ITestReceiver.ClientResult_Parameter_Zero)}", result);
        }

        // Add RemoteC to the group
        group.Add("RemoteC", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterC, serializer));
        receiverInMemoryB.Received.Clear();

        // Call the method on RemoteC
        {
            var resultTask = group.Single("RemoteC").ClientResult_Parameter_Zero();
            var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterC.Written[0])!;
            Assert.True(receiverWriterC.PendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId!.Value, out var pendingTask));
            pendingTask.TrySetResult("\"OK456\""u8.ToArray());

            var result = await resultTask;
            Assert.Empty(receiverInMemoryA.Received);
            Assert.Empty(receiverInMemoryB.Received);
            Assert.Empty(receiverWriterA.Written);
            Assert.Empty(receiverWriterB.Written);
            Assert.NotEmpty(receiverWriterC.Written);
            Assert.Equal("OK456", result);
            Assert.Equal(nameof(ITestReceiver.ClientResult_Parameter_Zero), invocationMessage.MethodName);
            Assert.Empty(invocationMessage.Arguments);
        }
    }

    [Fact]
    public async Task Group_Remote_Parameter_Zero()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.Single("Remote");
        var resultTask = target.ClientResult_Parameter_Zero();
        var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.True(receiverWriterA.PendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId!.Value, out var pendingTask));
        pendingTask.TrySetResult("\"OK\""u8.ToArray());

        var result = await resultTask;

        // Assert
        Assert.Empty(receiverInMemory.Received);
        Assert.Equal("OK", result);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(nameof(ITestReceiver.ClientResult_Parameter_Zero), invocationMessage.MethodName);
        Assert.Empty(invocationMessage.Arguments);
    }


    [Fact]
    public async Task Group_Remote_Parameter_One()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.Single("Remote");
        var resultTask = target.ClientResult_Parameter_One(456);
        var invocationMessage = JsonSerializer.Deserialize<TestJsonRemoteSerializer.SerializedInvocation>(receiverWriterA.Written[0])!;
        Assert.True(receiverWriterA.PendingTasks.TryGetAndUnregisterPendingTask(invocationMessage.MessageId!.Value, out var pendingTask));
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
    public async Task Group_InMemory_Parameter_One()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
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
    public async Task Group_Remote_NoTarget()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.Single("NoSuchTarget");
        var ex = await Record.ExceptionAsync(async () => await target.ClientResult_Parameter_Zero());

        // Assert
        Assert.NotNull(ex);
        Assert.Equal("No invocable target found.", ex.Message);
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public async Task Group_NotSingle_NotSupported()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");
            
        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
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
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.Only(["InMemory"]);
        await Assert.ThrowsAsync<NotSupportedException>(async () => await target.ClientResult_Parameter_One(123));
    }

    [Fact]
    public async Task Group_Remote_CanceledByDispose()
    {
        // Arrange
        var inMemoryProxyFactory = DynamicInMemoryProxyFactory.Instance;
        var remoteProxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var groupProvider = new RemoteGroupProvider(inMemoryProxyFactory, remoteProxyFactory, serializer);

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverInMemory = new TestInMemoryReceiver();
        var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("Test");

        group.Add("Remote", remoteProxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer));
        group.Add("InMemory", receiverInMemory);

        // Act & Assert
        var target = group.Single("Remote");
        var task = target.ClientResult_Parameter_Zero();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, receiverWriterA.PendingTasks.Count);

        receiverWriterA.PendingTasks.Dispose(); // Cancel all pending tasks.
        var ex = await Record.ExceptionAsync(async () => await task);

        // The task should be canceled by Dispose and removed from pending tasks.
        Assert.True(task.IsCompleted);
        Assert.Equal(0, receiverWriterA.PendingTasks.Count);
        Assert.IsType<TaskCanceledException>(ex);
    }
}