using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cysharp.Runtime.Multicast.Internal;
using Cysharp.Runtime.Multicast.Remoting;
using MessagePack;
using NATS.Client.Core;

namespace Cysharp.Runtime.Multicast.Distributed.Nats;

internal class NatsGroup<TKey, T> : IMulticastAsyncGroup<TKey, T>, IMulticastSyncGroup<TKey, T>, IDistributedGroup
    where TKey : IEquatable<TKey>
{
    private readonly NatsConnection _connection;
    private readonly IRemoteProxyFactory _proxyFactory;
    private readonly IRemoteSerializer _serializer;
    private readonly string _subject;
    private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _receivers = new();
    private readonly SemaphoreSlim _lock = new(1);
    private CancellationTokenSource _subscriptionTokenSource = new();
    private Task? _runningSubscriptionTask;
    private bool _disposed;

    public T All { get; }

    internal string Name { get; }

    public NatsGroup(string name, NatsConnection connection, IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer)
    {
        Name = name;
        _connection = connection;
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _subject = $"Multicaster.Group/{name}";

        All = proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, ImmutableArray<TKey>.Empty, null), serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    class NatsPublishWriter : IRemoteReceiverWriter
    {
        private readonly NatsConnection _connection;
        private readonly string _subject;
        private readonly ImmutableArray<TKey> _excludes;
        private readonly ImmutableArray<TKey>? _targets;

        public NatsPublishWriter(NatsConnection connection, string subject, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
        {
            _connection = connection;
            _subject = subject;
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
                KeyArrayFormatter<TKey>.Serialize(ref writer, _targets.Value.AsSpan());
            }
            writer.Flush();
            bufferWriter.Write(payload.Span);

            var taskPublish = _connection.PublishAsync(_subject, bufferWriter.WrittenMemory);
            if (!taskPublish.IsCompletedSuccessfully)
            {
                taskPublish.GetAwaiter().GetResult(); // TODO:
            }
        }
    }

    public T Except(ImmutableArray<TKey> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, excludes, null), _serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    public T Only(ImmutableArray<TKey> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, ImmutableArray<TKey>.Empty, targets), _serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    public T Single(TKey target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, ImmutableArray<TKey>.Empty, [target]), _serializer, NotSupportedRemoteClientResultPendingTaskRegistry.Instance);
    }

    public void Dispose()
    {
        if (_disposed) return;
        UnsubscribeAsync().GetAwaiter().GetResult();
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
                await SubscribeAsync();
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
                await UnsubscribeAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
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
        ThrowIfDisposed();

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
        throw new NotSupportedException();
    }

    private void Subscribe()
    {
        if (_runningSubscriptionTask is not null)
        {
            Unsubscribe();
        }

        _runningSubscriptionTask = Task.Run(async () =>
        {
            await foreach (var message in _connection.SubscribeAsync<byte[]>(_subject, cancellationToken: _subscriptionTokenSource.Token))
            {
                if (message.Data is { } data)
                {
                    HandleMessage(data);
                }
            }
        });
    }

    private async ValueTask SubscribeAsync()
    {
        if (_runningSubscriptionTask is not null)
        {
            await UnsubscribeAsync().ConfigureAwait(false);
        }

        _runningSubscriptionTask = Task.Run(async () =>
        {
            await foreach (var message in _connection.SubscribeAsync<byte[]>(_subject, cancellationToken: _subscriptionTokenSource.Token))
            {
                if (message.Data is { } data)
                {
                    HandleMessage(data);
                }
            }
        });
    }

    private void Unsubscribe()
    {
        _subscriptionTokenSource.Cancel();
        _subscriptionTokenSource.Dispose();
        _subscriptionTokenSource = new CancellationTokenSource();

        if (_runningSubscriptionTask is not null)
        {
            try
            {
                _runningSubscriptionTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            { }
        }
    }

    private async ValueTask UnsubscribeAsync()
    {
        _subscriptionTokenSource.Cancel();
        _subscriptionTokenSource.Dispose();
        _subscriptionTokenSource = new CancellationTokenSource();

        if (_runningSubscriptionTask is not null)
        {
            try
            {
                await _runningSubscriptionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { }
        }
    }

    private void HandleMessage(byte[] messageBytes)
    {
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
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NatsGroup<TKey, T>));
        }
    }

}