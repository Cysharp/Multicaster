using System.Text;

namespace Cysharp.Runtime.Multicast.CodeGen;

internal static class MethodIdCalculator
{
    public static int GetMethodId(string methodName)
    {
        var bytes = Encoding.UTF8.GetBytes(methodName);
        uint hash = 2166136261;
        for (var i = 0; i < bytes.Length; i++)
        {
            hash = unchecked((bytes[i] ^ hash) * 16777619);
        }

        return unchecked((int)hash);
    }
}
