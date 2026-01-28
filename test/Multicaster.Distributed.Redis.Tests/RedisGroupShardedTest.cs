using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.Distributed.Redis;
using Cysharp.Runtime.Multicast.Remoting;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;

using Multicaster.Tests;

using StackExchange.Redis;
using StackExchange.Redis.Profiling;

using Testcontainers.Redis;

namespace Multicaster.Distributed.Redis.Tests;

public class RedisGroupShardedTest : RedisGroupTestBase, IDisposable
{
    private readonly CancellationTokenSource _timeoutTokenSource = new(TimeSpan.FromSeconds(60));
    private CancellationToken TimeoutToken => _timeoutTokenSource.Token;

    private readonly INetwork _network;
    // Redis Cluster requires at least three master nodes.
    private readonly RedisContainer[] _redisContainers;

    public RedisGroupShardedTest()
    {
        _network = new NetworkBuilder()
            .WithName($"redis-cluster-network-{Guid.NewGuid()}")
            .Build();

        const int nodeCount = 3; // Number of Redis nodes in the cluster.
        const int portBase = 7000; // Base port for Redis nodes in the cluster.
        var hostIpAddress = GetHostIpAddress();
        var nodes = string.Join(" ", Enumerable.Range(0, nodeCount)
            .Select(x => $"redis-node-{x}:{portBase + x}"));

        _redisContainers = Enumerable.Range(0, nodeCount)
            .Select(x =>
            {
                var builder = new RedisBuilder()
                    .WithNetwork(_network)
                    .WithHostname($"redis-node-{x}")
                    .WithImage("bitnami/redis-cluster:latest")
                    .WithEnvironment("ALLOW_EMPTY_PASSWORD", "yes")
                    .WithEnvironment("REDIS_NODES", nodes)
                    .WithEnvironment("REDIS_PORT_NUMBER", $"{portBase + x}")
                    .WithEnvironment("REDIS_CLUSTER_DYNAMIC_IPS", "no")
                    .WithEnvironment("REDIS_CLUSTER_ANNOUNCE_IP", hostIpAddress)
                    .WithEnvironment("REDIS_CLUSTER_ANNOUNCE_PORT", $"{portBase + x}")
                    .WithEnvironment("REDIS_CLUSTER_ANNOUNCE_BUS_PORT", $"{portBase + x + 10000}")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("redis-cli", "-p", $"{portBase + x}", "ping"))
                    .WithPortBinding(portBase + x, portBase + x)
                    .WithPortBinding(portBase + x + 10000, portBase + x + 10000);

                if (x == 0)
                {
                    builder = builder
                        .WithEnvironment("REDIS_CLUSTER_REPLICAS", "0")
                        .WithEnvironment("REDIS_CLUSTER_CREATOR", "yes");
                }

                return builder.Build();
            })
            .ToArray();

        Task.WhenAll([
            Task.Delay(5000), // Give some time for the Redis cluster to start
            .._redisContainers.Select(x => x.StartAsync(TimeoutToken)),
        ]).GetAwaiter().GetResult();
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

    private string GetConnectionString()
        => $"localhost:7000,$CLUSTER="; // Disable auto-discovery, we use sharded pub/sub

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

        const string KeyForShard1 = "quux"; // 3850 -> Shard:1
        const string KeyForShard2 = "bar";  // 5061 -> Shard:2
        const string KeyForShard3 = "foo";  // 12182 -> Shard:3

        var received = new ConcurrentQueue<string>();

        using var conn1 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString());

        await conn1.GetDatabase().SetAddAsync(KeyForShard1, "1Value");
        await conn1.GetDatabase().SetAddAsync(KeyForShard2, "2Value");
        await conn1.GetDatabase().SetAddAsync(KeyForShard3, "3Value");

        var subscriber1 = conn1.GetSubscriber();
        var channel1 = RedisChannel.Sharded(KeyForShard1);
        var queue1 = await subscriber1.SubscribeAsync(channel1);
        queue1.OnMessage(message =>
        {
            received.Enqueue(message.Message.ToString());
        });

        using var conn2 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString());
        var subscriber2 = conn2.GetSubscriber();
        var channel2 = RedisChannel.Sharded(KeyForShard2);
        var queue2 = await subscriber2.SubscribeAsync(channel2);
        queue2.OnMessage(message =>
        {
            received.Enqueue(message.Message.ToString());
        });

        using var conn3 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString());
        var subscriber3 = conn3.GetSubscriber();
        var channel3 = RedisChannel.Sharded(KeyForShard3);
        var queue3 = await subscriber3.SubscribeAsync(channel3);
        queue3.OnMessage(message =>
        {
            received.Enqueue(message.Message.ToString());
        });

        await subscriber1.PublishAsync(channel3, "1->3");
        await Task.Delay(100);
        await subscriber2.PublishAsync(channel3, "2->3");
        await Task.Delay(100);
        await subscriber3.PublishAsync(channel2, "3->2");
        await Task.Delay(100);
        await subscriber3.PublishAsync(channel1, "3->1");
        await Task.Delay(100);

        Assert.Equal(received, ["1->3", "2->3", "3->2", "3->1"]);
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
        group2.All.Parameter_One(5678);

        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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
        await Task.Delay(100);
        group2.All.Parameter_One(5678);
        await Task.Delay(100);
        group3.All.Parameter_One(91011);

        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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
        using var groupShard1 = groupProviderShard1.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        using var connShard2 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSessionShard2 = new ProfilingSession();
        connShard2.RegisterProfiler(() => profSessionShard2);
        IMulticastGroupProvider groupProviderShard2 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ChannelFactory = RedisChannelFactory.Sharded, ConnectionMultiplexer = connShard2 });
        using var groupShard2 = groupProviderShard2.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup_");
        using var connShard3 = await ConnectionMultiplexer.ConnectAsync(GetConnectionString(), log: new DebugTextWriter());
        var profSessionShard3 = new ProfilingSession();
        connShard3.RegisterProfiler(() => profSessionShard3);
        IMulticastGroupProvider groupProviderShard3 = new RedisGroupProvider(proxyFactory, serializer, new RedisGroupOptions() { ChannelFactory = RedisChannelFactory.Sharded, ConnectionMultiplexer = connShard3});
        using var groupShard3 = groupProviderShard3.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup__");

        // Act
        // Shard1[A, B], Shard2[C, D], Shard3[A, C]
        groupShard1.Add(receiverA.Id, receiverA.Proxy);
        groupShard1.Add(receiverB.Id, receiverB.Proxy);
        groupShard2.Add(receiverC.Id, receiverC.Proxy);
        groupShard2.Add(receiverD.Id, receiverD.Proxy);
        groupShard3.Add(receiverA.Id, receiverA.Proxy);
        groupShard3.Add(receiverC.Id, receiverC.Proxy);

        groupShard1.All.Parameter_One(1234);
        await Task.Delay(100);
        groupShard2.All.Parameter_One(5678);
        await Task.Delay(100);
        groupShard3.All.Parameter_One(91011);

        // We need to wait to receive the message from Redis.
        await Task.Delay(100);

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

        foreach (var redisContainer in _redisContainers)
        {
            CastAndDispose(redisContainer);
        }

        CastAndDispose(_network);

        return;

        static void CastAndDispose(IAsyncDisposable resource)
        {
            if (resource is IDisposable resourceDisposable)
            {
                resourceDisposable.Dispose();
            }
            else
            {
                resource.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
