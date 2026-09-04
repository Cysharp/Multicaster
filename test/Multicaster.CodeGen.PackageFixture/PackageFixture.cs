using Cysharp.Runtime.Multicast.CodeGen;

namespace Multicaster.CodeGen.PackageFixture;

internal static class PackageFixture
{
    public static string EmitProxySource()
    {
        var receiver = new ReceiverDefinition(
            "global::PackageFixture.IReceiver",
            "__PackageFixtureReceiver",
            [
                new ReceiverMethodDefinition(
                    "Notify",
                    "void",
                    ReceiverReturnKind.Void,
                    null,
                    MethodIdCalculator.GetMethodId("Notify"),
                    [new ReceiverParameterDefinition("int", "value", false)],
                    declaringTypeName: "global::PackageFixture.IReceiver"),
            ]);

        return ProxyEmitter.Emit("PackageFixture", "GeneratedAnchor", [receiver]);
    }
}
