using MessagePack;
using StackExchange.Redis;

namespace Cysharp.Runtime.Multicast.Distributed.Redis;

/// <summary>
/// Represents configuration options for connecting to and interacting with a Redis group.
/// </summary>
/// <remarks>This class provides various settings to configure Redis connectivity, key serialization, and channel
/// creation. Use these options to customize the behavior of Redis-based operations, such as specifying a connection
/// string, providing a custom <see cref="ConnectionMultiplexer"/>, or defining a key prefix.</remarks>
public class RedisGroupOptions
{
    /// <summary>
    /// Gets or sets the connection string to connect to Redis. If <see cref="ConnectionMultiplexer"/> property is not set, this will be used.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets a ConnectionMultiplexer instance to connect to Redis. If this is set, <see cref="ConnectionString"/> property will be ignored.
    /// </summary>
    public ConnectionMultiplexer? ConnectionMultiplexer { get; set; }

    /// <summary>
    /// Gets or sets a prefix for the Redis key.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets a MessagePackSerializerOptions used for serializing the key.
    /// </summary>
    public MessagePackSerializerOptions? MessagePackSerializerOptionsForKey { get; set; }

    /// <summary>
    /// Gets or sets the factory method used to create <see cref="RedisChannel"/> instances. Default is <see cref="RedisChannelFactory.Default"/>.
    /// </summary>
    /// <remarks>Use this property to customize how <see cref="RedisChannel"/> instances are created,  such as
    /// applying specific naming conventions or transformations to channel names.</remarks>
    public Func<string, RedisChannel> ChannelFactory { get; set; } = RedisChannelFactory.Default;
}
