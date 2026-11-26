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

    public void Write(ReadOnlyMemory<byte> payload)
    {
        lock (Written)
        {
            Written.Add(Encoding.UTF8.GetString(payload.Span));
        }
    }

    internal async Task WaitForMessagesAsync(int count, CancellationToken cancellationToken)
    {
        while (true)
        {
            lock (Written)
            {
                if (Written.Count >= count) return;
            }

            await Task.Delay(20, cancellationToken);
        }
    }
}
