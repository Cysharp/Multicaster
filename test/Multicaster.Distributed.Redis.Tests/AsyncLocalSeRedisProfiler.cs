using StackExchange.Redis.Profiling;

namespace Multicaster.Distributed.Redis.Tests;

internal class AsyncLocalSeRedisProfiler
{
    private readonly AsyncLocal<ProfilingSession> _asyncLocalSession = new();

    public ProfilingSession GetSession()
    {
        var session = _asyncLocalSession.Value;
        if (session == null)
        {
            _asyncLocalSession.Value = session = new ProfilingSession();
        }

        return session;
    }
}