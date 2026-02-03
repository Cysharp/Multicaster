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
    private readonly MessagePackSerializerOptions _messagePackSerializerOptionsForKey;
    private readonly string _subject;
    private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _receivers = new();
    private readonly SemaphoreSlim _lock = new(1);
    private readonly Action<NatsGroup<TKey, T>> _onDisposeAction;

    private CancellationTokenSource _subscriptionTokenSource = new();
    private Task? _runningSubscriptionTask;
    private bool _disposed;

    public T All { get; }

    internal string Name { get; }

    public NatsGroup(string name, NatsConnection connection, IRemoteProxyFactory proxyFactory, IRemoteSerializer serializer, MessagePackSerializerOptions messagePackSerializerOptionsForKey, Action<NatsGroup<TKey, T>> onDisposeAction)
    {
        Name = name;
        _connection = connection;
        _proxyFactory = proxyFactory;
        _serializer = serializer;
        _subject = $"Multicaster.Group/{name}";
        _onDisposeAction = onDisposeAction;

        All = proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, ImmutableArray<TKey>.Empty, null, messagePackSerializerOptionsForKey), serializer);
        _messagePackSerializerOptionsForKey = messagePackSerializerOptionsForKey;
    }

    class NatsPublishWriter : IRemoteReceiverWriter
    {
        private readonly NatsConnection _connection;
        private readonly string _subject;
        private readonly ImmutableArray<TKey> _excludes;
        private readonly ImmutableArray<TKey>? _targets;
        private readonly MessagePackSerializerOptions _messagePackSerializerOptionsForKey;

        public IRemoteClientResultPendingTaskRegistry PendingTasks => NotSupportedRemoteClientResultPendingTaskRegistry.Instance;

        public NatsPublishWriter(NatsConnection connection, string subject, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets, MessagePackSerializerOptions messagePackSerializerOptionsForKey)
        {
            _connection = connection;
            _subject = subject;
            _excludes = excludes;
            _targets = targets;
            _messagePackSerializerOptionsForKey = messagePackSerializerOptionsForKey;
        }

        public void Write(InvocationWriteContext context)
        {
            using var bufferWriter = ArrayPoolBufferWriter.RentThreadStaticWriter();
            var writer = new MessagePackWriter(bufferWriter);

            // redis-format: [[excludes], [targets], method-name, method-id, [raw-body]]
            writer.WriteArrayHeader(4);
            MessagePackSerializer.Serialize(ref writer, _excludes, _messagePackSerializerOptionsForKey);
            if (_targets is null)
            {
                writer.WriteNil();
            }
            else
            {
                MessagePackSerializer.Serialize(ref writer, _targets, _messagePackSerializerOptionsForKey);
            }
            writer.Write(context.MethodId);
            writer.Flush();
            bufferWriter.Write(context.Payload.Span);

            System.Diagnostics.Debug.WriteLine($"Publish Broadcast: {System.Text.Encoding.UTF8.GetString(bufferWriter.WrittenMemory.Span)}");
            var taskPublish = _connection.PublishAsync(_subject, bufferWriter.WrittenMemory);
            if (!taskPublish.IsCompletedSuccessfully)
            {
                taskPublish.GetAwaiter().GetResult(); // TODO:
            }
        }
    }

    public T Except(IEnumerable<TKey> excludes)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, [..excludes], null, _messagePackSerializerOptionsForKey), _serializer);
    }

    public T Only(IEnumerable<TKey> targets)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, ImmutableArray<TKey>.Empty, [..targets], _messagePackSerializerOptionsForKey), _serializer);
    }

    public T Single(TKey target)
    {
        ThrowIfDisposed();
        return _proxyFactory.Create<T>(new NatsPublishWriter(_connection, _subject, ImmutableArray<TKey>.Empty, [target], _messagePackSerializerOptionsForKey), _serializer);
    }

    public void Dispose()
    {
        if (_disposed) return;
        UnsubscribeAsync().GetAwaiter().GetResult();
        _onDisposeAction(this);
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
            if (!await TryRemoveCoreAsync(key).ConfigureAwait(false))
            {
                // If the target key does not exist in the local group, a remove message is notified to another server.
                var message = BuildRemoveMessage(key);
                await _connection.PublishAsync(_subject, message).ConfigureAwait(false);
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
            if (!TryRemoveCore(key))
            {
                // If the target key does not exist in the local group, a remove message is notified to another server.
                var message = BuildRemoveMessage(key);
                var taskPublish = _connection.PublishAsync(_subject, message);
                if (!taskPublish.IsCompletedSuccessfully)
                {
                    taskPublish.GetAwaiter().GetResult();
                }
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
                    await HandleMessageAsync(data).ConfigureAwait(false);
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
                    await HandleMessageAsync(data).ConfigureAwait(false);
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

    private ValueTask HandleMessageAsync(byte[] messageBytes)
    {
        var reader = new MessagePackReader(messageBytes);

        var arrayLength = reader.ReadArrayHeader();

        // Broadcast: Array(4)[[Excludes], [Targets], MethodId, [Message]]
        // Remove: Array(2)[0x0, Id]
        if (arrayLength == 5)
        {
            // Broadcast
            var excludes = MessagePackSerializer.Deserialize<TKey[]>(ref reader, _messagePackSerializerOptionsForKey);
            var targets = MessagePackSerializer.Deserialize<TKey[]>(ref reader, _messagePackSerializerOptionsForKey);
            var methodId = reader.ReadInt32();
            var payload = messageBytes.AsMemory((int)reader.Consumed);
            var context = new InvocationWriteContext(methodId, null, payload);

            foreach (var receiver in _receivers)
            {
                if (excludes is not null && excludes.Contains(receiver.Key)) continue;
                if (targets is not null && !targets.Contains(receiver.Key)) continue;
                receiver.Value.Write(context);
            }
        }
        else if (arrayLength == 2)
        {
            // Remove
            var type = reader.ReadByte();
            var key = MessagePackSerializer.Deserialize<TKey>(ref reader, _messagePackSerializerOptionsForKey);
            return AwaitTryRemoveCore(key);
        }

        return default;

        async ValueTask AwaitTryRemoveCore(TKey key)
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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NatsGroup<TKey, T>));
        }
    }

}
