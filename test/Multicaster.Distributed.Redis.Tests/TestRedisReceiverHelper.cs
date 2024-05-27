using Cysharp.Runtime.Multicast.Distributed.Redis;
using Cysharp.Runtime.Multicast.Remoting;

using Multicaster.Tests;

namespace Multicaster.Distributed.Redis.Tests;

internal class TestRedisReceiverHelper
{
    public static (TestRemoteReceiverWriter Writer, ITestReceiver Proxy, Guid Id) CreateReceiverSet(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer)
    {
        var receiverWriter = new TestRemoteReceiverWriter();
        var receiver = proxyFactory.CreateDirect<ITestReceiver>(receiverWriter, serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
        var receiverId = Guid.NewGuid();
        return (receiverWriter, receiver, receiverId);
    }
}