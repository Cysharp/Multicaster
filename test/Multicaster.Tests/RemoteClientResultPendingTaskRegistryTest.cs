using Cysharp.Runtime.Multicast.Remoting;

using Microsoft.Extensions.Time.Testing;

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
    public void Timeout()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        using var reg = new RemoteClientResultPendingTaskRegistry(TimeSpan.FromMilliseconds(500), timeProvider);
        var serializer = new TestJsonRemoteSerializer();
        var tcs1 = new TaskCompletionSource<bool>();
        var pendingTask1 = reg.CreateTask("Foo", 0, Guid.NewGuid(), tcs1, default, serializer);
        reg.Register(pendingTask1);
        var tcs2 = new TaskCompletionSource<bool>();
        var pendingTask2 = reg.CreateTask("Foo", 0, Guid.NewGuid(), tcs2, default, serializer);
        reg.Register(pendingTask2);
        var tcs3 = new TaskCompletionSource();
        var pendingTask3 = reg.CreateTask("Bar", 0, Guid.NewGuid(), tcs3, new CancellationTokenSource(TimeSpan.FromMilliseconds(10), timeProvider).Token, serializer);
        reg.Register(pendingTask3);

        // Act
        timeProvider.Advance(TimeSpan.FromSeconds(100));
        var beforeSecondDelayTcs1IsCanceled = tcs1.Task.IsCanceled;
        var beforeSecondDelayTcs2IsCanceled = tcs2.Task.IsCanceled;
        var beforeSecondDelayTcs3IsCanceled = tcs3.Task.IsCanceled;
        timeProvider.Advance(TimeSpan.FromSeconds(500));

        // Assert
        Assert.False(beforeSecondDelayTcs1IsCanceled);
        Assert.False(beforeSecondDelayTcs2IsCanceled);
        Assert.False(beforeSecondDelayTcs3IsCanceled);
        // The timeout of the pending task is overridden.
        Assert.IsType<TaskCanceledException>(tcs3.Task.Exception!.InnerException);
        Assert.IsType<TimeoutException>(tcs3.Task.Exception!.InnerException.InnerException);

        Assert.True(tcs1.Task.IsCompleted);
        Assert.True(tcs2.Task.IsCompleted);
        Assert.True(tcs3.Task.IsCompleted);
    }
}