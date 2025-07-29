using System.Diagnostics.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.Remoting;

/// <summary>
/// Represents a proxy for interacting with a remote receiver, providing methods to access its writer.
/// </summary>
public interface IRemoteProxy
{
    /// <summary>
    /// Attempts to retrieve a direct writer for remote receiver communication.
    /// </summary>
    /// <remarks>
    /// This method returns a direct writer if the remote proxy holds or implements a direct writer for the remote receiver.
    /// If the proxy is grouped or does not have a direct writer, it will return false.
    /// </remarks>
    bool TryGetDirectWriter([NotNullWhen(true)] out IRemoteReceiverWriter? receiver);
}
