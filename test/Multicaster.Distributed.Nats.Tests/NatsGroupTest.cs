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

    private static string CreateJsonSerializedInvocation(string nameOfMethod, IReadOnlyList<object?> args)
        => CreateJsonSerializedInvocation(nameOfMethod, null, args);
    private static string CreateJsonSerializedInvocation(string nameOfMethod, Guid? messageId, IReadOnlyList<object?> args)
        => JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameOfMethod, FNV1A32.GetHashCode(nameOfMethod), messageId, args));

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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        group.All.Parameter_One(1234);
        await Task.Delay(250);
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
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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
    public async Task RemoveViaOtherGroupProvider()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // MyGroup
        //   - GroupProvider(1): A, B
        //   - GroupProvider(2): C, D
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Remove(receiverC.Id); // Remove C on GroupProvider(2) via GroupProvider(1).
        group2.Remove(receiverA.Id); // Remove A on GroupProvider(1) via GroupProvider(2).
        await Task.Delay(100); // We need to wait to receive the message from NATS.
        group.All.Parameter_One(1234);
        await Task.Delay(250); // Ensure order of transmission
        group2.All.Parameter_One(5678);
        await Task.Delay(100); // We need to wait to receive the message from NATS.

        // Assert
        // MyGroup
        //   - GroupProvider(1): B
        //   - GroupProvider(2): D
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverD.Writer.Written);
    }

    [Fact]
    public async Task RemoveViaOtherGroupProvider_Async()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddGroup<Guid, ITestReceiver>("MyGroup");

        // MyGroup
        //   - GroupProvider(1): A, B
        //   - GroupProvider(2): C, D
        await group.AddAsync(receiverA.Id, receiverA.Proxy);
        await group.AddAsync(receiverB.Id, receiverB.Proxy);
        await group2.AddAsync(receiverC.Id, receiverC.Proxy);
        await group2.AddAsync(receiverD.Id, receiverD.Proxy);

        // Act
        await group.RemoveAsync(receiverC.Id); // Remove C on GroupProvider(2) via GroupProvider(1).
        await group2.RemoveAsync(receiverA.Id); // Remove A on GroupProvider(1) via GroupProvider(2).
        
        
        await Task.Delay(100); // We need to wait to receive the message from NATS.
        group.All.Parameter_One(1234);
        await Task.Delay(250); // Ensure order of transmission
        group2.All.Parameter_One(5678);
        await Task.Delay(100); // We need to wait to receive the message from NATS.

        // Assert
        // MyGroup
        //   - GroupProvider(1): B
        //   - GroupProvider(2): D
        if (receiverB.Writer.Written[0].Contains("5678")) System.Diagnostics.Debugger.Break();
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal(["""{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[1234]}""", """{"MethodName":"Parameter_One","MethodId":1979862359,"MessageId":null,"Arguments":[5678]}"""], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
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
        using var groupA1 = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupA");
        using var groupB1 = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupB");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var groupA2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupA");
        using var groupB2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupB");
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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

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


    [Fact]
    public async Task Single_Only_Except()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // NOTE: NATS guarantees order per publisher, but not across publishers. In this case, each group is a publisher, so we need to call them in order to maintain order.
        group.Single(receiverA.Id).Parameter_One(1234);
        group.Only([receiverB.Id]).Parameter_One(4567);
        group.Single(receiverC.Id).Parameter_One(9876);
        group.Except([receiverA.Id, receiverB.Id, receiverC.Id]).Parameter_One(1098);
        await Task.Delay(250);
        group2.Single(receiverA.Id).Parameter_One(5678);
        group2.Only([receiverB.Id]).Parameter_One(8910);
        group2.Single(receiverC.Id).Parameter_One(5432);
        group2.Except([receiverA.Id, receiverB.Id, receiverC.Id]).Parameter_One(7654);

        // We need to wait to receive the message from Redis.
        await Task.Delay(250);

        // Assert
        System.Diagnostics.Debug.WriteLine("Assert#1");
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverA.Writer.Written);
        System.Diagnostics.Debug.WriteLine("Assert#2");
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [4567]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [8910])], receiverB.Writer.Written);
        System.Diagnostics.Debug.WriteLine("Assert#3");
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [9876]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5432])], receiverC.Writer.Written);
        System.Diagnostics.Debug.WriteLine("Assert#4");
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1098]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [7654])], receiverD.Writer.Written);
    }

    [Fact]
    public async Task KeysAreNotGuid()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<string, ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id.ToString(), receiverA.Proxy);
        group.Add(receiverB.Id.ToString(), receiverB.Proxy);
        group2.Add(receiverC.Id.ToString(), receiverC.Proxy);
        group2.Add(receiverD.Id.ToString(), receiverD.Proxy);

        // Wait for subscriptions to be established.
        await Task.Delay(250);

        // NOTE: NATS guarantees order per publisher, but not across publishers. In this case, each group is a publisher, so we need to call them in order to maintain order.
        group.Single(receiverA.Id.ToString()).Parameter_One(1234);
        group.Only([receiverB.Id.ToString()]).Parameter_One(4567);
        group.Single(receiverC.Id.ToString()).Parameter_One(9876);
        group.Except([receiverA.Id.ToString(), receiverB.Id.ToString(), receiverC.Id.ToString()]).Parameter_One(1098);
        await Task.Delay(250);
        group2.Single(receiverA.Id.ToString()).Parameter_One(5678);
        group2.Only([receiverB.Id.ToString()]).Parameter_One(8910);
        group2.Single(receiverC.Id.ToString()).Parameter_One(5432);
        group2.Except([receiverA.Id.ToString(), receiverB.Id.ToString(), receiverC.Id.ToString()]).Parameter_One(7654);

        // We need to wait to receive the message from Redis.
        await Task.Delay(500);

        // Assert
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverA.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [4567]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [8910])], receiverB.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [9876]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5432])], receiverC.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1098]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [7654])], receiverD.Writer.Written);
    }

    [Fact]
    public async Task UseSameNameGroupAfterDispose()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        IMulticastGroupProvider groupProvider = new NatsGroupProvider(proxyFactory, serializer, new NatsGroupOptions() { Url = _natsContainer.GetConnectionString() });
        var receiverA = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestNatsReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        // Act & Assert
        {
            var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("MyGroup");
            group.Add(receiverA.Id.ToString(), receiverA.Proxy);

            group.All.Parameter_One(1234);
            // We need to wait to receive the message from NATS.
            await Task.Delay(250);

            Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234])], receiverA.Writer.Written);
            Assert.Equal([], receiverB.Writer.Written);

            group.Remove(receiverA.Id.ToString());

            // Dispose the group "MyGroup".
            group.Dispose();
        }
        {
            // Expect to recreate the group "MyGroup" after disposing.
            var group = groupProvider.GetOrAddSynchronousGroup<string, ITestReceiver>("MyGroup");
            group.Add(receiverB.Id.ToString(), receiverB.Proxy);

            group.All.Parameter_One(5678);
            // We need to wait to receive the message from NATS.
            await Task.Delay(250);

            Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234])], receiverA.Writer.Written);
            Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverB.Writer.Written);

            group.Remove(receiverB.Id.ToString());
            group.Dispose();
        }
    }
}