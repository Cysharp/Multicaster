using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

internal class TestRemoteReceiverHelper
{
    public static (TestRemoteReceiverWriter Writer, ITestReceiver Proxy, Guid Id) CreateReceiverSet(IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        var receiverWriter = new TestRemoteReceiverWriter();
        var receiver = proxyFactory.CreateDirect<ITestReceiver>(receiverWriter, serializer, pendingTasks);
        var receiverId = Guid.NewGuid();
        return (receiverWriter, receiver, receiverId);
    }
}