using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Cysharp.Runtime.Multicast.InMemory;

public abstract class InMemoryProxyBase<T>
{
    private readonly IEnumerable<KeyValuePair<Guid, T>> _receivers;
    private readonly ImmutableArray<Guid> _excludes;
    private readonly ImmutableArray<Guid>? _targets;

    public InMemoryProxyBase(IEnumerable<KeyValuePair<Guid, T>> receivers, ImmutableArray<Guid> excludes, ImmutableArray<Guid>? targets)
    {
        _receivers = receivers;
        _excludes = excludes;
        _targets = targets;
    }

    protected void Invoke(Action<T> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1>(T1 arg1, Action<T, T1> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2>(T1 arg1, T2 arg2, Action<T, T1, T2> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Action<T, T1, T2, T3> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Action<T, T1, T2, T3, T4> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Action<T, T1, T2, T3, T4, T5> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Action<T, T1, T2, T3, T4, T5, T6> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Action<T, T1, T2, T3, T4, T5, T6, T7> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, Action<T, T1, T2, T3, T4, T5, T6, T7, T8> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected void Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, Action<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> invoker)
    {
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            }
            catch
            {
                // Ignore
            }
        }
    }
    protected TResult InvokeWithResult<TResult>(Func<T, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, TResult>(T1 arg1, Func<T, T1, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, TResult>(T1 arg1, T2 arg2, Func<T, T1, T2, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3, Func<T, T1, T2, T3, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Func<T, T1, T2, T3, T4, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Func<T, T1, T2, T3, T4, T5, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Func<T, T1, T2, T3, T4, T5, T6, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Func<T, T1, T2, T3, T4, T5, T6, T7, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }
    protected TResult InvokeWithResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, Func<T, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> invoker)
    {
        ThrowIfNotSingle();
        foreach (var (key, receiver) in _receivers)
        {
            if (!CanInvoke(key)) continue;
            try
            {
                return invoker(receiver, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
            }
            catch
            {
                // Ignore
            }
        }
        throw new InvalidOperationException();
    }

    private void ThrowIfNotSingle()
    {
        if (_targets is not { Length: 1 })
        {
            throw new NotSupportedException("In-memory proxy does not support to invoke multiple receivers.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanInvoke(Guid key)
        => !_excludes.Contains(key) &&
           (_targets is null || _targets.Value.Contains(key));

}