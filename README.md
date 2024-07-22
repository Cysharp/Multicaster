# Multicaster

This framework provides a proxy for transparently invoking methods of multiple POCO instances or remote clients through an interface.

For instance, a single call to `IGreeter.Hello` can invoke methods on several objects or broadcast the call to remote clients like those using MagicOnion or SignalR.

The framework incorporates the concept of groups that bundle together the receivers (targets) to enable transparent invocation, allowing receivers to be added or removed flexibly from the group for ease of management.

## Usage

### Basic Usage
A Group is requested from `IMulticastGroupProvider` by specifying a name, key type, and calling interface. If the Group does not exist, it will be created when it is requested.
There are synchronous and asynchronous operation versions of the Group, which can be selected according to the use case and supported functions.

```csharp
public interface IGreeterReceiver
{
    void OnMessage(string sender, string message);
}

public class GreeterReceiver(string name) : IGreeterReceiver
{
    public void OnMessage(string sender, string message)
        => Console.WriteLine($"[{name}] <{sender}> {message}")
}

var group = groupProvider.GetOrAddSynchronousGroup<Guid, IGreeterReceiver>("MyGroup");
```

You can register a receiver instance that implements the interface for the call with the key to the retrieved Group.

```csharp
var receiverId = Guid.NewGuid();
group.Add(receiverId, receiver);
```

>[!NOTE]
In the case of MagicOnion, you can register the `Client` property that can be used in StreamingHub.

You get a proxy object that calls the receiver via the Group's properties and methods such as `All`, `Except`, `Only`, and `Single`, and you can call the methods.

```csharp
group.All.OnMessage("A", "Hello");
```

Groups can be deleted from memory by calling `Dispose`. 

```csharp
group.Dispose();
```

>[!NOTE]
> Note that this means that in-memory groups are completely deleted, but if Redis/NATS is used as the backplane, **they are only deleted from memory on the .NET server that called it, and not from other servers**.

### In-Memory (Plain Old CLR Object)
Multicaster allows you to call the methods of multiple instances at once.

For example, it is possible to call multiple instances that implement `IGreeterReceiver.OnMessage` at once. The code below is an example of this.

```csharp
public interface IGreeterReceiver
{
    void OnMessage(string sender, string message);
}

public class GreeterReceiver(string name) : IGreeterReceiver
{
    public void OnMessage(string sender, string message)
        => Console.WriteLine($"[{name}] <{sender}> {message}")
}
```

```csharp
// Create receivers.
var receiverA = new GreeterReceiver("A");
var receiverIdA = Guid.NewGuid();
var receiverB = new GreeterReceiver("B");
var receiverIdB = Guid.NewGuid();
var receiverC = new GreeterReceiver("C");
var receiverIdC = Guid.NewGuid();

// Create a in-memory group provider.
var groupProvider = new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance);

// Create a group and add receivers to the group.
using var group = groupProvider.GetOrAddSynchronousGroup<Guid, IGreeterReceiver>("MyGroup");
group.Add(receiverIdA, receiverA);
group.Add(receiverIdB, receiverB);
group.Add(receiverIdC, receiverC);

// Call receivers via proxy.
group.All.OnMessage("System", "Hello");
group.Except([receiverIdA]).OnMessage("System", "Hello without A");

// Call the receiver directly. (Normal method call)
receiverA.OnMessage("DirectMessage", "Sent a message to the receiver directly.");
```

#### Outputs
```
[A] <System> Hello
[B] <System> Hello
[C] <System> Hello
[B] <System> Hello without A
[C] <System> Hello without A
[A] <DirectMessage> Sent a message to the receiver directly.
```

## API
### `IMulticastGroupProvider` interface
- `IMulticastAsyncGroup<TKey, TReceiver> GetOrAddGroup<TKey, TReceiver>(string name);`
- `IMulticastSyncGroup<TKey, TReceiver> GetOrAddSynchronousGroup<TKey, TReceiver>(string name);`

### `IMulticastGroup<TKey, T>` interface
- `TReceiver All { get; }`
- `TReceiver Except(ImmutableArray<TKey> excludes);`
- `TReceiver Only(ImmutableArray<TKey> targets);`
- `TReceiver Single(TKey target);`
- `void Dispose()`
    - Dispose and unregister the group from a group provider.

### `IMulticastAsyncGroup<TKey, T>` interface (implements `IMulticastGroup<TKey, T>`)
- `ValueTask AddAsync(TKey key, TReceiver receiver, CancellationToken cancellationToken = default);`
- `ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);`
- `ValueTask<int> CountAsync(CancellationToken cancellationToken = default);`

### `IMulticastSyncGroup<TKey, T>` interface (implements `IMulticastGroup<TKey, T>`)
- `void Add(TKey key, TReceiver receiver);`
- `void Remove(TKey key);`
- `int Count();`

## Transports and supported features

| Transport | Multicast | Synchronous | Count/CountAsync | Client Results |
| -- | -- | -- | -- | -- |
| InMemory | ✔️ | ✔️ | ✔️ | ✔️ |
| Remote (InMemory) | ✔️ | ✔️ | ✔️ | ✔️ |
| Redis | ✔️ | ✔️ | - | - |
| NATS | ✔️ | ✔️ | - | - |
