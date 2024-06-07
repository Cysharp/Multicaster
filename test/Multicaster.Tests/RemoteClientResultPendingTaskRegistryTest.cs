using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

public class RemoteClientResultPendingTaskRegistryTest
{
    [Fact]
    public void CancelAll_On_Dispose()
    {
        // Arrange
        var reg = new RemoteClientResultPendingTaskRegistry();
        var serializer = new TestJsonRemoteSerializer();
        var tcs1 = new TaskCompletionSource<bool>();
        var pendingTask1 = reg.CreateTask("Foo", 0, Guid.NewGuid(), tcs1, default, serializer);
        reg.Register(pendingTask1);
        var tcs2 = new TaskCompletionSource<bool>();
        var pendingTask2 = reg.CreateTask("Foo", 0, Guid.NewGuid(), tcs2, default, serializer);
        reg.Register(pendingTask2);
        var tcs3 = new TaskCompletionSource();
        var pendingTask3 = reg.CreateTask("Bar", 0, Guid.NewGuid(), tcs3, default, serializer);
        reg.Register(pendingTask3);

        // Act
        reg.Dispose();

        // Assert
        Assert.True(tcs1.Task.IsCanceled);
        Assert.True(tcs2.Task.IsCanceled);
        Assert.True(tcs3.Task.IsCanceled);
    }

    [Fact]
    public void CancelImmediately_On_Register_When_Disposed()
    {
        // Arrange
        var reg = new RemoteClientResultPendingTaskRegistry();
        var serializer = new TestJsonRemoteSerializer();
        reg.Dispose();

        // Act
        var tcs1 = new TaskCompletionSource<bool>();
        var pendingTask1 = reg.CreateTask("Foo", 0, Guid.NewGuid(), tcs1, default, serializer);
        reg.Register(pendingTask1);

        // Assert
        Assert.True(tcs1.Task.IsCanceled);
    }

    [Fact]
    public async Task Timeout()
    {
        // Arrange
        using var reg = new RemoteClientResultPendingTaskRegistry(TimeSpan.FromMilliseconds(500));
        var serializer = new TestJsonRemoteSerializer();
        var tcs1 = new TaskCompletionSource<bool>();
        var pendingTask1 = reg.CreateTask("Foo", 0, Guid.NewGuid(), tcs1, default, serializer);
        reg.Register(pendingTask1);
        var tcs2 = new TaskCompletionSource<bool>();
        var pendingTask2 = reg.CreateTask("Foo", 0, Guid.NewGuid(), tcs2, default, serializer);
        reg.Register(pendingTask2);
        var tcs3 = new TaskCompletionSource();
        var pendingTask3 = reg.CreateTask("Bar", 0, Guid.NewGuid(), tcs3, new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token, serializer);
        reg.Register(pendingTask3);

        // Act
        await Task.Delay(100);
        var beforeSecondDelayTcs1IsCanceled = tcs1.Task.IsCanceled;
        var beforeSecondDelayTcs2IsCanceled = tcs2.Task.IsCanceled;
        var beforeSecondDelayTcs3IsCanceled = tcs3.Task.IsCanceled;
        await Task.Delay(600);

        // Assert
        Assert.False(beforeSecondDelayTcs1IsCanceled);
        Assert.False(beforeSecondDelayTcs2IsCanceled);
        Assert.True(beforeSecondDelayTcs3IsCanceled); // The timeout of the pending task is overridden.
        Assert.True(tcs1.Task.IsCanceled);
        Assert.True(tcs2.Task.IsCanceled);
        Assert.True(tcs3.Task.IsCanceled);
    }
}