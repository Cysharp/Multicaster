using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

public class RemoteGroupClientResultTest
{
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
}