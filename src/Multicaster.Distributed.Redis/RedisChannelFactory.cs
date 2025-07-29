using StackExchange.Redis;

namespace Cysharp.Runtime.Multicast.Distributed.Redis;

/// <summary>
/// Provides factory methods for creating <see cref="RedisChannel"/> instances with predefined naming conventions.
/// </summary>
public static class RedisChannelFactory
{
    private const string Prefix = "Multicaster.Group?name=";

    /// <summary>
    /// Gets a factory that creates <see cref="RedisChannel"/> instances.
    /// </summary>
    public static Func<string, RedisChannel> Default { get; } = static (groupName) => RedisChannel.Literal($"{Prefix}{groupName}");

    /// <summary>
    /// Gets a factory that creates <see cref="RedisChannel"/> instances for use with Sharded Pub/Sub.
    /// </summary>
    public static Func<string, RedisChannel> Sharded { get; } = static (groupName) => RedisChannel.Sharded($"{Prefix}{groupName}");
}
