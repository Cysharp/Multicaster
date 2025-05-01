using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cysharp.Runtime.Multicast.Internal;
using Cysharp.Runtime.Multicast.Remoting;
using MessagePack;

using StackExchange.Redis;

namespace Cysharp.Runtime.Multicast.Distributed.Redis;

internal class RedisGroup<TKey, T> : IMulticastAsyncGroup<TKey, T>, IMulticastSyncGroup<TKey, T>, IDistributedGroup
    where TKey : IEquatable<TKey>
{
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly ISubscriber _subscriber;
    private readonly RedisChannel _channel;
    private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _receivers = new();
    private readonly SemaphoreSlim _lock = new(1);
    private readonly MessagePackSerializerOptions _messagePackSerializerOptionsForKey;
    private readonly Action<RedisGroup<TKey, T>> _onDisposeAction;

    private ChannelMessageQueue? _messageQueue;
    private bool _disposed;

    public T All { get; }

    internal string Name { get; }

    public RedisGroup(string name, ISubscriber subscriber, IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, MessagePackSerializerOptions messagePackSerializerOptions, Action<RedisGroup<TKey, T>> onDisposeAction)
    {
        Name = name;
        _subscriber = subscriber;
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _channel = new RedisChannel($"Multicaster.Group?name={name}", RedisChannel.PatternMode.Literal);
        _messagePackSerializerOptionsForKey = messagePackSerializerOptions;
        _onDisposeAction = onDisposeAction;

        All = proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, ImmutableArray<TKey>.Empty, null, _messagePackSerializerOptionsForKey), serializer);
    }

    class RedisPublishWriter : IRemoteReceiverWriter
    {
        private readonly ISubscriber _subscriber;
        private readonly RedisChannel _channel;
        private readonly ImmutableArray<TKey> _excludes;
        private readonly ImmutableArray<TKey>? _targets;
        private readonly MessagePackSerializerOptions _optionsForKey;

        public IRemoteClientResultPendingTaskRegistry PendingTasks => NotSupportedRemoteClientResultPendingTaskRegistry.Instance;

        public RedisPublishWriter(ISubscriber subscriber, RedisChannel channel, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets, MessagePackSerializerOptions optionsForKey)
        {
            _subscriber = subscriber;
            _channel = channel;
            _excludes = excludes;
            _targets = targets;
            _optionsForKey = optionsForKey;
        }

        public void Write(ReadOnlyMemory<byte> payload)
        {
            using var bufferWriter = ArrayPoolBufferWriter.RentThreadStaticWriter();
            var writer = new MessagePackWriter(bufferWriter);

            // Redis-format: [[excludes], [targets], [raw-body]]
            writer.WriteArrayHeader(3);

            MessagePackSerializer.Serialize(ref writer, _excludes, _optionsForKey);
            if (_targets is null)
            {
                writer.WriteNil();
            }
            else
            {
                MessagePackSerializer.Serialize(ref writer,  _targets.Value, _optionsForKey);
            }
            writer.Flush();
            bufferWriter.Write(payload.Span);

            _subscriber.Publish(_channel, bufferWriter.WrittenMemory);
        }
    }

    public T Except(IEnumerable<TKey> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, [..excludes], null, _messagePackSerializerOptionsForKey), _serializer);
    }

    public T Only(IEnumerable<TKey> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, ImmutableArray<TKey>.Empty, [..targets], _messagePackSerializerOptionsForKey), _serializer);
    }

    public T Single(TKey target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, ImmutableArray<TKey>.Empty, [target], _messagePackSerializerOptionsForKey), _serializer);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_messageQueue is not null)
        {
            _messageQueue.Unsubscribe();
        }

        _onDisposeAction(this);
        _disposed = true;
    }

    private void HandleMessage(ChannelMessage message) => HandleMessageCoreAsync(message, true);

    private Task HandleMessageAsync(ChannelMessage message) => HandleMessageCoreAsync(message, false);

    private Task HandleMessageCoreAsync(ChannelMessage message, bool isSynchronous)
    {
        var messageBytes = (byte[])message.Message!;
        var reader = new MessagePackReader(messageBytes);

        var arrayLength = reader.ReadArrayHeader();

        // Broadcast: Array(3)[[Excludes], [Targets], [Message]]
        // Remove: Array(2)[0x0, Id]
        if (arrayLength == 3)
        {
            // Broadcast
            var excludes = MessagePackSerializer.Deserialize<TKey[]>(ref reader, _messagePackSerializerOptionsForKey);
            var targets = MessagePackSerializer.Deserialize<TKey[]>(ref reader, _messagePackSerializerOptionsForKey);
            var payload = messageBytes.AsMemory((int)reader.Consumed);

            foreach (var receiver in _receivers)
            {
                if (excludes is not null && excludes.Contains(receiver.Key)) continue;
                if (targets is not null && !targets.Contains(receiver.Key)) continue;
                receiver.Value.Write(payload);
            }
        }
        else if (arrayLength == 2)
        {
            // Remove
            var type = reader.ReadByte();
            var key = MessagePackSerializer.Deserialize<TKey>(ref reader, _messagePackSerializerOptionsForKey);
            if (isSynchronous)
            {
                TryRemoveCore(key);
                return Task.CompletedTask;
            }
            else
            {
                return AwaitTryRemoveCore(key);
            }
        }

        return Task.CompletedTask;

        async Task AwaitTryRemoveCore(TKey key)
        {
            await TryRemoveCoreAsync(key).ConfigureAwait(false);
        }
    }

    private byte[] BuildRemoveMessage(TKey key)
    {
        using var bufferWriter = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var writer = new MessagePackWriter(bufferWriter);

        // Format: [0x0, Key]
        writer.WriteArrayHeader(2);
        writer.Write(0x0);
        MessagePackSerializer.Serialize(ref writer, key, _messagePackSerializerOptionsForKey);
        writer.Flush();
        return bufferWriter.WrittenMemory.ToArray();
    }

    public async ValueTask AddAsync(TKey key, T receiver, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (receiver is not IRemoteProxy singleRemoteReceiverProvider)
        {
            throw new ArgumentException("A receiver must implement IRemoteSingleReceiverWriterAccessor interface.", nameof(receiver));
        }
        if (!singleRemoteReceiverProvider.TryGetDirectWriter(out var directReceiverWriter))
        {
            throw new ArgumentException("There must be only one receiver writer. The receiver has zero or no-single receiver writers.", nameof(receiver));
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_receivers.TryAdd(key, directReceiverWriter) && _receivers.Count == 1)
            {
                await SubscribeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!await TryRemoveCoreAsync(key).ConfigureAwait(false))
            {
                // If the target key does not exist in the local group, a remove message is notified to another server.
                var message = BuildRemoveMessage(key);
                await _subscriber.PublishAsync(_channel, message).ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask<bool> TryRemoveCoreAsync(TKey key)
    {
        if (_receivers.Remove(key, out _))
        {
            if (_receivers.Count == 0)
            {
                await UnsubscribeAsync().ConfigureAwait(false);
            }
            return true;
        }

        return false;
    }

    private async ValueTask SubscribeAsync()
    {
        if (_messageQueue is not null)
        {
            await _messageQueue.UnsubscribeAsync().ConfigureAwait(false);
        }
        _messageQueue = await _subscriber.SubscribeAsync(_channel).ConfigureAwait(false);
        _messageQueue.OnMessage(HandleMessageAsync);
    }

    private async ValueTask UnsubscribeAsync()
    {
        if (_messageQueue is not null)
        {
            await _messageQueue.UnsubscribeAsync().ConfigureAwait(false);
        }
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        throw new NotSupportedException("CountAsync is not supported by this group and the group provider.");
    }

    private void Subscribe()
    {
        if (_messageQueue is not null)
        {
            _messageQueue.Unsubscribe();
        }
        _messageQueue = _subscriber.Subscribe(_channel);
        _messageQueue.OnMessage(HandleMessage);
    }

    private void Unsubscribe()
    {
        if (_messageQueue is not null)
        {
            _messageQueue.Unsubscribe();
        }
    }

    public void Add(TKey key, T receiver)
    {
        ThrowIfDisposed();

        if (receiver is not IRemoteProxy singleRemoteReceiverProvider)
        {
            throw new ArgumentException("A receiver must implement IRemoteSingleReceiverWriterAccessor interface.", nameof(receiver));
        }
        if (!singleRemoteReceiverProvider.TryGetDirectWriter(out var directReceiverWriter))
        {
            throw new ArgumentException("There must be only one receiver writer. The receiver has zero or no-single receiver writers.", nameof(receiver));
        }

        _lock.Wait();
        try
        {
            if (_receivers.TryAdd(key, directReceiverWriter) && _receivers.Count == 1)
            {
                Subscribe();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Remove(TKey key)
    {
        _lock.Wait();
        try
        {
            if (!TryRemoveCore(key))
            {
                // If the target key does not exist in the local group, a remove message is notified to another server.
                var message = BuildRemoveMessage(key);
                _subscriber.Publish(_channel, message);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool TryRemoveCore(TKey key)
    {
        if (_receivers.Remove(key, out _))
        {
            if (_receivers.Count == 0)
            {
                Unsubscribe();
            }

            return true;
        }

        return false;
    }

    public int Count()
    {
        ThrowIfDisposed();
        throw new NotSupportedException("CountAsync is not supported by this group and the group provider.");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RedisGroup<TKey, T>));
        }
    }
}