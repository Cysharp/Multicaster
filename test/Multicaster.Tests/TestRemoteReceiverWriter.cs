using System.Text;

using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

class TestRemoteReceiverWriter :IRemoteReceiverWriter
{
    public List<string> Written { get; } = new();

    public RemoteClientResultPendingTaskRegistry PendingTasks { get; }

    IRemoteClientResultPendingTaskRegistry IRemoteReceiverWriter.PendingTasks => PendingTasks;

    public TestRemoteReceiverWriter(RemoteClientResultPendingTaskRegistry? pendingTasks = null)
    {
        PendingTasks = pendingTasks ?? new();
    }

    public void Write(InvocationWriteContext context)
    {
        Written.Add(Encoding.UTF8.GetString(context.Payload.Span));
    }
}
