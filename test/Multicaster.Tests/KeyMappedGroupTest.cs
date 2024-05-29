using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

using Cysharp.Runtime.Multicast;

namespace Multicaster.Tests;

public class KeyMappedGroupTest
{
    [Fact]
    public void Sync_Add()
    {
        // Arrange
        var group = new FakeMulticastSyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedSyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        wrapped.Add("Foo", receiver);

        // Assert
        Assert.Equal([(nameof(IMulticastSyncGroup<ITestReceiver>.Add), (keyMapping["Foo"], (ITestReceiver)receiver))], group.Called);
    }

    [Fact]
    public void Sync_Remove()
    {
        // Arrange
        var group = new FakeMulticastSyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedSyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        wrapped.Add("Foo", receiver);
        var key = keyMapping["Foo"];
        wrapped.Remove("Foo");

        // Assert
        Assert.Equal([
            (nameof(IMulticastSyncGroup<ITestReceiver>.Add), (key, (ITestReceiver)receiver)),
            (nameof(IMulticastSyncGroup<ITestReceiver>.Remove), (key)),
        ], group.Called);
        Assert.Empty(keyMapping);
    }

    [Fact]
    public void Sync_Only()
    {
        // Arrange
        var group = new FakeMulticastSyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedSyncGroup<string, ITestReceiver>(group);
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        wrapped.Add("A", receiverA);
        wrapped.Add("B", receiverB);
        wrapped.Add("C", receiverC);
        var t = wrapped.Only(["A", "C"]);

        // Assert
        Assert.Equal(nameof(IMulticastSyncGroup<ITestReceiver>.Only), group.Called[3].Method);
        Assert.Equal(ImmutableArray<Guid>.Empty.Add(keyMapping["A"]).Add(keyMapping["C"]), (ImmutableArray<Guid>)group.Called[3].Arguments!);
    }

    [Fact]
    public void Sync_Except()
    {
        // Arrange
        var group = new FakeMulticastSyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedSyncGroup<string, ITestReceiver>(group);
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        wrapped.Add("A", receiverA);
        wrapped.Add("B", receiverB);
        wrapped.Add("C", receiverC);
        var t = wrapped.Except(["A", "C"]);

        // Assert
        Assert.Equal(nameof(IMulticastSyncGroup<ITestReceiver>.Except), group.Called[3].Method);
        Assert.Equal(ImmutableArray<Guid>.Empty.Add(keyMapping["A"]).Add(keyMapping["C"]), (ImmutableArray<Guid>)group.Called[3].Arguments!);
    }

    [Fact]
    public void Sync_Single()
    {
        // Arrange
        var group = new FakeMulticastSyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedSyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        wrapped.Add("Foo", receiver);
        var t = wrapped.Single("Foo");

        // Assert
        Assert.Equal([
            (nameof(IMulticastSyncGroup<ITestReceiver>.Add), (keyMapping["Foo"], (ITestReceiver)receiver)),
            (nameof(IMulticastSyncGroup<ITestReceiver>.Single), (keyMapping["Foo"])),
        ], group.Called);
    }

    [Fact]
    public void Sync_NotContains()
    {
        // Arrange
        var group = new FakeMulticastSyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedSyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        wrapped.Add("Foo", receiver);
        var t = wrapped.Single("KeyNotExists");

        // Assert
        Assert.Equal([
            (nameof(IMulticastSyncGroup<ITestReceiver>.Add), (keyMapping["Foo"], (ITestReceiver)receiver)),
            (nameof(IMulticastSyncGroup<ITestReceiver>.Single), (Guid.Empty)),
        ], group.Called);
    }

    [Fact]
    public void Sync_Dispose()
    {
        // Arrange
        var group = new FakeMulticastSyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedSyncGroup<string, ITestReceiver>(group);

        // Act
        wrapped.Dispose();

        // Assert
        Assert.Equal([
            (nameof(IMulticastSyncGroup<ITestReceiver>.Dispose), FakeMulticastSyncGroup<ITestReceiver>.ArgumentEmpty),
        ], group.Called);
    }

    [Fact]
    public async Task Async_Add()
    {
        // Arrange
        var group = new FakeMulticastAsyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedAsyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        await wrapped.AddAsync("Foo", receiver);

        // Assert
        Assert.Equal([(nameof(IMulticastAsyncGroup<ITestReceiver>.AddAsync), (keyMapping["Foo"], (ITestReceiver)receiver, default(CancellationToken)))], group.Called);
    }

    [Fact]
    public async Task Async_Remove()
    {
        // Arrange
        var group = new FakeMulticastAsyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedAsyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        await wrapped.AddAsync("Foo", receiver);
        var key = keyMapping["Foo"];
        await wrapped.RemoveAsync("Foo");

        // Assert
        Assert.Equal([
            (nameof(IMulticastAsyncGroup<ITestReceiver>.AddAsync), (key, (ITestReceiver)receiver, default(CancellationToken))),
            (nameof(IMulticastAsyncGroup<ITestReceiver>.RemoveAsync), (key, default(CancellationToken))),
        ], group.Called);
        Assert.Empty(keyMapping);
    }

    [Fact]
    public async Task Async_Only()
    {
        // Arrange
        var group = new FakeMulticastAsyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedAsyncGroup<string, ITestReceiver>(group);
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        await wrapped.AddAsync("A", receiverA);
        await wrapped.AddAsync("B", receiverB);
        await wrapped.AddAsync("C", receiverC);
        var t = wrapped.Only(["A", "C"]);

        // Assert
        Assert.Equal(nameof(IMulticastAsyncGroup<ITestReceiver>.Only), group.Called[3].Method);
        Assert.Equal(ImmutableArray<Guid>.Empty.Add(keyMapping["A"]).Add(keyMapping["C"]), (ImmutableArray<Guid>)group.Called[3].Arguments!);
    }

    [Fact]
    public async Task Async_Except()
    {
        // Arrange
        var group = new FakeMulticastAsyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedAsyncGroup<string, ITestReceiver>(group);
        var receiverA = new TestInMemoryReceiver();
        var receiverB = new TestInMemoryReceiver();
        var receiverC = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        await wrapped.AddAsync("A", receiverA);
        await wrapped.AddAsync("B", receiverB);
        await wrapped.AddAsync("C", receiverC);
        var t = wrapped.Except(["A", "C"]);

        // Assert
        Assert.Equal(nameof(IMulticastAsyncGroup<ITestReceiver>.Except), group.Called[3].Method);
        Assert.Equal(ImmutableArray<Guid>.Empty.Add(keyMapping["A"]).Add(keyMapping["C"]), (ImmutableArray<Guid>)group.Called[3].Arguments!);
    }

    [Fact]
    public async Task Async_Single()
    {
        // Arrange
        var group = new FakeMulticastAsyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedAsyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        await wrapped.AddAsync("Foo", receiver);
        var t = wrapped.Single("Foo");

        // Assert
        Assert.Equal([
            (nameof(IMulticastAsyncGroup<ITestReceiver>.AddAsync), (keyMapping["Foo"], (ITestReceiver)receiver, default(CancellationToken))),
            (nameof(IMulticastAsyncGroup<ITestReceiver>.Single), (keyMapping["Foo"])),
        ], group.Called);
    }

    [Fact]
    public async Task Async_NotContains()
    {
        // Arrange
        var group = new FakeMulticastAsyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedAsyncGroup<string, ITestReceiver>(group);
        var receiver = new TestInMemoryReceiver();
        var keyMapping = (ConcurrentDictionary<string, Guid>)wrapped.GetType().GetField("_keyMapping", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wrapped)!;

        // Act
        await wrapped.AddAsync("Foo", receiver);
        var t = wrapped.Single("KeyNotExists");

        // Assert
        Assert.Equal([
            (nameof(IMulticastAsyncGroup<ITestReceiver>.AddAsync), (keyMapping["Foo"], (ITestReceiver)receiver, default(CancellationToken))),
            (nameof(IMulticastAsyncGroup<ITestReceiver>.Single), (Guid.Empty)),
        ], group.Called);
    }

    [Fact]
    public void Async_Dispose()
    {
        // Arrange
        var group = new FakeMulticastAsyncGroup<ITestReceiver>();
        var wrapped = new KeyMappedAsyncGroup<string, ITestReceiver>(group);

        // Act
        wrapped.Dispose();

        // Assert
        Assert.Equal([
            (nameof(IMulticastAsyncGroup<ITestReceiver>.Dispose), FakeMulticastAsyncGroup<ITestReceiver>.ArgumentEmpty),
        ], group.Called);
    }

    class FakeMulticastSyncGroup<T> : IMulticastSyncGroup<T>
    {
        public static readonly object ArgumentEmpty = new object();

        public List<(string Method, object? Arguments)> Called { get; }= new();

        public T All => throw new NotSupportedException();

        public T Except(ImmutableArray<Guid> excludes)
        {
            Called.Add((nameof(Except), excludes));
            return default!;
        }

        public T Only(ImmutableArray<Guid> targets)
        {
            Called.Add((nameof(Only), targets));
            return default!;
        }

        public T Single(Guid target)
        {
            Called.Add((nameof(Single), target));
            return default!;
        }

        public void Dispose()
        {
            Called.Add((nameof(Dispose), ArgumentEmpty));
        }

        public void Add(Guid key, T receiver)
        {
            Called.Add((nameof(Add), (key, receiver)));
        }

        public void Remove(Guid key)
        {
            Called.Add((nameof(Remove), (key)));
        }

        public int Count()
        {
            Called.Add((nameof(Count), ArgumentEmpty));
            return 0;
        }
    }


    class FakeMulticastAsyncGroup<T> : IMulticastAsyncGroup<T>
    {
        public static readonly object ArgumentEmpty = new object();

        public List<(string Method, object? Arguments)> Called { get; } = new();

        public T All => throw new NotSupportedException();

        public T Except(ImmutableArray<Guid> excludes)
        {
            Called.Add((nameof(Except), excludes));
            return default!;
        }

        public T Only(ImmutableArray<Guid> targets)
        {
            Called.Add((nameof(Only), targets));
            return default!;
        }

        public T Single(Guid target)
        {
            Called.Add((nameof(Single), target));
            return default!;
        }

        public void Dispose()
        {
            Called.Add((nameof(Dispose), ArgumentEmpty));
        }

        public ValueTask AddAsync(Guid key, T receiver, CancellationToken cancellationToken = default)
        {
            Called.Add((nameof(AddAsync), (key, receiver, cancellationToken)));
            return default;
        }

        public ValueTask RemoveAsync(Guid key, CancellationToken cancellationToken = default)
        {
            Called.Add((nameof(RemoveAsync), (key, cancellationToken)));
            return default;
        }

        public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        {
            Called.Add((nameof(CountAsync), (cancellationToken)));
            return ValueTask.FromResult(0);
        }
    }
}