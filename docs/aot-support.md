# Multicaster AOT Support

Multicaster provides AOT (Ahead-of-Time) compilation support through a source generator that generates static proxy implementations at compile time.

## Overview

By default, Multicaster uses dynamic code generation (`System.Reflection.Emit`) to create proxy classes at runtime. While this approach is flexible, it's not compatible with AOT compilation scenarios like:

- .NET Native AOT publishing
- iOS/Android with trimming enabled
- Blazor WebAssembly with AOT
- MagicOnion StreamingHub with AOT

The `Multicaster.SourceGenerator` package provides a source generator that creates these proxy implementations at compile time, making them fully compatible with AOT scenarios.

## Installation

Add the `Multicaster.SourceGenerator` package to your project:

```xml
<ItemGroup>
  <PackageReference Include="Multicaster" Version="x.x.x" />
  <PackageReference Include="Multicaster.SourceGenerator" Version="x.x.x" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

Or if using project reference (e.g., during development):

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Multicaster.SourceGenerator.csproj" OutputItemType="Analyzer" />
</ItemGroup>
```

## Usage

### 1. Define Your Receiver Interfaces

```csharp
public interface IChatReceiver
{
    void OnMessage(string user, string message);
    void OnUserJoined(string user);
    void OnUserLeft(string user);
}

public interface IGameReceiver
{
    void OnGameStarted(int gameId);
    void OnPlayerMoved(int playerId, int x, int y);
    void OnGameEnded(int winnerId);
}
```

### 2. Create a Proxy Factory Class

Create a partial class with the `[MulticasterProxyGeneration]` attribute, specifying all receiver interface types:

```csharp
using Cysharp.Runtime.Multicast;

[MulticasterProxyGeneration(typeof(IChatReceiver), typeof(IGameReceiver))]
public partial class MyProxyFactory
{
}
```

The source generator will automatically implement `IInMemoryProxyFactory` and `IRemoteProxyFactory` interfaces for this class.

### 3. Use the Generated Factory

```csharp
// Create an instance of the generated factory
var proxyFactory = new MyProxyFactory();

// Use with InMemoryGroupProvider
var inMemoryProvider = new InMemoryGroupProvider(proxyFactory);

// Use with RemoteGroupProvider
var remoteProvider = new RemoteGroupProvider(proxyFactory, proxyFactory, serializer);

// Get a group and use it
var chatGroup = inMemoryProvider.GetOrAddGroup<string, IChatReceiver>("chat-room");
chatGroup.Add("user1", new MyChatReceiver());
chatGroup.All.OnMessage("system", "Welcome!");
```

## How It Works

The source generator analyzes the receiver interface types specified in the `[MulticasterProxyGeneration]` attribute and generates:

1. **InMemory Proxy Classes**: For each receiver interface, a proxy class is generated that inherits from `InMemoryProxyBase<TKey, T>` and implements the receiver interface. These proxies iterate over all registered receivers and invoke methods on them.

2. **Remote Proxy Classes**: For each receiver interface, a proxy class is generated that inherits from `RemoteProxyBase` and implements the receiver interface. These proxies serialize method calls and send them to remote receivers.

3. **Factory Implementation**: The partial class is completed with implementations of `IInMemoryProxyFactory.Create<TKey, T>()` and `IRemoteProxyFactory.Create<T>()` that return the appropriate proxy instances.

## Client Results Support

The source generator also supports client results (async methods that return values):

```csharp
public interface IClientResultReceiver
{
    Task<bool> ConfirmAsync(string message, CancellationToken cancellationToken = default);
    Task NotifyAsync(string message, CancellationToken cancellationToken = default);
}
```

These methods are properly handled by the generated proxies, using the appropriate `InvokeWithResult` methods.

## Limitations

- All receiver interface types must be specified in the `[MulticasterProxyGeneration]` attribute
- Methods can have up to 15 parameters (same as the dynamic implementation)
- The generated factory only supports the receiver types explicitly listed in the attribute

## Migration from Dynamic Proxies

To migrate from dynamic proxies to source-generated proxies:

1. Add the `Multicaster.SourceGenerator` package
2. Create a partial class with `[MulticasterProxyGeneration]` listing all your receiver interfaces
3. Replace `DynamicInMemoryProxyFactory` with your generated factory class
4. Replace `DynamicRemoteProxyFactory` with your generated factory class

Before:
```csharp
var inMemoryFactory = new DynamicInMemoryProxyFactory();
var remoteFactory = new DynamicRemoteProxyFactory();
```

After:
```csharp
var factory = new MyProxyFactory(); // Implements both interfaces
```

## Troubleshooting

### "The type 'X' is not supported" Exception

This error occurs when you try to create a proxy for a receiver type that wasn't included in the `[MulticasterProxyGeneration]` attribute. Add the missing type to the attribute:

```csharp
[MulticasterProxyGeneration(typeof(IChatReceiver), typeof(IMissingReceiver))]
public partial class MyProxyFactory { }
```

### Build Errors in Generated Code

If you see build errors in the generated code, ensure:
- All receiver types are interfaces
- Method signatures are valid (void or Task/Task<T> return types)
- Parameter count doesn't exceed 15

## Integration with MagicOnion

When using Multicaster with MagicOnion's StreamingHub for AOT support:

```csharp
// Define your StreamingHub receiver interface
public interface IChatHubReceiver
{
    void OnMessage(string user, string message);
    void OnUserJoined(string user);
}

// Create a proxy factory with the receiver interface
[MulticasterProxyGeneration(typeof(IChatHubReceiver))]
public partial class MulticasterProxyFactory { }

// Configure MagicOnion to use the static proxy factory
builder.Services.AddMagicOnion()
    .UseStaticMethodProvider<MagicOnionMethodProvider>()
    .UseStaticProxyFactory<MulticasterProxyFactory>();
```

See the [MagicOnion AOT Sample](https://github.com/Cysharp/MagicOnion/tree/main/samples/AotSample) for a complete example.
