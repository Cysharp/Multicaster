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
            .Select(x => (new TestInMemoryReceiver(), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var receiversQueue = new ConcurrentQueue<(TestInMemoryReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(groupId =>
                Task.Run(() =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (receiver, receiverId) = receiverAndId;
                        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>($"MyGroup_{groupId % 3}");
                        group.Add(receiverId, receiver);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        var group1Count = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup_0").Count();
        var group2Count = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup_1").Count();
        var group3Count = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup_2").Count();
        Assert.Equal(10000, group1Count + group2Count + group3Count);
    }

    [Fact]
    public async Task Concurrent_GetOrAddGroup()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new TestInMemoryReceiver(), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var receiversQueue = new ConcurrentQueue<(TestInMemoryReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(groupId =>
                Task.Run(async () =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (receiver, receiverId) = receiverAndId;
                        var group = groupProvider.GetOrAddGroup<Guid, ITestReceiver>($"MyGroup_{groupId % 3}");
                        await group.AddAsync(receiverId, receiver);
                    }
                }))
            .ToArray();

        waiter.Set();
        await Task.WhenAll(tasks);

        // Assert
        var group1Count = await groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup_0").CountAsync();
        var group2Count = await groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup_1").CountAsync();
        var group3Count = await groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup_2").CountAsync();
        Assert.Equal(10000, group1Count + group2Count + group3Count);
    }

    [Fact]
    public async Task Concurrent_Add()
    {
        // Arrange
        var receivers = Enumerable.Range(0, 10000)
            .Select(x => (new TestInMemoryReceiver(), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        var receiversQueue = new ConcurrentQueue<(TestInMemoryReceiver, Guid)>(receivers);
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
            .Select(x => (new TestInMemoryReceiver(), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        var receiversQueue = new ConcurrentQueue<(TestInMemoryReceiver, Guid)>(receivers);
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
            .Select(x => (new TestInMemoryReceiver(), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        foreach (var (receiver, receiverId) in receivers)
        {
            group.Add(receiverId, receiver);
        }

        // Act
        var receiversQueue = new ConcurrentQueue<(TestInMemoryReceiver, Guid)>(receivers);
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
            .Select(x => (new TestInMemoryReceiver(), Guid.NewGuid()))
            .ToArray();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup");
        foreach (var (receiver, receiverId) in receivers)
        {
            await group.AddAsync(receiverId, receiver);
        }

        // Act
        var receiversQueue = new ConcurrentQueue<(TestInMemoryReceiver, Guid)>(receivers);
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
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup");

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
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
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
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup");
        await group.AddAsync(receiverIdA, receiverA);
        await group.AddAsync(receiverIdB, receiverB);

        // Act
        await group.RemoveAsync(receiverIdA);

        // Assert
        Assert.Equal(1, await group.CountAsync());
    }

    [Fact]
    public void Parameter_Zero()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_Zero();

        // Assert
        Assert.Equal([(nameof(ITestReceiver.Parameter_Zero), TestInMemoryReceiver.ParameterZeroArgument)], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Zero), TestInMemoryReceiver.ParameterZeroArgument)], receiverB.Received);
    }

    [Fact]
    public void Parameter_One()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_One(1234);

        // Assert
        Assert.Equal([(nameof(ITestReceiver.Parameter_One), (1234))], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_One), (1234))], receiverB.Received);
    }

    [Fact]
    public void Parameter_Two()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_Two(1234, "Hello");

        // Assert
        Assert.Equal([(nameof(ITestReceiver.Parameter_Two), (1234, "Hello"))], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Two), (1234, "Hello"))], receiverB.Received);
    }

    [Fact]
    public void Parameter_Many()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);

        // Act
        group.All.Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
    }

    [Fact]
    public void Group_Separation()
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
        var groupA = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupA");
        var groupB = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupB");
        groupA.Add(receiverIdA, receiverA);
        groupA.Add(receiverIdB, receiverB);
        groupB.Add(receiverIdC, receiverC);
        groupB.Add(receiverIdD, receiverD);

        // Act
        groupA.All.Parameter_Many(1234, "Hello", true, 9876543210L);
        groupB.All.Parameter_Two(4321, "Konnichiwa");

        // Assert
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Two), (4321, "Konnichiwa"))], receiverC.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Two), (4321, "Konnichiwa"))], receiverD.Received);
    }

    [Fact]
    public void IgnoreExceptions()
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
        var groupA = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupA");
        groupA.Add(receiverIdA, receiverA);
        groupA.Add(receiverIdB, receiverB);
        groupA.Add(receiverIdC, receiverC);
        groupA.Add(receiverIdD, receiverD);

        // Act
        var ex = Record.Exception(() => groupA.All.Throw());

        // Assert
        Assert.Equal([(nameof(ITestReceiver.Throw), TestInMemoryReceiver.ParameterZeroArgument)], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Throw), TestInMemoryReceiver.ParameterZeroArgument)], receiverB.Received);
        Assert.Equal([(nameof(ITestReceiver.Throw), TestInMemoryReceiver.ParameterZeroArgument)], receiverC.Received);
        Assert.Equal([(nameof(ITestReceiver.Throw), TestInMemoryReceiver.ParameterZeroArgument)], receiverD.Received);
    }

    [Fact]
    public void Except()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);

        // Act
        group.Except([receiverIdA, receiverIdC]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
    }

    [Fact]
    public void Only()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);

        // Act
        group.Only([receiverIdA, receiverIdC]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverA.Received);
        Assert.Equal([], receiverB.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverC.Received);
    }

    [Fact]
    public void Single()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverIdA, receiverA);
        group.Add(receiverIdB, receiverB);
        group.Add(receiverIdC, receiverC);

        // Act
        group.Single(receiverIdB).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Received);
        Assert.Equal([(nameof(ITestReceiver.Parameter_Many), (1234, "Hello", true, 9876543210L))], receiverB.Received);
        Assert.Equal([], receiverC.Received);
    }

    [Fact]
    public void Single_NotContains()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();
        var receiverC = new TestInMemoryReceiver();
        var receiverIdC = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
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
    public void Case_1()
    {
        // Arrange
        var receiverA = new GreeterReceiver("A");
        var receiverIdA = Guid.NewGuid();
        var receiverB = new GreeterReceiver("B");
        var receiverIdB = Guid.NewGuid();

        IMulticastGroupProvider groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, IGreeterReceiver>("MyGroup");
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

    [Fact]
    public void Groups_Created()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        var groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Assert
        Assert.Single(groupProvider.AsPrivateProxy()._groups);
    }

    [Fact]
    public void Groups_Disposed()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        var groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Dispose();

        // Assert
        Assert.Empty(groupProvider.AsPrivateProxy()._groups);
    }

    [Fact]
    public void UseSameNameGroupAfterDispose()
    {
        // Arrange
        var receiverA = new TestInMemoryReceiver();
        var receiverIdA = Guid.NewGuid();
        var receiverB = new TestInMemoryReceiver();
        var receiverIdB = Guid.NewGuid();

        var groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

        // Act & Assert
        {
            var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
            group.Add(receiverIdA, receiverA);
            group.Remove(receiverIdA);

            // Dispose the group "MyGroup".
            group.Dispose();
        }
        {
            // Expect to recreate the group "MyGroup" after disposing.
            var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
            group.Add(receiverIdB, receiverB);
            group.Remove(receiverIdB);

            // Dispose the group "MyGroup".
            group.Dispose();
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
