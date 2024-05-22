using Cysharp.Runtime.Multicast.Internal;
using System.Reflection;
using System.Reflection.Emit;

namespace Cysharp.Runtime.Multicast.Remoting;

public class DynamicRemoteProxyFactory : IRemoteProxyFactory
{
    public static IRemoteProxyFactory Instance { get; } = new DynamicRemoteProxyFactory();

    private static readonly AssemblyBuilder _assemblyBuilder;
    private static readonly ModuleBuilder _moduleBuilder;

    static DynamicRemoteProxyFactory()
    {
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"DynamicRemoteProxyFactory-{Guid.NewGuid()}"), AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Multicaster");
    }

    public T Create<T>(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
    {
        return Core<T>.Create(writer, serializer, pendingQueue);
    }

    static class Core<T>
    {
        private static readonly Type _type;

        static Core()
        {
            var typeBuilder = _moduleBuilder.DefineType($"{typeof(T).FullName!.Replace(".", "_")}_Proxy", TypeAttributes.Class, typeof(RemoteProxyBase));
            typeBuilder.AddInterfaceImplementation(typeof(T));
            var ctorBase = typeof(RemoteProxyBase).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(IRemoteReceiverWriter), typeof(IRemoteSerializer), typeof(IRemoteCallPendingMessageQueue)])!;
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, [typeof(IRemoteReceiverWriter), typeof(IRemoteSerializer), typeof(IRemoteCallPendingMessageQueue)]);
            {
                var il = ctor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Call, ctorBase);
                il.Emit(OpCodes.Ret);
            }

            foreach (var method in typeof(T).GetMethods())
            {
                if (method.ReturnType == typeof(void))
                {
                    var methodBuilder = typeBuilder.DefineMethod(
                        method.Name,
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        typeof(void),
                        method.GetParameters().Select(x => x.ParameterType).ToArray()
                    );
                    var methodWrite = typeof(RemoteProxyBase)
                        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(x => x.Name == "Invoke" && x.GetGenericArguments().Length == method.GetParameters().Length)
                        .MakeGenericMethod(method.GetParameters().Select(x => x.ParameterType).ToArray());

                    {
                        var il = methodBuilder.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0); // this
                        il.Emit(OpCodes.Ldstr, method.Name); // name
                        il.Emit(OpCodes.Ldc_I4, FNV1A32.GetHashCode(method.Name)); // methodId
                        for (var i = 0; i < method.GetParameters().Length; i++)
                        {
                            il.Emit(OpCodes.Ldarg, 1 + i);
                        }
                        il.Emit(OpCodes.Callvirt, methodWrite); // base.Invoke(method.Name, methodId, arg1, arg2 ...);
                        il.Emit(OpCodes.Ret);
                    }
                }
                else
                {
                    var methodBuilder = typeBuilder.DefineMethod(
                        method.Name,
                        MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                        method.ReturnType,
                        method.GetParameters().Select(x => x.ParameterType).ToArray()
                    );
                    var methodInvokeWithResult = typeof(RemoteProxyBase)
                        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(x => x.Name == "InvokeWithResult" && x.GetGenericArguments().Length == method.GetParameters().Length + 1)
                        .MakeGenericMethod([..method.GetParameters().Select(x => x.ParameterType), method.ReturnType.GetGenericArguments()[0]]);

                    {
                        var il = methodBuilder.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0); // this
                        il.Emit(OpCodes.Ldstr, method.Name); // name
                        il.Emit(OpCodes.Ldc_I4, FNV1A32.GetHashCode(method.Name)); // methodId
                        for (var i = 0; i < method.GetParameters().Length; i++)
                        {
                            il.Emit(OpCodes.Ldarg, 1 + i);
                        }
                        il.Emit(OpCodes.Callvirt, methodInvokeWithResult); // base.InvokeWithResponse<TArg1, TArg2..., TResult>(method.Name, methodId, arg1, arg2 ...);
                        il.Emit(OpCodes.Ret);
                    }
                }
            }

            _type = typeBuilder.CreateType()!;
        }

        public static T Create(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteCallPendingMessageQueue pendingQueue)
        {
            return (T)Activator.CreateInstance(_type, [writer, serializer, pendingQueue])!;
        }
    }
}

