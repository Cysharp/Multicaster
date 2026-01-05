using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Remoting;
using Xunit;

namespace Multicaster.SourceGenerator.Tests;

public class SourceGeneratorTests
{
    [Fact]
    public void GeneratedProxyFactory_ImplementsIInMemoryProxyFactory()
    {
        // Arrange & Act
        var factory = new GeneratedProxyFactory();

        // Assert
        Assert.IsAssignableFrom<IInMemoryProxyFactory>(factory);
    }

    [Fact]
    public void GeneratedProxyFactory_ImplementsIRemoteProxyFactory()
    {
        // Arrange & Act
        var factory = new GeneratedProxyFactory();

        // Assert
        Assert.IsAssignableFrom<IRemoteProxyFactory>(factory);
    }

    [Fact]
    public void InMemoryProxy_CanBeCreated_ForChatReceiver()
    {
        // Arrange
        var factory = new GeneratedProxyFactory();
        var receivers = ReceiverHolder.CreateMutable<string, IChatReceiver>();
        var testReceiver = new TestChatReceiver();
        receivers.Add("user1", testReceiver);

        // Act
        var proxy = factory.Create(receivers);

        // Assert
        Assert.NotNull(proxy);
        Assert.IsAssignableFrom<IChatReceiver>(proxy);
    }

    [Fact]
    public void InMemoryProxy_InvokesAllReceivers()
    {
        // Arrange
        var factory = new GeneratedProxyFactory();
        var receivers = ReceiverHolder.CreateMutable<string, IChatReceiver>();
        var receiver1 = new TestChatReceiver();
        var receiver2 = new TestChatReceiver();
        receivers.Add("user1", receiver1);
        receivers.Add("user2", receiver2);

        // Act
        var proxy = factory.Create(receivers);
        proxy.OnMessage("sender", "Hello!");

        // Assert
        Assert.Equal(1, receiver1.MessageCount);
        Assert.Equal(1, receiver2.MessageCount);
        Assert.Equal("sender", receiver1.LastUser);
        Assert.Equal("Hello!", receiver1.LastMessage);
    }

    [Fact]
    public void InMemoryProxy_ExcludesSpecifiedReceivers()
    {
        // Arrange
        var factory = new GeneratedProxyFactory();
        var receivers = ReceiverHolder.CreateMutable<string, IChatReceiver>();
        var receiver1 = new TestChatReceiver();
        var receiver2 = new TestChatReceiver();
        receivers.Add("user1", receiver1);
        receivers.Add("user2", receiver2);

        // Act
        var proxy = factory.Except(receivers, ["user1"]);
        proxy.OnMessage("sender", "Hello!");

        // Assert
        Assert.Equal(0, receiver1.MessageCount);
        Assert.Equal(1, receiver2.MessageCount);
    }

    private class TestChatReceiver : IChatReceiver
    {
        public int MessageCount { get; private set; }
        public string? LastUser { get; private set; }
        public string? LastMessage { get; private set; }

        public void OnMessage(string user, string message)
        {
            MessageCount++;
            LastUser = user;
            LastMessage = message;
        }

        public void OnUserJoined(string user) { }
        public void OnUserLeft(string user) { }
    }
}
