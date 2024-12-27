using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;

BenchmarkRunner.Run<Benchmarks_AddGroup>();

[MemoryDiagnoser, RankColumn, ShortRunJob]
public class Benchmarks_MethodCall
{
    private readonly IMulticastGroupProvider _groupProvider;
    private readonly IMulticastSyncGroup<Guid, IReceiver> _group;
    private readonly IMulticastSyncGroup<Guid, IReceiver> _group100;
    private readonly IMulticastSyncGroup<Guid, IReceiver> _group500;

    public Benchmarks_MethodCall()
    {
        _groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        _group = _groupProvider.GetOrAddSynchronousGroup<Guid, IReceiver>("Test");

        _group100 = _groupProvider.GetOrAddSynchronousGroup<Guid, IReceiver>("Test100");
        foreach (var receiver in Enumerable.Range(0, 100).Select(x => new A()))
        {
            _group100.Add(Guid.NewGuid(), receiver);
        }

        _group500 = _groupProvider.GetOrAddSynchronousGroup<Guid, IReceiver>("Test500");
        foreach (var receiver in Enumerable.Range(0, 500).Select(x => new A()))
        {
            _group500.Add(Guid.NewGuid(), receiver);
        }
    }

    [Benchmark(Baseline = true)]
    public void Group_0_All_Method_Call()
    {
        _group.All.Method();
    }

    [Benchmark]
    public void Group_100_All_Method_Call()
    {
        _group100.All.Method();
    }

    [Benchmark]
    public void Group_500_All_Method_Call()
    {
        _group500.All.Method();
    }
}


[MemoryDiagnoser, RankColumn, ShortRunJob]
public class Benchmarks_AddGroup
{
    private readonly IMulticastGroupProvider _groupProvider;
    private readonly IMulticastSyncGroup<Guid, IReceiver> _group;
    private readonly IMulticastSyncGroup<Guid, IReceiver> _group100;
    private readonly IMulticastSyncGroup<Guid, IReceiver> _group500;
    private readonly IReceiver[] _receivers100 = Enumerable.Range(0, 100).Select(x => new A()).ToArray();
    private readonly IReceiver[] _receivers500 = Enumerable.Range(0, 500).Select(x => new A()).ToArray();

    public Benchmarks_AddGroup()
    {
        _groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);
        _group = _groupProvider.GetOrAddSynchronousGroup<Guid, IReceiver>("Test");

        _group100 = _groupProvider.GetOrAddSynchronousGroup<Guid, IReceiver>("Test100");
        _group500 = _groupProvider.GetOrAddSynchronousGroup<Guid, IReceiver>("Test500");
    }

    [Benchmark]
    public void Group_100()
    {
        foreach (var receiver in _receivers100)
        {
            _group.Add(new Guid(), receiver);
            _group.All.Method();
        }
    }

    [Benchmark]
    public void Group_500()
    {
        foreach (var receiver in _receivers500)
        {
            _group.Add(new Guid(), receiver);
            _group.All.Method();
        }
    }
}


public interface IReceiver
{
    void Method();
}
public class A : IReceiver
{
    public void Method() { }
}