using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Cysharp.Runtime.Multicast.Internal;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cysharp.Runtime.Multicast.Remoting;

public abstract partial class RemoteProxyBase : IRemoteProxy
{
    private readonly IRemoteReceiverWriter _writer;
    private readonly IRemoteSerializer _serializer;

    protected RemoteProxyBase(IRemoteReceiverWriter writer, IRemoteSerializer serializer)
    {
        _writer = writer;
        _serializer = serializer;
    }

    bool IRemoteProxy.TryGetDirectWriter([NotNullWhen(true)] out IRemoteReceiverWriter? receiver)
        => (receiver = (_writer as RemoteDirectWriter)?.Writer) is not null;

    protected void Invoke(string name, int methodId)
    {
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        _serializer.SerializeInvocation(writer, new SerializationContext(name, methodId, null));
        _writer.Write(writer.WrittenMemory);
    }

    protected Task InvokeWithResultNoReturnValue(string name, int methodId, CancellationToken timeoutCancellationToken = default)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask(name, methodId, timeoutCancellationToken);
        _serializer.SerializeInvocation(writer, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }

    protected Task<TResult> InvokeWithResult<TResult>(string name, int methodId, CancellationToken timeoutCancellationToken = default)
    {
        ThrowIfNotSingleWriter();
        using var writer = ArrayPoolBufferWriter.RentThreadStaticWriter();
        var (task, messageId) = EnqueuePendingTask<TResult>(name, methodId, timeoutCancellationToken);
        _serializer.SerializeInvocation(writer, new SerializationContext(name, methodId, messageId));
        _writer.Write(writer.WrittenMemory);
        return task;
    }

    private (Task Task, Guid MessageId) EnqueuePendingTask(string name, int methodId, CancellationToken timeoutCancellationToken = default)
    {
        var messageId = Guid.NewGuid();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingTask = _writer.PendingTasks.CreateTask(name, methodId, messageId, tcs, timeoutCancellationToken, _serializer);
        _writer.PendingTasks.Register(pendingTask);
        return (tcs.Task, messageId);
    }

    private (Task<TResult> Task, Guid MessageId) EnqueuePendingTask<TResult>(string name, int methodId, CancellationToken timeoutCancellationToken = default)
    {
        var messageId = Guid.NewGuid();

        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingTask = _writer.PendingTasks.CreateTask<TResult>(name, methodId, messageId, tcs, timeoutCancellationToken, _serializer);
        _writer.PendingTasks.Register(pendingTask);
        return (tcs.Task, messageId);
    }

    private void ThrowIfNotSingleWriter()
    {
        if (_writer is not IRemoteSingleWriter)
        {
            throw new NotSupportedException("The client results method does not support multiple targets. Please use `Single` method.");
        }
    }

    internal class RemoteMultiWriter<TKey> : IRemoteReceiverWriter
        where TKey : IEquatable<TKey>
    {
        private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _remoteReceivers;
        private readonly ImmutableArray<TKey> _excludes;
        private readonly ImmutableArray<TKey>? _targets;

        public RemoteMultiWriter(ConcurrentDictionary<TKey, IRemoteReceiverWriter> remoteReceivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
        {
            _remoteReceivers = remoteReceivers;
            _excludes = excludes;
            _targets = targets;
        }

        IRemoteClientResultPendingTaskRegistry IRemoteReceiverWriter.PendingTasks
            => NotSupportedRemoteClientResultPendingTaskRegistry.Instance;

        void IRemoteReceiverWriter.Write(ReadOnlyMemory<byte> payload)
        {
            foreach (var (receiverId, receiver) in _remoteReceivers)
            {
                if (_excludes.Contains(receiverId)) continue;
                if (_targets is not null && !_targets.Contains(receiverId)) continue;
                try
                {
                    receiver.Write(payload);
                }
                catch
                {
                    // Ignore
                }
            }
        }
    }

    internal class RemoteSingleWriter<TKey> : IRemoteReceiverWriter, IRemoteSingleWriter
        where TKey : IEquatable<TKey>
    {
        private readonly ConcurrentDictionary<TKey, IRemoteReceiverWriter> _remoteReceivers;
        private readonly TKey _target;

        public RemoteSingleWriter(ConcurrentDictionary<TKey, IRemoteReceiverWriter> remoteReceivers, TKey target)
        {
            _remoteReceivers = remoteReceivers;
            _target = target;
        }

        IRemoteClientResultPendingTaskRegistry IRemoteReceiverWriter.PendingTasks
            => _remoteReceivers.GetValueOrDefault(_target)?.PendingTasks ?? throw new InvalidOperationException("The target is not found.");

        void IRemoteReceiverWriter.Write(ReadOnlyMemory<byte> payload)
        {
            try
            {
                _remoteReceivers.GetValueOrDefault(_target)?.Write(payload);
            }
            catch
            {
                // Ignore
            }
        }
    }

    internal class RemoteDirectWriter : IRemoteReceiverWriter, IRemoteSingleWriter
    {
        public IRemoteReceiverWriter Writer { get; }

        public RemoteDirectWriter(IRemoteReceiverWriter writer)
        {
            Writer = writer;
        }

        IRemoteClientResultPendingTaskRegistry IRemoteReceiverWriter.PendingTasks => Writer.PendingTasks;

        void IRemoteReceiverWriter.Write(ReadOnlyMemory<byte> payload)
        {
            try
            {
                Writer.Write(payload);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
