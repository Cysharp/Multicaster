using System.Collections.Concurrent;

using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

public class DynamicRemoteProxyFactoryTest
{
    [Fact]
    public void CreateDirect()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingQueue = new RemoteCallPendingMessageQueue();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var proxy = proxyFactory.CreateDirect<ITestReceiver>(receiverWriterA, serializer, pendingQueue);

        // Act
        proxy.Parameter_Many(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal(["""{"Method":"Parameter_Many","MessageId":null,"Arguments":[1234,"Hello",true,1234567890]}"""], receiverWriterA.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Create_Multiple_Parameter_Zero()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingQueue = new RemoteCallPendingMessageQueue();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverWriterB = new TestRemoteReceiverWriter();
        var receivers = new ConcurrentDictionary<Guid, IRemoteReceiverWriter>();
        receivers.TryAdd(Guid.NewGuid(), receiverWriterA);
        receivers.TryAdd(Guid.NewGuid(), receiverWriterB);

        var proxy = proxyFactory.Create<ITestReceiver>(receivers, serializer, pendingQueue);

        // Act
        proxy.Parameter_Zero();

        // Assert
        Assert.Equal(["""{"Method":"Parameter_Zero","MessageId":null,"Arguments":[]}"""], receiverWriterA.Written);
        Assert.Equal(["""{"Method":"Parameter_Zero","MessageId":null,"Arguments":[]}"""], receiverWriterB.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Create_Multiple_Parameter_One()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingQueue = new RemoteCallPendingMessageQueue();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverWriterB = new TestRemoteReceiverWriter();
        var receivers = new ConcurrentDictionary<Guid, IRemoteReceiverWriter>();
        receivers.TryAdd(Guid.NewGuid(), receiverWriterA);
        receivers.TryAdd(Guid.NewGuid(), receiverWriterB);

        var proxy = proxyFactory.Create<ITestReceiver>(receivers, serializer, pendingQueue);

        // Act
        proxy.Parameter_One(1234);

        // Assert
        Assert.Equal(["""{"Method":"Parameter_One","MessageId":null,"Arguments":[1234]}"""], receiverWriterA.Written);
        Assert.Equal(["""{"Method":"Parameter_One","MessageId":null,"Arguments":[1234]}"""], receiverWriterB.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public void Create_Multiple_Parameter_Many()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var pendingQueue = new RemoteCallPendingMessageQueue();

        var receiverWriterA = new TestRemoteReceiverWriter();
        var receiverWriterB = new TestRemoteReceiverWriter();
        var receivers = new ConcurrentDictionary<Guid, IRemoteReceiverWriter>();
        receivers.TryAdd(Guid.NewGuid(), receiverWriterA);
        receivers.TryAdd(Guid.NewGuid(), receiverWriterB);

        var proxy = proxyFactory.Create<ITestReceiver>(receivers, serializer, pendingQueue);

        // Act
        proxy.Parameter_Many(1234, "Hello", true, 1234567890L);

        // Assert
        Assert.Equal(["""{"Method":"Parameter_Many","MessageId":null,"Arguments":[1234,"Hello",true,1234567890]}"""], receiverWriterA.Written);
        Assert.Equal(["""{"Method":"Parameter_Many","MessageId":null,"Arguments":[1234,"Hello",true,1234567890]}"""], receiverWriterB.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

}