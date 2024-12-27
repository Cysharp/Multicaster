using Cysharp.Runtime.Multicast.InMemory;

namespace Multicaster.Tests;

public class ImmutableReceiverHolderTest
{
    [Fact]
    public void Create()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var holder = ReceiverHolder.CreateImmutable<string, ITestReceiver>([receiverA, receiverB, receiverC]);

        // Act
        using var snapshot = holder.AsSnapshot();
        var receivers = snapshot.AsSpan().ToArray();

        // Assert
        Assert.Equal([
            new ReceiverRegistration<string, ITestReceiver>(null, receiverA, HasKey: false),
            new ReceiverRegistration<string, ITestReceiver>(null, receiverB, HasKey: false),
            new ReceiverRegistration<string, ITestReceiver>(null, receiverC, HasKey: false),
        ], receivers);
    }
}

public class MutableReceiverHolderTest
{
    [Fact]
    public void CreateMutableWithInitialReceivers()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var receiverD = new TestInMemoryReceiver();
        var initialReceivers = new List<(string, ITestReceiver)>([("A", receiverA), ("B", receiverB), ("C", receiverC)]);

        // Act
        var holder = ReceiverHolder.CreateMutableWithInitialReceivers<string, ITestReceiver>(initialReceivers);
        using var snapshot = holder.AsSnapshot();
        var receivers = snapshot.AsSpan().ToArray();

        // Assert
        Assert.Equal([
            new ReceiverRegistration<string, ITestReceiver>("A", receiverA, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("B", receiverB, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("C", receiverC, HasKey: true),
        ], receivers);
    }

    [Fact]
    public void CreateMutableWithInitialReceivers_ModifyInitialReceivers()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var receiverD = new TestInMemoryReceiver();
        var initialReceivers = new List<(string, ITestReceiver)>([("A", receiverA), ("B", receiverB), ("C", receiverC)]);
        var holder = ReceiverHolder.CreateMutableWithInitialReceivers<string, ITestReceiver>(initialReceivers);

        // Act
        using var snapshot = holder.AsSnapshot();
        var receivers = snapshot.AsSpan().ToArray();
        initialReceivers.Add(("D", receiverD));

        // Assert
        Assert.Equal([
            new ReceiverRegistration<string, ITestReceiver>("A", receiverA, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("B", receiverB, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("C", receiverC, HasKey: true),
        ], receivers);
    }

    [Fact]
    public void Add()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var receiverD = new TestInMemoryReceiver();
        var holder = ReceiverHolder.CreateMutable<string, ITestReceiver>();

        // Act
        holder.Add("A", receiverA);
        holder.Add("B", receiverB);
        holder.Add("C", receiverC);
        holder.Add("D", receiverD);
        using var snapshot = holder.AsSnapshot();
        var receivers = snapshot.AsSpan().ToArray();

        // Assert
        Assert.Equal([
            new ReceiverRegistration<string, ITestReceiver>("A", receiverA, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("B", receiverB, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("C", receiverC, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("D", receiverD, HasKey: true),
        ], receivers);
    }

    [Fact]
    public void Remove()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var receiverD = new TestInMemoryReceiver();

        var holder = ReceiverHolder.CreateMutable<string, ITestReceiver>();
        holder.Add("A", receiverA);
        holder.Add("B", receiverB);
        holder.Add("C", receiverC);
        holder.Add("D", receiverD);

        // Act
        holder.Remove("C");
        using var snapshot = holder.AsSnapshot();
        var receivers = snapshot.AsSpan().ToArray();

        // Assert
        Assert.Equal([
            new ReceiverRegistration<string, ITestReceiver>("A", receiverA, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("B", receiverB, HasKey: true),
            new ReceiverRegistration<string, ITestReceiver>("D", receiverD, HasKey: true),
        ], receivers);
    }
}