using System.Collections.Concurrent;

using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast;

namespace Multicaster.Tests;

public class InMemoryGroupTest
{
    [Fact]
    public async Task Concurrent_GetOrAddSynchronousGroup()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new GreeterReceiver(x.ToString()), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var receiversQueue = new ConcurrentQueue<(GreeterReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(groupId =>
                Task.Run(() =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (receiver, receiverId) = receiverAndId;
                        var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>($"MyGroup_{groupId % 3}");
                        group.Add(receiverId, receiver);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        var group1Count = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup_0").Count();
        var group2Count = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup_1").Count();
        var group3Count = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup_2").Count();
        Assert.Equal(10000, group1Count + group2Count + group3Count);
    }

    [Fact]
    public async Task Concurrent_GetOrAddGroup()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new GreeterReceiver(x.ToString()), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var receiversQueue = new ConcurrentQueue<(GreeterReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(groupId =>
                Task.Run(async () =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (receiver, receiverId) = receiverAndId;
                        var group = groupProvider.GetOrAddGroup<IGreeterReceiver>($"MyGroup_{groupId % 3}");
                        await group.AddAsync(receiverId, receiver);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        var group1Count = await groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup_0").CountAsync();
        var group2Count = await groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup_1").CountAsync();
        var group3Count = await groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup_2").CountAsync();
        Assert.Equal(10000, group1Count + group2Count + group3Count);
    }

    [Fact]
    public async Task Concurrent_Add()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new GreeterReceiver(x.ToString()), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup");

        // Act
        var receiversQueue = new ConcurrentQueue<(GreeterReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(_ =>
            Task.Run(() =>
            {
                waiter.Wait();
                while (receiversQueue.TryDequeue(out var receiverAndId))
                {
                    var (receiver, receiverId) = receiverAndId;
                    group.Add(receiverId, receiver);
                }
            }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10000, group.Count());
    }

    [Fact]
    public async Task Concurrent_AddAsync()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new GreeterReceiver(x.ToString()), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup");

        // Act
        var receiversQueue = new ConcurrentQueue<(GreeterReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(_ =>
                Task.Run(async () =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (receiver, receiverId) = receiverAndId;
                        await group.AddAsync(receiverId, receiver);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10000, await group.CountAsync());
    }

    [Fact]
    public async Task Concurrent_Remove()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new GreeterReceiver(x.ToString()), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup");
        foreach (var (receiver, receiverId) in receivers)
        {
            group.Add(receiverId, receiver);
        }

        // Act
        var receiversQueue = new ConcurrentQueue<(GreeterReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(_ =>
                Task.Run(() =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (_, receiverId) = receiverAndId;
                        group.Remove(receiverId);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(0, group.Count());
    }

    [Fact]
    public async Task Concurrent_RemoveAsync()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new GreeterReceiver(x.ToString()), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup");
        foreach (var (receiver, receiverId) in receivers)
        {
            await group.AddAsync(receiverId, receiver);
        }

        // Act
        var receiversQueue = new ConcurrentQueue<(GreeterReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(_ =>
                Task.Run(async () =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (_, receiverId) = receiverAndId;
                        await group.RemoveAsync(receiverId);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(0, await group.CountAsync());
    }

    [Fact]
    public void Add()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup");

        // Act
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Assert
        Assert.Equal(2, group.Count());
    }

    [Fact]
    public async Task AddAsync()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup");

        // Act
        await group.AddAsync(receiverIdA, receiverA);
        await group.AddAsync(receiverIdB, receiverB);

        // Assert
        Assert.Equal(2, await group.CountAsync());
    }

    [Fact]
    public void Remove()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.Remove(receiverIdA);

        // Assert
        Assert.Equal(1, group.Count());
    }

    [Fact]
    public async Task RemoveAsync()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<IGreeterReceiver>("MyGroup");
        await group.AddAsync(receiverIdA, receiverA);
        await group.AddAsync(receiverIdB, receiverB);

        // Act
        await group.RemoveAsync(receiverIdA);

        // Assert
        Assert.Equal(1, await group.CountAsync());
    }

    [Fact]
    public void Broadcast()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.OnMessage("Sender1", "Hello");
        group.All.OnMessage("Sender2", "World");

        // Assert
        Assert.Equal([
            ("Sender1", "Hello"),
            ("Sender2", "World"),
        ], receiverA.Received);

        Assert.Equal([
            ("Sender1", "Hello"),
            ("Sender2", "World"),
        ], receiverB.Received);
    }

    [Fact]
    public void Parameter_Zero()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_Zero();

        // Assert
        Assert.Equal([(nameof(Parameter_Zero), TestReceiver.ParameterZeroArgument)], receiverA.Received);
        Assert.Equal([(nameof(Parameter_Zero), TestReceiver.ParameterZeroArgument)], receiverB.Received);
    }

    [Fact]
    public void Parameter_One()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_One(1234);

        // Assert
        Assert.Equal([(nameof(Parameter_One), (1234))], receiverA.Received);
        Assert.Equal([(nameof(Parameter_One), (1234))], receiverB.Received);
    }

    [Fact]
    public void Parameter_Two()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_Two(1234, "Hello");

        // Assert
        Assert.Equal([(nameof(Parameter_Two), (1234, "Hello"))], receiverA.Received);
        Assert.Equal([(nameof(Parameter_Two), (1234, "Hello"))], receiverB.Received);
    }

    [Fact]
    public void Parameter_Many()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverA.Received);
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
    }

    [Fact]
    public void Group_Separation()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var groupA = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupA");
        var groupB = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupB");
        groupA.Add(receiverIdA, receiverA);
        groupA.Add(receiverIdB, receiverB);
        groupB.Add(receiverIdC, receiverC);
        groupB.Add(receiverIdD, receiverD);

        // Act
        groupA.All.Parameter_Many(1234, "Hello", true, 9876543210L);
        groupB.All.Parameter_Two(4321, "Konnichiwa");

        // Assert
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverA.Received);
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
        Assert.Equal([(nameof(Parameter_Two), (4321, "Konnichiwa"))], receiverC.Received);
        Assert.Equal([(nameof(Parameter_Two), (4321, "Konnichiwa"))], receiverD.Received);
    }

    [Fact]
    public void Except()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);

        // Act
        group.Except([receiverIdA, receiverIdC]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
    }

    [Fact]
    public void Only()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);

        // Act
        group.Only([receiverIdA, receiverIdC]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverA.Received);
        Assert.Equal([], receiverB.Received);
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverC.Received);
    }

    [Fact]
    public void Single()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);

        // Act
        group.Single(receiverIdB).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
    }

    [Fact]
    public void Single_NotContains()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);

        // Act
        group.Single(Guid.NewGuid()).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([], receiverB.Received);
        Assert.Equal([], receiverC.Received);
    }

    [Fact]
    public async Task ClientInvoke_Parameter_Zero_NoReturnValue()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        await group.Single(receiverIdB).ClientInvoke_Parameter_Zero_NoReturnValue();

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ClientInvoke_Parameter_Zero_NoReturnValue), TestReceiver.ParameterZeroArgument)], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task ClientInvoke_Parameter_One_NoReturnValue()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        await group.Single(receiverIdB).ClientInvoke_Parameter_One_NoReturnValue(1234);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ClientInvoke_Parameter_One_NoReturnValue), (1234))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task ClientInvoke_Parameter_Many_NoReturnValue()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        await group.Single(receiverIdB).ClientInvoke_Parameter_Many_NoReturnValue(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ClientInvoke_Parameter_Many_NoReturnValue), (1234, "Hello", true, 1234567890L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task ClientInvoke_Parameter_Zero()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var retVal = await group.Single(receiverIdB).ClientInvoke_Parameter_Zero();

        // Assert
        Assert.Equal($"{nameof(ClientInvoke_Parameter_Zero)}", retVal);
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ClientInvoke_Parameter_Zero), TestReceiver.ParameterZeroArgument)], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task ClientInvoke_Parameter_One()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var retVal = await group.Single(receiverIdB).ClientInvoke_Parameter_One(1234);

        // Assert
        Assert.Equal($"{nameof(ClientInvoke_Parameter_One)}:1234", retVal);
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ClientInvoke_Parameter_One), (1234))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task ClientInvoke_Parameter_Many()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var retVal = await group.Single(receiverIdB).ClientInvoke_Parameter_Many(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal($"{nameof(ClientInvoke_Parameter_Many)}:1234,Hello,True,1234567890", retVal);
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ClientInvoke_Parameter_Many), (1234, "Hello", true, 1234567890L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
        Assert.Equal([], receiverD.Received);
    }

    [Fact]
    public async Task ClientInvoke_NotSingle()
    {
        // Arrange
        var receiverA = new TestReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestReceiver();
        var receiverIdC = Guid.NewGuid();
        var receiverD = new TestReceiver();
        var receiverIdD = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);
        group.Add(receiverIdD, receiverD);

        // Act
        var ex = await Record.ExceptionAsync(async () => await group.All.ClientInvoke_Parameter_Many(1234, "Hello", true, 1234567890L));

        // Assert
        Assert.NotNull(ex);
        Assert.IsType<NotSupportedException>(ex);
        Assert.Empty(receiverA.Received);
        Assert.Empty(receiverB.Received);
        Assert.Empty(receiverC.Received);
        Assert.Empty(receiverD.Received);
    }

    [Fact]
    public void Case_1()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
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

    public interface ITestReceiver
    {
        void Parameter_Zero();
        void Parameter_One(int arg1);
        void Parameter_Two(int arg1, string arg2);
        void Parameter_Many(int arg1, string arg2, bool arg3, long arg4);

        Task ClientInvoke_Parameter_Zero_NoReturnValue();
        Task ClientInvoke_Parameter_One_NoReturnValue(int arg1);
        Task ClientInvoke_Parameter_Many_NoReturnValue(int arg1, string arg2, bool arg3, long arg4);
        Task<string> ClientInvoke_Parameter_Zero();
        Task<string> ClientInvoke_Parameter_One(int arg1);
        Task<string> ClientInvoke_Parameter_Many(int arg1, string arg2, bool arg3, long arg4);
    }

    public class TestReceiver : ITestReceiver
    {
        public static readonly object ParameterZeroArgument = new();

        public List<(string Name, object? Arguments)> Received { get; } = new ();
        
        public void Parameter_Zero()
            => Received.Add((nameof(Parameter_Zero), ParameterZeroArgument));

        public void Parameter_One(int arg1)
            => Received.Add((nameof(Parameter_One), (arg1)));

        public void Parameter_Two(int arg1, string arg2)
            => Received.Add((nameof(Parameter_Two), (arg1, arg2)));

        public void Parameter_Many(int arg1, string arg2, bool arg3, long arg4)
            => Received.Add((nameof(Parameter_Many), (arg1, arg2, arg3, arg4)));

        public async Task ClientInvoke_Parameter_Zero_NoReturnValue()
        {
            Received.Add((nameof(ClientInvoke_Parameter_Zero_NoReturnValue), ParameterZeroArgument));
            await Task.Delay(500);
        }

        public async Task ClientInvoke_Parameter_One_NoReturnValue(int arg1)
        {
            Received.Add((nameof(ClientInvoke_Parameter_One_NoReturnValue), (arg1)));
            await Task.Delay(500);
        }

        public async Task ClientInvoke_Parameter_Many_NoReturnValue(int arg1, string arg2, bool arg3, long arg4)
        {
            Received.Add((nameof(ClientInvoke_Parameter_Many_NoReturnValue), (arg1, arg2, arg3, arg4)));
            await Task.Delay(500);
        }

        public async Task<string> ClientInvoke_Parameter_Zero()
        {
            Received.Add((nameof(ClientInvoke_Parameter_Zero), ParameterZeroArgument));
            await Task.Delay(500);
            return nameof(ClientInvoke_Parameter_Zero);
        }

        public async Task<string> ClientInvoke_Parameter_One(int arg1)
        {
            Received.Add((nameof(ClientInvoke_Parameter_One), (arg1)));
            await Task.Delay(500);
            return $"{nameof(ClientInvoke_Parameter_One)}:{arg1}";
        }

        public async Task<string> ClientInvoke_Parameter_Many(int arg1, string arg2, bool arg3, long arg4)
        {
            Received.Add((nameof(ClientInvoke_Parameter_Many), (arg1, arg2, arg3, arg4)));
            await Task.Delay(500);
            return $"{nameof(ClientInvoke_Parameter_Many)}:{arg1},{arg2},{arg3},{arg4}";
        }
    }

    public interface IGreeterReceiver
    {
        void OnMessage(string name, string message);
        Task<string> HelloAsync(string name, int age);
    }

    class GreeterReceiver(string name) : IGreeterReceiver
    {
        public string Name { get; } = name;
        public List<(string Name, string Message)> Received { get; } = new();

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