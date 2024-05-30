using System.Runtime.CompilerServices;
using MessagePack.Formatters;
using MessagePack;

namespace Cysharp.Runtime.Multicast.Distributed.Nats;

internal static class KeyArrayFormatter<TKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(ref MessagePackWriter writer, ReadOnlySpan<TKey> values)
    {
        if (values == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(values.Length);
        foreach (var value in values)
        {
            FormatterCache<TKey>.Instance.Serialize(ref writer, value, MessagePackSerializer.DefaultOptions);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TKey[]? Deserialize(ref MessagePackReader reader)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var len = reader.ReadArrayHeader();
        var result = new TKey[len];
        for (int i = 0; i < len; i++)
        {
            result[i] = FormatterCache<TKey>.Instance.Deserialize(ref reader, MessagePackSerializer.DefaultOptions);
        }

        return result;
    }

    static class FormatterCache<T>
    {
        public static IMessagePackFormatter<T> Instance { get; }
        static FormatterCache()
        {
            if (typeof(T) == typeof(Guid))
            {
                Instance = (IMessagePackFormatter<T>)(object)NativeGuidFormatter.Instance;
                return;
            }
            else
            {
                // TODO: Options
                Instance = (IMessagePackFormatter<T>)(object)(MessagePackSerializerOptions.Standard.Resolver.GetFormatter<T>() ?? throw new NotSupportedException());
            }

            throw new NotSupportedException();
        }
    }
}