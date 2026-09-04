using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;

namespace Multicaster.SourceGenerator.PackageFixture;

[MulticasterGeneration(typeof(IPackageReceiver))]
internal partial class PackageGeneratedMulticaster
{
}

internal interface IBasePackageReceiver
{
    void Receive(string value);
}

internal interface IPackageReceiver : IBasePackageReceiver
{
    void SendDynamic(dynamic value);
}

internal sealed class PackageReceiver : IPackageReceiver
{
    public string? Value { get; private set; }

    public void Receive(string value) => Value = value;

    public void SendDynamic(dynamic value) => Value = (string)(object)value;
}

internal static class Program
{
    public static void Main()
    {
        var receiver = new PackageReceiver();
        var holder = new ImmutableReceiverHolder<Guid, IPackageReceiver>([receiver]);
        PackageGeneratedMulticaster.InMemoryProxyFactory.Create(holder).Receive("Package");
        if (receiver.Value != "Package")
        {
            throw new InvalidOperationException("The packaged source generator did not produce a working proxy.");
        }

        PackageGeneratedMulticaster.InMemoryProxyFactory.Create(holder).SendDynamic("Dynamic package");
        if (receiver.Value != "Dynamic package")
        {
            throw new InvalidOperationException("The packaged source generator did not dispatch the dynamic argument statically.");
        }
    }
}
