﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cysharp.Runtime.Multicast.InMemory;

public abstract class InMemoryProxyBase<TKey, T>
    where TKey : IEquatable<TKey>
{
    private readonly IReceiverHolder<TKey, T> _receivers;
    private readonly ImmutableArray<TKey> _excludes;
    private readonly ImmutableArray<TKey>? _targets;
    private readonly bool _alwaysInvokable;

    public InMemoryProxyBase(IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
    {
        _receivers = receivers;
        _excludes = excludes;
        _targets = targets;
        _alwaysInvokable = _excludes.IsEmpty && _targets is null;
    }

    protected void Invoke(Action<T> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1>(T1 arg1, Action<T, T1> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2>(T1 arg1, T2 arg2, Action<T, T1, T2> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Action<T, T1, T2, T3> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Action<T, T1, T2, T3, T4> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Action<T, T1, T2, T3, T4, T5> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Action<T, T1, T2, T3, T4, T5, T6> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Action<T, T1, T2, T3, T4, T5, T6, T7> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, Action<T, T1, T2, T3, T4, T5, T6, T7, T8> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> invoker)
    {
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            }
            catch
            {
                // Ignore
            }
        }
    }

    [DoesNotReturn]
    private TResult ThrowNoInvocableTarget<TResult>() => throw new InvalidOperationException("No invocable target found.");

    protected TResult InvokeWithResult<TResult>(Func<T, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver);
            }
            catch
            {
                // Ignore
            }
        }

        return ThrowNoInvocableTarget<TResult>();
    }

    protected TResult InvokeWithResult<T1, TResult>(T1 arg1, Func<T, T1, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, TResult>(T1 arg1, T2 arg2, Func<T, T1, T2, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3, Func<T, T1, T2, T3, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Func<T, T1, T2, T3, T4, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Func<T, T1, T2, T3, T4, T5, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Func<T, T1, T2, T3, T4, T5, T6, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Func<T, T1, T2, T3, T4, T5, T6, T7, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> invoker)
    {
        ThrowIfNotSingle();
        using var snapshot = _receivers.AsSnapshot();
        foreach (var receiverRegistration in snapshot.AsSpan())
        {
            if (!_alwaysInvokable && !CanInvoke(receiverRegistration)) continue;
            try
            {
                return invoker(receiverRegistration.Receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
            }
            catch
            {
                // Ignore
            }
        }
        return ThrowNoInvocableTarget<TResult>();
    }

    private void ThrowIfNotSingle()
    {
        if (_targets is not { Length: 1 })
        {
            throw new NotSupportedException("In-memory proxy does not support to invoke multiple receivers.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanInvoke(ReceiverRegistration<TKey, T> r)
        => !r.HasKey || r.Key is null || (!_excludes.Contains(r.Key) && (_targets is null || _targets.Value.Contains(r.Key)));
}