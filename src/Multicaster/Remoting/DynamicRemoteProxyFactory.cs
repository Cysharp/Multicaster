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

    public T Create<T>(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        return Core<T>.Create(writer, serializer, pendingTasks);
    }

    static class Core<T>
    {
        private static readonly Type _type;

        static Core()
        {
            var typeBuilder = _moduleBuilder.DefineType($"{typeof(T).FullName!.Replace(".", "_")}_Proxy", TypeAttributes.Class, typeof(RemoteProxyBase));
            typeBuilder.AddInterfaceImplementation(typeof(T));
            var ctorBase = typeof(RemoteProxyBase).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(IRemoteReceiverWriter), typeof(IRemoteSerializer), typeof(IRemoteClientResultPendingTaskRegistry)])!;
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, [typeof(IRemoteReceiverWriter), typeof(IRemoteSerializer), typeof(IRemoteClientResultPendingTaskRegistry)]);
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
                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    method.ReturnType,
                    method.GetParameters().Select(x => x.ParameterType).ToArray()
                );

                var methodInvoke = method.ReturnType == typeof(void)
                    ? MethodInvokeHelper.GetInvokeMethodInfo(method)
                    : MethodInvokeHelper.GetInvokeWithResultMethodInfo(method);

                {
                    var il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldstr, method.Name); // name
                    il.Emit(OpCodes.Ldc_I4, FNV1A32.GetHashCode(method.Name)); // methodId
                    for (var i = 0; i < method.GetParameters().Length; i++)
                    {
                        il.Emit(OpCodes.Ldarg, 1 + i);
                    }
                    il.Emit(OpCodes.Callvirt, methodInvoke); // base.Invoke(method.Name, methodId, arg1, arg2 ...);
                    il.Emit(OpCodes.Ret);
                }
            }

            _type = typeBuilder.CreateType()!;
        }

        public static T Create(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
        {
            return (T)Activator.CreateInstance(_type, [writer, serializer, pendingTasks])!;
        }
    }

    static class MethodInvokeHelper
    {
        private static readonly Dictionary<int, MethodInfo> MethodInfoInvoke;
        private static readonly Dictionary<int, MethodInfo> MethodInfoInvokeWithResult;
        private static readonly Dictionary<int, MethodInfo> MethodInfoInvokeWithResultNoReturnValue;

        static MethodInvokeHelper()
        {
            MethodInfoInvoke = typeof(RemoteProxyBase)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "Invoke")
                .ToDictionary(k => k.GetGenericArguments().Length, v => v);
            MethodInfoInvokeWithResult = typeof(RemoteProxyBase)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "InvokeWithResult")
                .ToDictionary(k => k.GetGenericArguments().Length, v => v);
            MethodInfoInvokeWithResultNoReturnValue = typeof(RemoteProxyBase)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "InvokeWithResultNoReturnValue")
                .ToDictionary(k => k.GetGenericArguments().Length, v => v);
        }

        public static MethodInfo GetInvokeMethodInfo(MethodInfo interfaceMethod)
        {
            var parameters = interfaceMethod.GetParameters();
            if (parameters.Length > 15)
            {
                throw new NotSupportedException($"A method must have less than 15 parameters. Method '{interfaceMethod.Name}' has {parameters.Length} parameters.");
            }

            var methodInvoke = MethodInfoInvoke[parameters.Length];

            if (methodInvoke.ContainsGenericParameters)
            {
                methodInvoke = methodInvoke.MakeGenericMethod([.. parameters.Select(x => x.ParameterType)]);
            }

            return methodInvoke;
        }

        public static MethodInfo GetInvokeWithResultMethodInfo(MethodInfo interfaceMethod)
        {
            var parameters = interfaceMethod.GetParameters();
            if (parameters.Length > 15)
            {
                throw new NotSupportedException($"A method must have less than 15 parameters. Method '{interfaceMethod.Name}' has {parameters.Length} parameters.");
            }

            var isGenericTaskOrValueTask = interfaceMethod.ReturnType.IsConstructedGenericType;
            if (isGenericTaskOrValueTask)
            {
                return MethodInfoInvokeWithResult[parameters.Length + 1].MakeGenericMethod([.. parameters.Select(x => x.ParameterType), interfaceMethod.ReturnType.GetGenericArguments()[0]]);

            }
            else
            {
                if (parameters.Length == 0)
                {
                    return MethodInfoInvokeWithResultNoReturnValue[parameters.Length];
                }
                else
                {
                    return MethodInfoInvokeWithResultNoReturnValue[parameters.Length].MakeGenericMethod([.. parameters.Select(x => x.ParameterType)]);
                }
            }
        }
    }
}

