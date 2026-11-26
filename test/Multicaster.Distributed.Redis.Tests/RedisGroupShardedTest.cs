using System.Net.NetworkInformation;
using System.Net.Sockets;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.Distributed.Redis;
using Cysharp.Runtime.Multicast.Remoting;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

using Multicaster.Tests;

using StackExchange.Redis;
using StackExchange.Redis.Profiling;

using Testcontainers.Redis;

namespace Multicaster.Distributed.Redis.Tests;

public class RedisGroupShardedTest : RedisGroupTestBase, IAsyncLifetime, IDisposable
{
    private const string KeyForShard1 = "quux"; // Slot 3850
    private const string KeyForShard2 = "key1"; // Slot 9189
    private const string KeyForShard3 = "foo"; // Slot 12182

    private readonly CancellationTokenSource _timeoutTokenSource = new(TimeSpan.FromSeconds(60));
    private CancellationToken TimeoutToken => _timeoutTokenSource.Token;

    private IFutureDockerImage? _redisClusterImage;
    private IContainer? _redisClusterContainer;


    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        // Create redis-cluster image from Dockerfile
        _redisClusterImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory("redis-cluster")
            .Build();
        await _redisClusterImage.CreateAsync(TimeoutToken);

        // Start redis-cluster
        _redisClusterContainer = new ContainerBuilder()
            .WithImage(_redisClusterImage)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Redis Cluster is ready."))
            .WithPortBinding(30001, 30001)
            .WithPortBinding(30002, 30002)
            .WithPortBinding(30003, 30003)
            .Build();
        await _redisClusterContainer.StartAsync(TimeoutToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_redisClusterContainer is not null)
        {
            await _redisClusterContainer.DisposeAsync();
        }

        if (_redisClusterImage is not null)
        {
            await _redisClusterImage.DisposeAsync();
        }

        Dispose();
    }

    private static string GetHostIpAddress()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .First(x => x.Address.AddressFamily == AddressFamily.InterNetwork /* IPv4 only */)
            .Address
            .ToString();
    }

    // Use the same loopback address that the cluster advertises.
    private static string GetConnectionString()
        => "127.0.0.1:30001";

    private Task WaitForMessagesAsync(int count, params TestRemoteReceiverWriter[] writers)
        => Task.WhenAll(writers.Select(writer => writer.WaitForMessagesAsync(count, TimeoutToken)));

    [Fact]
    public async Task ShardingServerConnectivityTest()
    {
        // In Redis Sharded Pub/Sub, shards are determined based on the hash of the key.
        // The hashing algorithm used is the same as the key hash computation in Redis Cluster.
        // https://redis.io/docs/latest/develop/pubsub/#sharded-pubsub
        // https://redis.io/docs/latest/operate/oss_and_stack/reference/cluster-spec/#key-distribution-model
        //
        // HASH_SLOT = CRC16(key) mod 16384
        //
        // Based on this, the test sets keys to ensure distribution across three shards (i.e., three masters).
        //
        // Shards/Slots:
        // 1: 0–5460
        // 2: 5461–10922
        // 3: 10923–16383

        using var conn1 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString());
        Assert.Equal(3, conn1.GetEndPoints().Length);
        using var conn2 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString());
        using var conn3 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString());
        var subscriber1 = conn1.GetSubscriber();
        var subscriber2 = conn2.GetSubscriber();
        var subscriber3 = conn3.GetSubscriber();
        var channel1 = RedisChannel.Sharded(KeyForShard1);
        var channel2 = RedisChannel.Sharded(KeyForShard2);
        var channel3 = RedisChannel.Sharded(KeyForShard3);
        var queue1 = await subscriber1.SubscribeAsync(channel1);
        var queue2 = await subscriber2.SubscribeAsync(channel2);
        var queue3 = await subscriber3.SubscribeAsync(channel3);

        Assert.Equal(1L, await subscriber1.PublishAsync(channel2, "1->2"));
        Assert.Equal("1->2", (await queue2.ReadAsync(TimeoutToken)).Message.ToString());
        Assert.Equal(1L, await subscriber2.PublishAsync(channel3, "2->3"));
        Assert.Equal("2->3", (await queue3.ReadAsync(TimeoutToken)).Message.ToString());
        Assert.Equal(1L, await subscriber3.PublishAsync(channel1, "3->1"));
        Assert.Equal("3->1", (await queue1.ReadAsync(TimeoutToken)).Message.ToString());
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

        IMulticastGroupProvider groupProvider = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = GetConnectionString() });
        using var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionString = GetConnectionString() });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");

        // Act
        group.Add(receiverA.Id, receiverA.Proxy);
        group.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);

        group.All.Parameter_One(1234);
        await WaitForMessagesAsync(1, receiverA.Writer, receiverB.Writer, receiverC.Writer, receiverD.Writer);
        group2.All.Parameter_One(5678);
        await WaitForMessagesAsync(2, receiverA.Writer, receiverB.Writer, receiverC.Writer, receiverD.Writer);

        // Assert
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverA.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverB.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverC.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverD.Writer.Written);
    }

    class DebugTextWriter : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public override void WriteLine(string? value)
        {
            System.Diagnostics.Debug.WriteLine(value);
        }
    }

    [Fact]
    public async Task Broadcast_Default() // Not sharded, but using the default channel factory
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        using var conn1 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSession1 = new ProfilingSession();
        conn1.RegisterProfiler(() => profSession1);
        IMulticastGroupProvider groupProvider1 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionMultiplexer = conn1 });
        using var group1 = groupProvider1.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        using var conn2 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSession2 = new ProfilingSession();
        conn2.RegisterProfiler(() => profSession2);
        IMulticastGroupProvider groupProvider2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionMultiplexer = conn2 });
        using var group2 = groupProvider2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup_");
        using var conn3 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSession3 = new ProfilingSession();
        conn3.RegisterProfiler(() => profSession3);
        IMulticastGroupProvider groupProvider3 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ConnectionMultiplexer = conn3 });
        using var group3 = groupProvider3.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup__");

        // Act
        // 1[A, B], 2[C, D], 3[A, C]
        group1.Add(receiverA.Id, receiverA.Proxy);
        group1.Add(receiverB.Id, receiverB.Proxy);
        group2.Add(receiverC.Id, receiverC.Proxy);
        group2.Add(receiverD.Id, receiverD.Proxy);
        group3.Add(receiverA.Id, receiverA.Proxy);
        group3.Add(receiverC.Id, receiverC.Proxy);

        group1.All.Parameter_One(1234);
        await WaitForMessagesAsync(1, receiverA.Writer, receiverB.Writer);
        group2.All.Parameter_One(5678);
        await WaitForMessagesAsync(1, receiverC.Writer, receiverD.Writer);
        group3.All.Parameter_One(91011);
        await WaitForMessagesAsync(2, receiverA.Writer, receiverC.Writer);

        // Assert
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [91011])], receiverA.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234])], receiverB.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [91011])], receiverC.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverD.Writer.Written);

        var commands1 = profSession1.FinishProfiling();
        var commands2 = profSession2.FinishProfiling();
        var commands3 = profSession3.FinishProfiling();
        Assert.Contains(commands1, x => x.Command == "PUBLISH");
        Assert.Contains(commands1, x => x.Command == "SUBSCRIBE");
        Assert.Contains(commands2, x => x.Command == "PUBLISH");
        Assert.Contains(commands2, x => x.Command == "SUBSCRIBE");
        Assert.Contains(commands3, x => x.Command == "PUBLISH");
        Assert.Contains(commands3, x => x.Command == "SUBSCRIBE");
    }

    [Fact]
    public async Task Broadcast_Sharded()
    {
        // Arrange
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();
        var receiverA = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverB = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverC = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);
        var receiverD = TestRedisReceiverHelper.CreateReceiverSet(proxyFactory, serializer);

        using var connShard1 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSessionShard1 = new ProfilingSession();
        connShard1.RegisterProfiler(() => profSessionShard1);
        IMulticastGroupProvider groupProviderShard1 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ChannelFactory = RedisChannelFactory.Sharded, ConnectionMultiplexer = connShard1 });
        using var groupShard1 = groupProviderShard1.GetOrAddSynchronousGroup<Guid, ITestReceiver>($"MyGroup{{{KeyForShard1}}}");
        using var connShard2 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSessionShard2 = new ProfilingSession();
        connShard2.RegisterProfiler(() => profSessionShard2);
        IMulticastGroupProvider groupProviderShard2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ChannelFactory = RedisChannelFactory.Sharded, ConnectionMultiplexer = connShard2 });
        using var groupShard2 = groupProviderShard2.GetOrAddSynchronousGroup<Guid, ITestReceiver>($"MyGroup{{{KeyForShard2}}}");
        using var connShard3 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSessionShard3 = new ProfilingSession();
        connShard3.RegisterProfiler(() => profSessionShard3);
        IMulticastGroupProvider groupProviderShard3 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ChannelFactory = RedisChannelFactory.Sharded, ConnectionMultiplexer = connShard3});
        using var groupShard3 = groupProviderShard3.GetOrAddSynchronousGroup<Guid, ITestReceiver>($"MyGroup{{{KeyForShard3}}}");

        // Act
        // Shard1[A, B], Shard2[C, D], Shard3[A, C]
        groupShard1.Add(receiverA.Id, receiverA.Proxy);
        groupShard1.Add(receiverB.Id, receiverB.Proxy);
        groupShard2.Add(receiverC.Id, receiverC.Proxy);
        groupShard2.Add(receiverD.Id, receiverD.Proxy);
        groupShard3.Add(receiverA.Id, receiverA.Proxy);
        groupShard3.Add(receiverC.Id, receiverC.Proxy);

        groupShard1.All.Parameter_One(1234);
        await WaitForMessagesAsync(1, receiverA.Writer, receiverB.Writer);
        groupShard2.All.Parameter_One(5678);
        await WaitForMessagesAsync(1, receiverC.Writer, receiverD.Writer);
        groupShard3.All.Parameter_One(91011);
        await WaitForMessagesAsync(2, receiverA.Writer, receiverC.Writer);

        // Assert
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [91011])], receiverA.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [1234])], receiverB.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678]), CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [91011])], receiverC.Writer.Written);
        Assert.Equal([CreateJsonSerializedInvocation(nameof(ITestReceiver.Parameter_One), [5678])], receiverD.Writer.Written);

        var commandsShard1 = profSessionShard1.FinishProfiling();
        var commandsShard2 = profSessionShard2.FinishProfiling();
        var commandsShard3 = profSessionShard3.FinishProfiling();
        Assert.Contains(commandsShard1, x => x.Command == "SPUBLISH");
        Assert.Contains(commandsShard1, x => x.Command == "SSUBSCRIBE");
        Assert.Contains(commandsShard2, x => x.Command == "SPUBLISH");
        Assert.Contains(commandsShard2, x => x.Command == "SSUBSCRIBE");
        Assert.Contains(commandsShard3, x => x.Command == "SPUBLISH");
        Assert.Contains(commandsShard3, x => x.Command == "SSUBSCRIBE");
    }

    public void Dispose()
    {
        _timeoutTokenSource.Dispose();
    }
}
