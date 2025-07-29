using System.Text.Json;
using Cysharp.Runtime.Multicast.Internal;
using Multicaster.Tests;

namespace Multicaster.Distributed.Redis.Tests;

public abstract class RedisGroupTestBase
{
    protected static string CreateJsonSerializedInvocation(string nameOfMethod, IReadOnlyList<object?> args)
        => CreateJsonSerializedInvocation(nameOfMethod, null, args);
    protected static string CreateJsonSerializedInvocation(string nameOfMethod, Guid? messageId, IReadOnlyList<object?> args)
        => JsonSerializer.Serialize(new TestJsonRemoteSerializer.SerializedInvocation(nameOfMethod, FNV1A32.GetHashCode(nameOfMethod), messageId, args));
}
