using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace Cysharp.Runtime.Multicast.InMemory;

public class DynamicInMemoryProxyFactory : IInMemoryProxyFactory
{
    public static IInMemoryProxyFactory Instance { get; } = new DynamicInMemoryProxyFactory();

    private static readonly AssemblyBuilder _assemblyBuilder;
    private static readonly ModuleBuilder _moduleBuilder;

    static DynamicInMemoryProxyFactory()
    {
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"DynamicInMemoryProxyFactory-{Guid.NewGuid()}"), AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Multicaster");
    }

    public T Create<TKey, T>(IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
        where TKey : IEquatable<TKey>
    {
        return Core<TKey, T>.Create(receivers, excludes, targets);
    }

    static class Core<TKey, T>
        where TKey : IEquatable<TKey>
    {
        private static readonly Type _type;
        private static readonly Func<IReceiverHolder<TKey, T>, ImmutableArray<TKey>, ImmutableArray<TKey>?, T> _factory;

        static Core()
        {
            var typeInMemoryProxyBase = typeof(InMemoryProxyBase<TKey, T>);
            var ctorParameters = new[] { typeof(IReceiverHolder<TKey, T>), typeof(ImmutableArray<TKey>), typeof(ImmutableArray<TKey>?) };
            var typeBuilder = _moduleBuilder.DefineType($"{typeof(T).FullName!.Replace(".", "_")}_Proxy", TypeAttributes.NotPublic, typeInMemoryProxyBase);
            typeBuilder.AddInterfaceImplementation(typeof(T));

            // _thunk{Method}Delegate fields
            // _Thunk{Method} methods
            // {Method} methods
            var thunkMethods = new List<(Type[] DelegateParamTypes, FieldBuilder FieldDelegate, Type DelegateType, MethodBuilder MethodThunk)>();
            foreach (var method in typeof(T).GetInterfaces().Append(typeof(T)).SelectMany(x => x.GetMethods()))
            {
                if (method.ReturnType == typeof(void))
                {
                    // Fire-and-forget
                    // Action<T, T1, T2...>
                    Type[] delegateParamTypes = [typeof(T), .. method.GetParameters().Select(x => x.ParameterType)];
                    var delegateType = (method.GetParameters().Length switch
                    {
                        0 =>  typeof(Action<>),
                        1 =>  typeof(Action<,>),
                        2 =>  typeof(Action<,,>),
                        3 =>  typeof(Action<,,,>),
                        4 =>  typeof(Action<,,,,>),
                        5 =>  typeof(Action<,,,,,>),
                        6 =>  typeof(Action<,,,,,,>),
                        7 =>  typeof(Action<,,,,,,,>),
                        8 =>  typeof(Action<,,,,,,,,>),
                        9 =>  typeof(Action<,,,,,,,,,>),
                        10 => typeof(Action<,,,,,,,,,,>),
                        11 => typeof(Action<,,,,,,,,,,,>),
                        12 => typeof(Action<,,,,,,,,,,,,>),
                        13 => typeof(Action<,,,,,,,,,,,,,>),
                        14 => typeof(Action<,,,,,,,,,,,,,,>),
                        15 => typeof(Action<,,,,,,,,,,,,,,,>),
                        _ => throw new NotImplementedException(),
                    }).MakeGenericType(delegateParamTypes);

                    // Invoke<T1, T2...>(T1, T2, ..., Action<T, T1, T2...>)
                    var methodInvoke = typeof(InMemoryProxyBase<TKey, T>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(x => x.Name == "Invoke" && x.GetGenericArguments().Length == method.GetParameters().Length);
                    if (methodInvoke.ContainsGenericParameters)
                    {
                        methodInvoke = methodInvoke.MakeGenericMethod(method.GetParameters().Select(x => x.ParameterType).ToArray());
                    }

                    // private static readonly Action<...> _thunk{MethodName}Delegate;
                    var fieldDelegate = typeBuilder.DefineField($"_thunk{method.Name}Delegate", delegateType, FieldAttributes.Private | FieldAttributes.Static);
                    // private static void _Thunk_{MethodName}(T self, T1 arg1, T2 arg2...) => self.MethodName(arg1, arg2...);
                    var methodThunk = typeBuilder.DefineMethod($"_Thunk_{method.Name}", MethodAttributes.Private | MethodAttributes.Static, typeof(void), delegateParamTypes);
                    {
                        var il = methodThunk.GetILGenerator();
                        for (var i = 0; i < delegateParamTypes.Length; i++)
                        {
                            il.Emit(OpCodes.Ldarg, i);
                        }
                        il.Emit(OpCodes.Callvirt, method);
                        il.Emit(OpCodes.Ret);
                    }
                    // void {MethodName}(T1 arg1, T2 arg2) => Invoke(arg1, arg2..., _thunk{MethodName}Delegate);
                    var methodImpl = typeBuilder.DefineMethod(method.Name,
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        method.ReturnType,
                        method.GetParameters().Select(x => x.ParameterType).ToArray()
                    );
                    {
                        var il = methodImpl.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        for (var i = 0; i < method.GetParameters().Length; i++)
                        {
                            il.Emit(OpCodes.Ldarg, 1 + i);
                        }
                        il.Emit(OpCodes.Ldsfld, fieldDelegate);
                        il.Emit(OpCodes.Callvirt, methodInvoke);
                        il.Emit(OpCodes.Ret);
                    }

                    thunkMethods.Add((delegateParamTypes, fieldDelegate, delegateType, methodThunk));
                }
                else
                {
                    // Func<TTarget, T1, T2..., TResult>
                    Type[] delegateParamTypes = [typeof(T), .. method.GetParameters().Select(x => x.ParameterType), method.ReturnType];
                    var delegateType = (method.GetParameters().Length switch
                    {
                        0 => typeof(Func<,>),
                        1 => typeof(Func<,,>),
                        2 => typeof(Func<,,,>),
                        3 => typeof(Func<,,,,>),
                        4 => typeof(Func<,,,,,>),
                        5 => typeof(Func<,,,,,,>),
                        6 => typeof(Func<,,,,,,,>),
                        7 => typeof(Func<,,,,,,,,>),
                        8 => typeof(Func<,,,,,,,,,>),
                        9 => typeof(Func<,,,,,,,,,,>),
                        10 => typeof(Func<,,,,,,,,,,,>),
                        11 => typeof(Func<,,,,,,,,,,,,>),
                        12 => typeof(Func<,,,,,,,,,,,,,>),
                        13 => typeof(Func<,,,,,,,,,,,,,,>),
                        14 => typeof(Func<,,,,,,,,,,,,,,,>),
                        15 => typeof(Func<,,,,,,,,,,,,,,,,>),
                        _ => throw new NotImplementedException(),
                    }).MakeGenericType(delegateParamTypes);

                    // InvokeWithResult<TTarget, T1, T2..., TResult>(T1, T2, ..., Func<TTarget, T1, T2..., TResult>)
                    var methodInvokeWithResult = typeof(InMemoryProxyBase<TKey, T>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(x => x.Name == "InvokeWithResult" && x.GetGenericArguments().Length == method.GetParameters().Length + 1)
                        .MakeGenericMethod([..method.GetParameters().Select(x => x.ParameterType), method.ReturnType]);

                    // private static readonly Action<...> _thunk{MethodName}Delegate;
                    var fieldDelegate = typeBuilder.DefineField($"_thunk{method.Name}Delegate", delegateType, FieldAttributes.Private | FieldAttributes.Static);
                    // private static TResult _Thunk_{MethodName}(TTarget self, T1 arg1, T2 arg2...) => self.MethodName(arg1, arg2...);
                    Type[] thunkMethodParams = [typeof(T), .. method.GetParameters().Select(x => x.ParameterType)];
                    var methodThunk = typeBuilder.DefineMethod($"_Thunk_{method.Name}", MethodAttributes.Private | MethodAttributes.Static, method.ReturnType, thunkMethodParams);
                    {
                        var il = methodThunk.GetILGenerator();
                        for (var i = 0; i < thunkMethodParams.Length; i++)
                        {
                            il.Emit(OpCodes.Ldarg, i);
                        }
                        il.Emit(OpCodes.Callvirt, method);
                        il.Emit(OpCodes.Ret);
                    }
                    // TTarget {MethodName}(T1 arg1, T2 arg2) => Invoke(arg1, arg2..., _thunk{MethodName}Delegate);
                    var methodImpl = typeBuilder.DefineMethod(method.Name,
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        method.ReturnType,
                        method.GetParameters().Select(x => x.ParameterType).ToArray()
                    );
                    {
                        var il = methodImpl.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        for (var i = 0; i < method.GetParameters().Length; i++)
                        {
                            il.Emit(OpCodes.Ldarg, 1 + i);
                        }
                        il.Emit(OpCodes.Ldsfld, fieldDelegate);
                        il.Emit(OpCodes.Callvirt, methodInvokeWithResult);
                        il.Emit(OpCodes.Ret);
                    }

                    thunkMethods.Add((delegateParamTypes, fieldDelegate, delegateType, methodThunk));
                }

            }

            // static Proxy() { ... }
            var cctor = typeBuilder.DefineTypeInitializer();
            {
                var il = cctor.GetILGenerator();
                foreach (var thunkMethod in thunkMethods)
                {
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldftn, thunkMethod.MethodThunk);
                    il.Emit(OpCodes.Newobj, thunkMethod.DelegateType.GetConstructor([typeof(object), typeof(nint)])!);
                    il.Emit(OpCodes.Stsfld, thunkMethod.FieldDelegate);
                }
                il.Emit(OpCodes.Ret);
            }

            var ctorBase = typeInMemoryProxyBase.GetConstructor(BindingFlags.Public | BindingFlags.Instance, ctorParameters)!;
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, [typeof(IReceiverHolder<TKey, T>), typeof(ImmutableArray<TKey>), typeof(ImmutableArray<TKey>?)]);
            {
                var il = ctor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Call, ctorBase);
                il.Emit(OpCodes.Ret);
            }

            {
                var createInstanceMethod = typeBuilder.DefineMethod(
                    "CreateInstance",
                    MethodAttributes.Private | MethodAttributes.Static,
                    typeof(T),
                    [typeof(IReceiverHolder<TKey, T>), typeof(ImmutableArray<TKey>), typeof(ImmutableArray<TKey>?)]
                );
                {
                    var il = createInstanceMethod.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0); // receivers
                    il.Emit(OpCodes.Ldarg_1); // excludes
                    il.Emit(OpCodes.Ldarg_2); // targets
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Ret);
                }
            }

            _type = typeBuilder.CreateType()!;
            _factory = _type.GetMethod("CreateInstance", BindingFlags.Static | BindingFlags.NonPublic)!
                .CreateDelegate<Func<IReceiverHolder<TKey, T>, ImmutableArray<TKey>, ImmutableArray<TKey>?, T>>();
        }

        public static T Create(IReceiverHolder<TKey, T> receivers, ImmutableArray<TKey> excludes, ImmutableArray<TKey>? targets)
            => _factory(receivers, excludes, targets);
    }
}