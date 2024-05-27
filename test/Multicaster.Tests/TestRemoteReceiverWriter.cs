using System.Text;

using Cysharp.Runtime.Multicast.Remoting;

namespace Multicaster.Tests;

class TestRemoteReceiverWriter :IRemoteReceiverWriter
{
    public List<string> Written { get; } = new();

    public void Write(ReadOnlyMemory<byte> payload)
    {
        Written.Add(Encoding.UTF8.GetString(payload.Span));
    }
}