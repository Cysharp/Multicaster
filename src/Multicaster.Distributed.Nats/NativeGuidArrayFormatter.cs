using System.Runtime.CompilerServices;
using MessagePack.Formatters;
using MessagePack;

namespace Cysharp.Runtime.Multicast.Distributed.Nats;

internal static class NativeGuidArrayFormatter
{
    private static readonly IMessagePackFormatter<Guid> _formatter = NativeGuidFormatter.Instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(ref MessagePackWriter writer, ReadOnlySpan<Guid> values)
    {
        if (values == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(values.Length);
        foreach (var value in values)
        {
            _formatter.Serialize(ref writer, value, MessagePackSerializer.DefaultOptions);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid[]? Deserialize(ref MessagePackReader reader)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var len = reader.ReadArrayHeader();
        var result = new Guid[len];
        for (int i = 0; i < len; i++)
        {
            result[i] = _formatter.Deserialize(ref reader, MessagePackSerializer.DefaultOptions);
        }

        return result;
    }
}