using System.Buffers;
using System.Collections.Immutable;

using Cysharp.Runtime.Multicast.Internal;

namespace Multicaster.Distributed.Redis.Tests;

public class DistributedGroupMessageTest
{
    private static readonly Guid Key = new("00112233-4455-6677-8899-aabbccddeeff");

    [Theory]
    [InlineData(false, "9391C41033221100554477668899AABBCCDDEEFFC0")]
    [InlineData(true, "939091C41033221100554477668899AABBCCDDEEFF")]
    public void BroadcastMatchesMessagePackV2WireFormat(bool hasTargets, string headerHex)
    {
        var options = DistributedGroupMessage.DefaultKeyOptions;
        ImmutableArray<Guid> excludes = hasTargets ? [] : [Key];
        ImmutableArray<Guid>? targets = hasTargets ? [Key] : null;
        var payload = "[123]"u8.ToArray();
        var expected = Convert.FromHexString(headerHex).Concat(payload).ToArray();
        var output = new ArrayBufferWriter<byte>();

        DistributedGroupMessage.WriteBroadcast(output, excludes, targets, payload, options);

        Assert.Equal(expected, output.WrittenSpan.ToArray());
        using var reader = new DistributedGroupMessage.Reader(expected, options);
        Assert.Equal(3, reader.ReadArrayHeader());
        Assert.Equal(excludes.ToArray(), reader.Deserialize<Guid[]>());
        Assert.Equal(targets?.ToArray(), reader.Deserialize<Guid[]>());
        Assert.Equal(payload, reader.Remaining.ToArray());
    }

    [Fact]
    public void RemoveMatchesMessagePackV2WireFormat()
    {
        var options = DistributedGroupMessage.DefaultKeyOptions;
        var expected = Convert.FromHexString("9200C41033221100554477668899AABBCCDDEEFF");

        Assert.Equal(expected, DistributedGroupMessage.WriteRemove(Key, options));
        using var reader = new DistributedGroupMessage.Reader(expected, options);
        Assert.Equal(2, reader.ReadArrayHeader());
        Assert.True(reader.TryReadRemoveKey<Guid>(out var key));
        Assert.Equal(Key, key);
        Assert.True(reader.Remaining.IsEmpty);
    }

    [Fact]
    public void UnknownMessageTypeDoesNotReadRemoveKey()
    {
        var message = Convert.FromHexString("9201C0");

        using var reader = new DistributedGroupMessage.Reader(message, DistributedGroupMessage.DefaultKeyOptions);
        Assert.Equal(2, reader.ReadArrayHeader());
        Assert.False(reader.TryReadRemoveKey<Guid>(out _));
        Assert.Equal(message.AsSpan(2).ToArray(), reader.Remaining.ToArray());
    }
}
