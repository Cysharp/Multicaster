using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;

using Cysharp.Runtime.Multicast.Internal;
using Cysharp.Runtime.Multicast.Remoting;

using MessagePack;

using StackExchange.Redis;

using static System.Net.Mime.MediaTypeNames;

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

    private ChannelMessageQueue? _messageQueue;
    private bool _disposed;

    public T All { get; }

    internal string Name { get; }

    public RedisGroup(string name, ISubscriber subscriber, IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer)
    {
        Name = name;
        _subscriber = subscriber;
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _channel = new RedisChannel($"Multicaster.Group?name={name}", RedisChannel.PatternMode.Literal);

        All = proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, ImmutableArray<TKey>.Empty, null), serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    class RedisPublishWriter : IRemoteReceiverWriter
    {
        private readonly ISubscriber _subscriber;
        private readonly RedisChannel _channel;
        private readonly ImmutableArray<TKey> _excludes;
        private readonly ImmutableArray<TKey>? _targets;

        public RedisPublishWriter(ISubscriber subscriber, RedisChannel channel, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
        {
            _subscriber = subscriber;
            _channel = channel;
            _excludes = excludes;
            _targets = targets;
        }

        public void Write(ReadOnlyMemory<byte> payload)
        {
            using var bufferWriter = ArrayPoolBufferWriter.RentThreadStaticWriter();
            var writer = new MessagePackWriter(bufferWriter);

            // redis-format: [[excludes], [targets], [raw-body]]
            writer.WriteArrayHeader(3);
            KeyArrayFormatter<TKey>.Serialize(ref writer, _excludes.AsSpan());
            if (_targets is null)
            {
                writer.WriteNil();
            }
            else
            {
                KeyArrayFormatter<TKey>.Serialize(ref writer,  _targets.Value.AsSpan());
            }
            writer.Flush();
            bufferWriter.Write(payload.Span);

            _subscriber.Publish(_channel, bufferWriter.WrittenMemory);
        }
    }

    public T Except(ImmutableArray<TKey> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, excludes, null), _serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    public T Only(ImmutableArray<TKey> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, ImmutableArray<TKey>.Empty, targets), _serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    public T Single(TKey target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new RedisPublishWriter(_subscriber, _channel, ImmutableArray<TKey>.Empty, [target]), _serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_messageQueue is not null)
        {
            _messageQueue.Unsubscribe();
        }
        _disposed = true;
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
            if (_receivers.Remove(key, out _) && _receivers.Count == 0)
            {
                await UnsubscribeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private void HandleMessage(ChannelMessage message)
    {
        var messageBytes = (byte[])message.Message!;
        var reader = new MessagePackReader(messageBytes);

        var arrayLength = reader.ReadArrayHeader();
        if (arrayLength == 3)
        {
            var excludes = KeyArrayFormatter<TKey>.Deserialize(ref reader);
            var targets = KeyArrayFormatter<TKey>.Deserialize(ref reader);
            var payload = messageBytes.AsMemory((int)reader.Consumed);

            foreach (var receiver in _receivers)
            {
                if (excludes is not null && excludes.Contains(receiver.Key)) continue;
                if (targets is not null && !targets.Contains(receiver.Key)) continue;
                receiver.Value.Write(payload);
            }
        }
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

    private async ValueTask SubscribeAsync()
    {
        if (_messageQueue is not null)
        {
            await _messageQueue.UnsubscribeAsync().ConfigureAwait(false);
        }
        _messageQueue = await _subscriber.SubscribeAsync(_channel).ConfigureAwait(false);
        _messageQueue.OnMessage(HandleMessage);
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
            if (_receivers.Remove(key, out _) && _receivers.Count == 0)
            {
                Unsubscribe();
            }
        }
        finally
        {
            _lock.Release();
        }
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