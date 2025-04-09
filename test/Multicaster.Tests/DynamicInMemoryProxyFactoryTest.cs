using System.Collections.Immutable;

using Cysharp.Runtime.Multicast.InMemory;

namespace Multicaster.Tests;

public class DynamicInMemoryProxyFactoryTest
{
    [Fact]
    public void Inherited_Create()
    {
        // Arrange
        var receiverA = new TestInheritedReceiver();
        var receiverAId = Guid.NewGuid();
        var receiverB = new TestInheritedReceiver();
        var receiverBId = Guid.NewGuid();

        // Act
        var proxy = DynamicInMemoryProxyFactory.Instance.Create<Guid, ITestInheritedReceiver3>(ReceiverHolder.CreateMutableWithInitialReceivers([
            (receiverAId, (ITestInheritedReceiver3)receiverA),
            (receiverBId, (ITestInheritedReceiver3)receiverB)
        ]), ImmutableArray<Guid>.Empty, null);
    }

    [Fact]
    public void Inherited_Call()
    {
        // Arrange
        var receiverA = new TestInheritedReceiver();
        var receiverAId = Guid.NewGuid();
        var receiverB = new TestInheritedReceiver();
        var receiverBId = Guid.NewGuid();

        var proxy = DynamicInMemoryProxyFactory.Instance.Create<Guid, ITestInheritedReceiver3>(ReceiverHolder.CreateMutableWithInitialReceivers([
            (receiverAId, (ITestInheritedReceiver3)receiverA),
            (receiverBId, (ITestInheritedReceiver3)receiverB)
        ]), ImmutableArray<Guid>.Empty, null);

        // Act
        proxy.Parameter_Zero();
        proxy.Parameter_One(12345);
        proxy.Parameter_Many(1234, "Hello", true);

        // Assert
        Assert.Equal([
            (nameof(ITestInheritedReceiver.Parameter_Zero), (TestInheritedReceiver.ParameterZeroArgument)),
            (nameof(ITestInheritedReceiver2.Parameter_One), (12345)),
            (nameof(ITestInheritedReceiver3.Parameter_Many), (1234, "Hello", true)),
        ], receiverA.Received);
        Assert.Equal([
            (nameof(ITestInheritedReceiver.Parameter_Zero), (TestInheritedReceiver.ParameterZeroArgument)),
            (nameof(ITestInheritedReceiver2.Parameter_One), (12345)),
            (nameof(ITestInheritedReceiver3.Parameter_Many), (1234, "Hello", true)),
        ], receiverB.Received);
    }

    [Fact]
    public void DifferentKeyTypes()
    {
        DynamicInMemoryProxyFactory.Instance.Create(new ImmutableReceiverHolder<Guid, ITestReceiver>([(Guid.NewGuid(), new TestInMemoryReceiver())]), [], null);
        DynamicInMemoryProxyFactory.Instance.Create(new ImmutableReceiverHolder<string, ITestReceiver>([(string.Empty, new TestInMemoryReceiver())]), [], null);
    }
}