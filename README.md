# Multicaster

This framework provides a proxy for transparently invoking methods of multiple POCO instances or remote clients through an interface.

For instance, a single call to `IGreeter.Hello` can invoke methods on several objects or broadcast the call to remote clients like those using MagicOnion or SignalR.

The framework incorporates the concept of groups that bundle together the receivers (targets) to enable transparent invocation, allowing receivers to be added or removed flexibly from the group for ease of management.

## Usage
### In-Memory (Plain Old CLR Object)
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
var group = groupProvider.GetOrAddSynchronousGroup<IGreeterReceiver>("MyGroup");
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
- `IMulticastAsyncGroup<TReceiver> GetOrAddGroup<TReceiver>(string name);`
- `IMulticastSyncGroup<TReceiver> GetOrAddSynchronousGroup<TReceiver>(string name);`

### `IMulticastGroup<T>` interface
- `TReceiver All { get; }`
- `TReceiver Except(ImmutableArray<Guid> excludes);`
- `TReceiver Only(ImmutableArray<Guid> targets);`
- `TReceiver Single(Guid target);`

### `IMulticastAsyncGroup<T>` interface (implements `IMulticastGroup<T>`)
- `ValueTask AddAsync(Guid key, TReceiver receiver, CancellationToken cancellationToken = default);`
- `ValueTask RemoveAsync(Guid key, CancellationToken cancellationToken = default);`
- `ValueTask<int> CountAsync(CancellationToken cancellationToken = default);`

### `IMulticastSyncGroup<T>` interface (implements `IMulticastGroup<T>`)
- `void Add(Guid key, TReceiver receiver);`
- `void Remove(Guid key);`
- `int Count();`