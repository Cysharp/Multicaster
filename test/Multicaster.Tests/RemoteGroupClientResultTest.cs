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
        var pendingTasks = new RemoteClientResultPendingTaskRegistry(TimeSpan.FromMilliseconds(500));

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingTasks);

        // Act & Assert
        var task = proxy.ClientResult_Parameter_Zero();
        Assert.False(task.IsCompleted);
        Assert.NotEmpty(receiverWriterA.Written);
        Assert.Equal(1, pendingTasks.Count);

        await Task.Delay(TimeSpan.FromMilliseconds(750));

        Assert.True(task.IsCompleted);
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.Equal(0, pendingTasks.Count);
    }
}