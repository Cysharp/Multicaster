using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast;

namespace Multicaster.Tests;

public class InMemoryGroupTest
{
    [Fact]
    public async Task Case_1()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var group = groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup");
        await group.AddAsync(receiverIdA, receiverA);
        await group.AddAsync(receiverIdB, receiverB);
        group.All.OnMessage("System", "Hello");
        group.Except([receiverIdA]).OnMessage("System", "Hello without A");

        receiverA.OnMessage("DirectMessage", "Sent a message to the receiver directly.");

        // Assert
        Assert.Equal([
            ("System", "Hello"),
            ("DirectMessage", "Sent a message to the receiver directly."),
        ], receiverA.Received);

        Assert.Equal([
            ("System", "Hello"),
            ("System", "Hello without A"),
        ], receiverB.Received);
    }

    public interface IGreeterReceiver
    {
        void OnMessage(string name, string message);
        Task<string> HelloAsync(string name, int age);
    }

    class GreeterReceiver(string name) : IGreeterReceiver
    {
        public string Name { get; } = name;
        public List<(string Name, string Message)> Received { get; } = new ();

        public Task<string> HelloAsync(string name, int age)
        {
            return Task.FromResult($"Hello {name} ({age})!");
        }

        public void OnMessage(string name, string message)
        {
            Received.Add((name, message));
        }
    }
}