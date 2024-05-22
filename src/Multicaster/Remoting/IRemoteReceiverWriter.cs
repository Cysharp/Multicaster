namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteReceiverWriter
{
    void Write(ReadOnlyMemory<byte> payload);
}