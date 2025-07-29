using System.Runtime.InteropServices;

using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;
using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

public class StressTest
{
    [Fact]
    public void ManyOnly()
    {
        var proxyFactory = DynamicRemoteProxyFactory.Instance;
        var proxyFactoryInMemory = DynamicInMemoryProxyFactory.Instance;
        var serializer = new TestJsonRemoteSerializer();

        IMulticastGroupProvider groupProvider = new RemoteGroupProvider(proxyFactoryInMemory, proxyFactory, serializer);
        var group = groupProvider.GetOrAddSynchronousGroup<Guid, ITestReceiver>("MyGroup");
        var receivers = new List<(Guid Id, ITestReceiver Proxy)>();
        for (var i =0; i < 200; i++)
        {
            var receiverWriter = new NullRemoteReceiverWriter();
            var receiver = proxyFactory.CreateDirect<ITestReceiver>(receiverWriter, serializer);
            var receiverId = Guid.NewGuid();

            receivers.Add((receiverId, receiver));
            group.Add(receiverId, receiver);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var ids = receivers.Select(x => x.Id).ToArray();
        for (var i = 0; i < 5000000; i++)
        {
            var targets = Random.Shared.GetItems(ids, 10);
            group.Only(targets).Parameter_Many(default, string.Empty, default, default);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

    }

    class NullRemoteReceiverWriter(RemoteClientResultPendingTaskRegistry? pendingTasks = null) : IRemoteReceiverWriter
    {
        public RemoteClientResultPendingTaskRegistry PendingTasks { get; } = pendingTasks ?? new();

        IRemoteClientResultPendingTaskRegistry IRemoteReceiverWriter.PendingTasks => PendingTasks;

        public void Write(ReadOnlyMemory<byte> payload)
        {
        }
    }
}
