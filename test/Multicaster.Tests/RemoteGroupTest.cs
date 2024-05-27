using System.Collections.Concurrent;
using System.Text.Json;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Internal;
using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

public class RemoteGroupTest
{
    [Fact]
    public void Add()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.All.Parameter_One(1234);

        // Assert
        Assert.Equal(2, group.Count());
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverA.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverB.Writer.Written);
    }

    [Fact]
    public void Remove()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Remove(receiverB.Id);
        group.Remove(receiverD.Id);
        group.All.Parameter_One(1234);

        // Assert
        Assert.Equal(2, group.Count());
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
    }

    [Fact]
    public async Task Concurrent_Add()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receivers = Enumerable.Range(0, 10000)
            .Select(x => TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks))
            .ToArray();

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Act
        var receiversQueue = new ConcurrentQueue<(TestRemoteReceiverWriter, ITestReceiver, Guid)>(receivers);
        var waiter = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 8).Select(_ =>
                Task.Run(() =>
                {
                    waiter.Wait();
                    while (receiversQueue.TryDequeue(out var receiverAndId))
                    {
                        var (writer, receiver, receiverId) = receiverAndId;
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
    public void Parameter_Zero()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);

        // Act
        group.All.Parameter_Zero();

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverB.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Parameter_Many()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);

        // Act
        group.All.Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Group_Separation()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var groupA = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupA");
        var groupB = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupB");
        groupA.Add(receiverA.Id, receiverA.Proxy);
        groupA.Add(receiverB.Id, receiverB.Proxy);
        groupB.Add(receiverC.Id, receiverC.Proxy);
        groupB.Add(receiverD.Id, receiverD.Proxy);

        // Act
        groupA.All.Parameter_Many(1234, "Hello", true, 9876543210L);
        groupB.All.Parameter_Two(4321, "Konnichiwa");

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [4321, "Konnichiwa"]))], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [4321, "Konnichiwa"]))], receiverD.Writer.Written);
        Assert.Equal(2, serializer.SerializeInvocationCallCount);
    }


    [Fact]
    public void IgnoreExceptions()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var groupA = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupA");
        groupA.Add(receiverA.Id, receiverA.Proxy);
        groupA.Add(receiverB.Id, receiverB.Proxy);
        groupA.Add(receiverC.Id, receiverC.Proxy);
        groupA.Add(receiverD.Id, receiverD.Proxy);

        // Act
        var ex = Record.Exception(() => groupA.All.Throw());

        // Assert
        Assert.Null(ex);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Except()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);

        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Except([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Only()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);

        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Only([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Single()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Single(receiverB.Id).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Single_NotContains()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var receiverA = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverB = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverC = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);
        var receiverD = TestRemoteReceiverHelper.CreateReceiverSet(proxyFactory, serializer, pendingTasks);

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group.Add(receiverC.Id, receiverC.Proxy);
        group.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Single(Guid.NewGuid()).Parameter_Many(1234, "Hello", true, 9876543210L);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Groups_Created()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);

        // Act
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Assert
        Assert.Single(groupProvider.AsPrivateProxy()._groups);
    }

    [Fact]
    public void Groups_Disposed()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingTasks = new RemoteClientResultPendingTaskRegistry();

        var groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer, pendingTasks);

        // Act
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Dispose();

        // Assert
        Assert.Empty(groupProvider.AsPrivateProxy()._groups);
    }
}