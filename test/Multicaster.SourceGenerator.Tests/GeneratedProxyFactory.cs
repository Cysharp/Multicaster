using Cysharp.Runtime.Multicast;

namespace Multicaster.SourceGenerator.Tests;

/// <summary>
/// Generated proxy factory for test receivers.
/// The source generator will generate the implementation of IInMemoryProxyFactory and IRemoteProxyFactory.
/// </summary>
[MulticasterProxyGeneration(typeof(IChatReceiver), typeof(IGameReceiver), typeof(IClientResultReceiver))]
public partial class GeneratedProxyFactory
{
}
