using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

public interface IRemoteProxy
{
    bool TryGetDirectWriter([NotNullWhen(true)] out IRemoteReceiverWriter? receiver);
}