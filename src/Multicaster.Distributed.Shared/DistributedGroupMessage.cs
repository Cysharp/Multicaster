using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using MessagePack;

namespace Cysharp.Runtime.Multicast.Internal;

internal static class DistributedGroupMessage
{
    private const byte RemoveMessageType = 0x00;

    public static MessagePackSerializerOptions DefaultKeyOptions { get; } =
        new(MessagePackFormatterFactory.Combine(new BinaryGuidFormatterFactory(), MessagePackFormatterFactory.Default));

    public static MessagePackSerializerOptions GetKeyOptions(MessagePackSerializerOptions? options)
    {
        if (options is null)
        {
            return DefaultKeyOptions;
        }

        return ReferenceEquals(options.Resolver, MessagePackSerializerOptions.Default.Resolver)
            ? options with { Resolver = DefaultKeyOptions.Resolver }
            : options;
    }

    public static void WriteBroadcast<TKey>(IBufferWriter<byte> output, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets, ReadOnlyMemory<byte> payload, MessagePackSerializerOptions options)
    {
        using var writer = new MessagePackWriter();
        writer.WriteArrayHeader(3);
        writer.WriteTo(output);

        MessagePackSerializer.Serialize(output, excludes, options);
        if (targets is { } targetKeys)
        {
            MessagePackSerializer.Serialize(output, targetKeys, options);
        }
        else
        {
            output.Write([MessagePackCode.Nil]);
        }

        output.Write(payload.Span);
    }

    public static byte[] WriteRemove<TKey>(TKey key, MessagePackSerializerOptions options)
    {
        using var writer = new MessagePackWriter();
        writer.WriteArrayHeader(2);
        writer.WriteByte(RemoveMessageType);
        if (options.MessageProcessor is null)
        {
            writer.Serialize(key, options);
        }
        else
        {
            writer.WriteRaw(MessagePackSerializer.Serialize(key, options));
        }

        return writer.ToArray();
    }

    public ref struct Reader(ReadOnlyMemory<byte> message, MessagePackSerializerOptions options)
    {
        private MessagePackReader _reader = new(message);

        public ReadOnlyMemory<byte> Remaining => message[(int)_reader.BytesConsumed..];

        public int ReadArrayHeader() => _reader.ReadArrayHeader();

        public bool TryReadRemoveKey<TKey>([MaybeNullWhen(false)] out TKey key) where TKey : notnull
        {
            if (_reader.ReadByte() != RemoveMessageType)
            {
                key = default;
                return false;
            }

            key = Deserialize<TKey>() ?? throw new MessagePackSerializationException("A remove message must contain a group key.");
            return true;
        }

        public T? Deserialize<T>()
        {
            if (options.MessageProcessor is null)
            {
                return _reader.Deserialize<T>(options);
            }

            var start = (int)_reader.BytesConsumed;
            _reader.Skip();
            return MessagePackSerializer.Deserialize<T>(message.Span[start..(int)_reader.BytesConsumed], options);
        }

        public void Dispose() => _reader.Dispose();
    }

    // Preserve the native GUID wire format without changing the default format of other key types.
    private sealed class BinaryGuidFormatterFactory : MessagePackFormatterFactory
    {
#if NET9_0_OR_GREATER
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
            => type == typeof(Guid) ? DotNetOptimizedFormatterFactory.Instance.CreateFormatter<TWriteBuffer, TReadBuffer>(type) : null;

#endif
        public override object? CreateFormatter(Type writeBuffer, Type readBuffer, Type type)
            => type == typeof(Guid) ? DotNetOptimizedFormatterFactory.Instance.CreateFormatter(writeBuffer, readBuffer, type) : null;
    }
}
