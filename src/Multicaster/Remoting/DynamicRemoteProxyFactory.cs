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
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Cysharp.Runtime.Multicast.Remoting.DynamicRemoteProxyFactory-{Guid.NewGuid()}"), AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Multicaster");
    }

    public T Create<T>(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
    {
        return Core<T>.Create(writer, serializer, pendingTasks);
    }

    static class Core<T>
    {
        private static readonly Type _type;
        private static readonly Func<IRemoteReceiverWriter, IRemoteSerializer, IRemoteClientResultPendingTaskRegistry, T> _factory;

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

            foreach (var method in typeof(T).GetInterfaces().Append(typeof(T)).SelectMany(x => x.GetMethods()))
            {
                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    method.ReturnType,
                    method.GetParameters().Select(x => x.ParameterType).ToArray()
                );

                if (method.ReturnType == typeof(void))
                {
                    // Standard Broadcast
                    var methodInvoke = MethodInvokeHelper.GetInvokeMethodInfo(method);
                    var il = methodBuilder.GetILGenerator();

                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldstr, method.Name); // name
                    il.Emit(OpCodes.Ldc_I4, MethodInvokeHelper.GetMethodId(method)); // methodId
                    for (var i = 0; i < method.GetParameters().Length; i++)
                    {
                        il.Emit(OpCodes.Ldarg, 1 + i);
                    }

                    il.Emit(OpCodes.Callvirt, methodInvoke); // base.Invoke(method.Name, methodId, arg1, arg2 ...);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    // Client Result
                    var (methodInvoke, cancellationTokenIndex) =  MethodInvokeHelper.GetInvokeWithResultMethodInfo(method);
                    var il = methodBuilder.GetILGenerator();

                    var local_ctDefault = default(LocalBuilder);
                    if (!cancellationTokenIndex.HasValue)
                    {
                        local_ctDefault = il.DeclareLocal(typeof(CancellationToken));
                    }

                    il.Emit(OpCodes.Ldarg_0); // this
                    il.Emit(OpCodes.Ldstr, method.Name); // name
                    il.Emit(OpCodes.Ldc_I4, MethodInvokeHelper.GetMethodId(method)); // methodId
                    for (var i = 0; i < method.GetParameters().Length; i++)
                    {
                        if (cancellationTokenIndex.HasValue && cancellationTokenIndex.Value == i)
                        {
                            continue; // Skip if the parameter is CancellationToken for client result.
                        }
                        il.Emit(OpCodes.Ldarg, 1 + i);
                    }

                    // CancellationToken
                    if (cancellationTokenIndex.HasValue)
                    {
                        il.Emit(OpCodes.Ldarg, 1 + cancellationTokenIndex.Value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloca_S, local_ctDefault!);
                        il.Emit(OpCodes.Initobj, typeof(CancellationToken));
                        il.Emit(OpCodes.Ldloc_S, local_ctDefault!);
                    }

                    il.Emit(OpCodes.Callvirt, methodInvoke); // base.Invoke(method.Name, methodId, arg1, arg2 ...);
                    il.Emit(OpCodes.Ret);
                }
            }

            {
                var methodBuilder = typeBuilder.DefineMethod(
                    "CreateInstance",
                    MethodAttributes.Private | MethodAttributes.Static,
                    typeof(T),
                    [typeof(IRemoteReceiverWriter), typeof(IRemoteSerializer), typeof(IRemoteClientResultPendingTaskRegistry)]
                );
                {
                    var il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0); // writer
                    il.Emit(OpCodes.Ldarg_1); // serializer
                    il.Emit(OpCodes.Ldarg_2); // pendingTasks
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Ret);
                }
            }
            _type = typeBuilder.CreateType()!;

            _factory = _type.GetMethod("CreateInstance", BindingFlags.Static | BindingFlags.NonPublic)!
                .CreateDelegate<Func<IRemoteReceiverWriter, IRemoteSerializer, IRemoteClientResultPendingTaskRegistry, T>>();
        }

        public static T Create(IRemoteReceiverWriter writer, IRemoteSerializer serializer, IRemoteClientResultPendingTaskRegistry pendingTasks)
            => _factory(writer, serializer, pendingTasks);
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

        public static int GetMethodId(MethodInfo interfaceMethod)
        {
            return (int?)interfaceMethod.GetCustomAttributes()
                .Where(x => x.GetType().Name is "MethodId" or "MethodIdAttribute")
                .Select(x => (Attribute: x, Field: x.GetType().GetField("MethodId"), Property: x.GetType().GetProperty("MethodId")))
                .Where(x => x.Field is not null || x.Property is not null)
                .Select(x => x.Field?.GetValue(x.Attribute) ?? x.Property?.GetValue(x.Attribute))
                .FirstOrDefault() ?? FNV1A32.GetHashCode(interfaceMethod.Name);
        }

        public static MethodInfo GetInvokeMethodInfo(MethodInfo interfaceMethod)
        {
            var parameters = interfaceMethod.GetParameters();
            if (parameters.Length > 15)
            {
                throw new NotSupportedException($"A receiver method must have less than 15 parameters. Method '{interfaceMethod.Name}' has {parameters.Length} parameters.");
            }

            var methodInvoke = MethodInfoInvoke[parameters.Length];
            if (methodInvoke.ContainsGenericParameters)
            {
                methodInvoke = methodInvoke.MakeGenericMethod([.. parameters.Select(x => x.ParameterType)]);
            }

            return methodInvoke;
        }

        public static (MethodInfo MethodInfo, int? CancellationTokenInde) GetInvokeWithResultMethodInfo(MethodInfo interfaceMethod)
        {
            var parameters = interfaceMethod.GetParameters();
            if (parameters.Length > 15)
            {
                throw new NotSupportedException($"A receiver method must have less than 15 parameters. Method '{interfaceMethod.Name}' has {parameters.Length} parameters.");
            }

            var clientResultCancellationParams = parameters
                .Select((x, i) => (ParameterInfo: x, Index: i))
                .Where(x => x.ParameterInfo.ParameterType == typeof(CancellationToken))
                .ToArray();

            var cancellationTokenIndex = default(int?);
            if (clientResultCancellationParams.Any())
            {
                // The method has CancellationToken parameter for Client Result
                if (clientResultCancellationParams.Length > 1)
                {
                    throw new NotSupportedException("A receiver method has multiple CancellationToken parameter for client result. Only one CancellationToken parameter is allowed for client result.");
                }

                cancellationTokenIndex = clientResultCancellationParams.Select(x => x.Index).First();
                parameters = parameters
                    .Where(x => x.ParameterType != typeof(CancellationToken))
                    .ToArray();
            }

            var isGenericTaskOrValueTask = interfaceMethod.ReturnType.IsConstructedGenericType;
            if (isGenericTaskOrValueTask)
            {
                return (MethodInfoInvokeWithResult[parameters.Length + 1].MakeGenericMethod([.. parameters.Select(x => x.ParameterType), interfaceMethod.ReturnType.GetGenericArguments()[0]]), cancellationTokenIndex);

            }
            else
            {
                if (parameters.Length == 0)
                {
                    return (MethodInfoInvokeWithResultNoReturnValue[parameters.Length], cancellationTokenIndex);
                }
                else
                {
                    return (MethodInfoInvokeWithResultNoReturnValue[parameters.Length].MakeGenericMethod([.. parameters.Select(x => x.ParameterType)]), cancellationTokenIndex);
                }
            }
        }
    }
}

