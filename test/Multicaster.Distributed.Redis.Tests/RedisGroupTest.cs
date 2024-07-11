using System.Text.Json;
using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.Distributed.Redis;
using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Internal;
using Cysharp.Runtime.Multicast.Remoting;
using Multicaster.Tests;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Multicaster.Distributed.Redis.Tests;

public class RedisGroupTest
{
    private readonly CancellationTokenSource _timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    protected CancellationToken TimeoutToken => _timeoutTokenSource.Token;

    private readonly RedisContainer _redisContainer;

    public RedisGroupTest()
    {
        _redisContainer = new RedisBuilder().Build();
        _redisContainer.StartAsync(TimeoutToken).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Broadcast()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        group.All.Parameter_One(1234);
        group2.All.Parameter_One(5678);

        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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
        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        group.All.Parameter_One(1234);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);
        group.Dispose(); // Dispose `group` immediately. The group will unsubscribe the channel.
        group2.All.Parameter_One(5678);

        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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
        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        group.All.Parameter_One(1234);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

        group.Remove(receiverA.Id);
        group.Remove(receiverB.Id); // All members of the `group` are removed from the group. `group` will unsubscribe the channel.
        
        group2.All.Parameter_One(5678);
        
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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
        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
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
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);
        group.All.Parameter_One(1234);
        group2.All.Parameter_One(5678);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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
        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        var group = groupProvider.GetOrAddGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
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
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);
        group.All.Parameter_One(1234);
        group2.All.Parameter_One(5678);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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
    public async Task Parameter_Zero()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.All.Parameter_Zero();
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.All.Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var groupA1 = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupA");
        using var groupB1 = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupB");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var groupA2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupA");
        using var groupB2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroupB");
        groupA1.Add(receiverA.Id, receiverA.Proxy);
        groupA2.Add(receiverB.Id, receiverB.Proxy);
        groupB1.Add(receiverC.Id, receiverC.Proxy);
        groupB2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        groupA1.All.Parameter_Many(1234, "Hello via GroupA; Area=1", true, 9876543210L);
        groupB1.All.Parameter_Two(4321, "Konnichiwa via GroupB; Area=1");
        groupA2.All.Parameter_Many(5678, "Hey via GroupA; Area=2", false, 1234567890L);
        groupB2.All.Parameter_Two(8765, "Hi via GroupB; Area=2");
        // We need to wait to receive the message from Redis.
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

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        var ex = Record.Exception(() => group.All.Throw());
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Except([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Only([receiverA.Id, receiverC.Id]).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Single(receiverB.Id).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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

        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = _redisContainer.GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        // Act
        group.Single(Guid.NewGuid()).Parameter_Many(1234, "Hello", true, 9876543210L);
        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

        // Assert
        Assert.Equal([], receiverA.Writer.Written);
        Assert.Equal([], receiverB.Writer.Written);
        Assert.Equal([], receiverC.Writer.Written);
        Assert.Equal([], receiverD.Writer.Written);
        Assert.Equal(1, serializer.SerializeInvocationCallCount);
    }
}