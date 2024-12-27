using System.Text;

namespace Cysharp.Runtime.Multicast.Internal;

internal static class FNV1A32
{
    public static int GetHashCode(string str)
    {
        Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetMaxByteCount(str.Length)];
        var written = Encoding.UTF8.GetBytes(str, buffer);

        return GetHashCode(buffer.Slice(0, written));
    }

    public static int GetHashCode(ReadOnlySpan<byte> obj)
    {
        uint hash = 2166136261;
        for (int i = 0; i < obj.Length; i++)
        {
            hash = unchecked((obj[i] ^ hash) * 16777619);
        }

        return unchecked((int)hash);
    }
}