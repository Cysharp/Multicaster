namespace Cysharp.Runtime.Multicast;

public interface IMulticastGroupProvider
{
    IMulticastAsyncGroup<TReceiver> GetOrAddGroup<TReceiver>(string name);
    IMulticastSyncGroup<TReceiver> GetOrAddSynchronousGroup<TReceiver>(string name);
}
