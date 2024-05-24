using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;

namespace Multicaster.Tests;

public class InMemoryGroupClientResultTest
{
    [Fact]
    public async Task Parameter_Zero_NoReturnValue()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        await group.Single(receiverIdB).ClientResult_Parameter_Zero_NoReturnValue();

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.ClientResult_Parameter_Zero_NoReturnValue), TestInMemoryReceiver.ParameterZeroArgument)], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task Parameter_One_NoReturnValue()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        await group.Single(receiverIdB).ClientResult_Parameter_One_NoReturnValue(1234);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.ClientResult_Parameter_One_NoReturnValue), (1234))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task Parameter_Many_NoReturnValue()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        await group.Single(receiverIdB).ClientResult_Parameter_Many_NoReturnValue(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.ClientResult_Parameter_Many_NoReturnValue), (1234, "Hello", true, 1234567890L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task Parameter_Zero()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var retVal = await group.Single(receiverIdB).ClientResult_Parameter_Zero();

        // Assert
        Assert.Equal($"{nameof(ITestReceiver.ClientResult_Parameter_Zero)}", retVal);
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.ClientResult_Parameter_Zero), TestInMemoryReceiver.ParameterZeroArgument)], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task Parameter_One()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var retVal = await group.Single(receiverIdB).ClientResult_Parameter_One(1234);

        // Assert
        Assert.Equal($"{nameof(ITestReceiver.ClientResult_Parameter_One)}:1234", retVal);
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.ClientResult_Parameter_One), (1234))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task Parameter_Many()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var retVal = await group.Single(receiverIdB).ClientResult_Parameter_Many(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal($"{nameof(ITestReceiver.ClientResult_Parameter_Many)}:1234,Hello,True,1234567890", retVal);
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.ClientResult_Parameter_Many), (1234, "Hello", true, 1234567890L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task NotSingle()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var ex = await Record.ExceptionAsync(async () => await group.All.ClientResult_Parameter_Many(1234, "Hello", true, 1234567890L));

        // Assert
        Assert.NotNull(ex);
        Assert.IsType<NotSupportedException>(ex);
        Assert.Empty(receiverA.Received);
        Assert.Empty(receiverB.Received);
        Assert.Empty(receiverC.Received);
        Assert.Empty(receiverD.Received);
    }

    [Fact]
    public async Task NoClientFound()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestInMemoryReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var ex = await Record.ExceptionAsync(async () => await group.Single(Guid.NewGuid()).ClientResult_Parameter_Many(1234, "Hello", true, 1234567890L));

        // Assert
        Assert.NotNull(ex);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Empty(receiverA.Received);
        Assert.Empty(receiverB.Received);
        Assert.Empty(receiverC.Received);
        Assert.Empty(receiverD.Received);
    }
}