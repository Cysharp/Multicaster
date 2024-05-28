using System.Text.Json;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.Distributed.Nats;
using Cysharp.Runtime.Multicast.Internal;
using Cysharp.Runtime.Multicast.Remoting;

using Multicaster.Tests;

using Testcontainers.Nats;

namespace Multicaster.Distributed.Nats.Tests;

public class NatsGroupTest
{
    private readonly CancellationTokenSource _timeoutTokenSource = new (TimeSpan.FromSeconds(60));
    protected CancellationToken TimeoutToken => _timeoutTokenSource.Token;

    private readonly NatsContainer _natsContainer;

    public NatsGroupTest()
    {
        _natsContainer = new NatsBuilder().Build();
        _natsContainer.StartAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Broadcast()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        group.All.Parameter_One(1234);
        group2.All.Parameter_One(5678);

        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverA.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverB.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverC.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverD.Writer.Written);
    }

    [Fact]
    public async Task DisposeAndUnsubscribe()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        group.All.Parameter_One(1234);
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);
        group.Dispose(); // Dispose `group` immediately. The group will unsubscribe the channel.
        group2.All.Parameter_One(5678);

        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverA.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverB.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverC.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverD.Writer.Written);
    }


    [Fact]
    public async Task RemoveAllMembersAndUnsubscribe()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        group.All.Parameter_One(1234);
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        group.Remove(receiverA.Id);
        group.Remove(receiverB.Id); // All members of the `group` are removed from the group. `group` will unsubscribe the channel.

        group2.All.Parameter_One(5678);

        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverA.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}"""], receiverB.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverC.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverD.Writer.Written);
    }

    [Fact]
    public async Task Parameter_Zero()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // Act
        group.All.Parameter_Zero();
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Zero), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Zero)), null, Array.Empty<object>()))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task Parameter_Many()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // Act
        group.All.Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task Group_Separation()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var groupA1 = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupA");
        using var groupB1 = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupB");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var groupA2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupA");
        using var groupB2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroupB");
        groupA1.Add(receiverA.Id, receiverA.Proxy);
        groupA2.Add(receiverB.Id, receiverB.Proxy);
        groupB1.Add(receiverC.Id, receiverC.Proxy);
        groupB2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(100);

        // Act
        groupA1.All.Parameter_Many(1234, "Hello via GroupA; Area=1", true, 9876543210L);
        await Task.Delay(100);
        groupB1.All.Parameter_Two(4321, "Konnichiwa via GroupB; Area=1");
        await Task.Delay(100);
        groupA2.All.Parameter_Many(5678, "Hey via GroupA; Area=2", false, 1234567890L);
        await Task.Delay(100);
        groupB2.All.Parameter_Two(8765, "Hi via GroupB; Area=2");
        
        // We need to wait to receive the message from NATS.
        await Task.Delay(100);

        // Assert
        Assert.Equal([
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello via GroupA; Area=1", true, 9876543210L])),
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [5678, "Hey via GroupA; Area=2", false, 1234567890L])),
        ], receiverA.Writer.Written);
        Assert.Equal([
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello via GroupA; Area=1", true, 9876543210L])),
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [5678, "Hey via GroupA; Area=2", false, 1234567890L])),
        ], receiverB.Writer.Written);
        Assert.Equal([
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [4321, "Konnichiwa via GroupB; Area=1"])),
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [8765, "Hi via GroupB; Area=2"])),
        ], receiverC.Writer.Written); Assert.Equal([
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [4321, "Konnichiwa via GroupB; Area=1"])),
            JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Two), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Two)), null, [8765, "Hi via GroupB; Area=2"])),
        ], receiverD.Writer.Written);
        Assert.Equal(4, serializer.SerializeInvocationCallCount);
    }


    [Fact]
    public async Task IgnoreExceptions()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // Act
        var ex = Record.Exception(() => group.All.Throw());
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Null(ex);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Throw), FNV1A32.GetHashCode(nameof(ITestReceiver.Throw)), null, Array.Empty<object>()))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task Except()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // Act
        group.Except([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task Only()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // Act
        group.Only([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task Single()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // Act
        group.Single(receiverB.Id).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameof(ITestReceiver.Parameter_Many), FNV1A32.GetHashCode(nameof(ITestReceiver.Parameter_Many)), null, [1234, "Hello", true, 9876543210L]))], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }

    [Fact]
    public async Task Single_NotContains()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // Act
        group.Single(Guid.NewGuid()).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from NATS.
        await Task.Delay(250);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }
}