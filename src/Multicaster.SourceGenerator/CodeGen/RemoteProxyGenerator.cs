using System.Text;
using Cysharp.Runtime.Multicast.SourceGenerator.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.SourceGenerator.CodeGen;

/// <summary>
/// Generates Remote proxy implementations for receiver interfaces.
/// </summary>
public static class RemoteProxyGenerator
{
    public static string Generate(ReceiverInterfaceInfo receiver)
    {
        var sb = new StringBuilder();
        var safeTypeName = GetSafeTypeName(receiver.InterfaceName);

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Generated Remote proxy for {receiver.InterfaceName}.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    file sealed class {safeTypeName}_RemoteProxy : global::Cysharp.Runtime.Multicast.Remoting.RemoteProxyBase, {receiver.InterfaceType}");
        sb.AppendLine($"    {{");

        // Constructor
        sb.AppendLine($"        public {safeTypeName}_RemoteProxy(");
        sb.AppendLine($"            global::Cysharp.Runtime.Multicast.Remoting.IRemoteReceiverWriter writer,");
        sb.AppendLine($"            global::Cysharp.Runtime.Multicast.Remoting.IRemoteSerializer serializer)");
        sb.AppendLine($"            : base(writer, serializer)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        // Generate methods
        foreach (var method in receiver.Methods)
        {
            GenerateRemoteMethod(sb, method);
        }

        sb.AppendLine($"    }}");

        return sb.ToString();
    }

    static void GenerateRemoteMethod(StringBuilder sb, ReceiverMethodInfo method)
    {
        var parameters = method.Parameters;
        var paramList = string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}"));

        sb.AppendLine($"        public {method.ReturnType} {method.MethodName}({paramList})");
        sb.AppendLine($"        {{");

        if (method.IsVoid)
        {
            // Fire-and-forget method - use Invoke
            // Invoke(name, methodId) or Invoke<T1, T2, ...>(name, methodId, arg1, arg2, ...)
            if (parameters.Count == 0)
            {
                sb.AppendLine($"            Invoke(\"{method.MethodName}\", {method.MethodId});");
            }
            else
            {
                var argList = string.Join(", ", parameters.Select(p => p.Name));
                sb.AppendLine($"            Invoke(\"{method.MethodName}\", {method.MethodId}, {argList});");
            }
        }
        else
        {
            // Method with return value (client result) - use InvokeWithResult
            var nonCancellationParams = parameters.Where(p => !p.IsCancellationToken).ToList();
            var cancellationTokenParam = parameters.FirstOrDefault(p => p.IsCancellationToken);
            var ctArg = cancellationTokenParam?.Name ?? "default";

            if (method.AsyncResultType is null)
            {
                // Task or ValueTask without result - use InvokeWithResultNoReturnValue
                if (nonCancellationParams.Count == 0)
                {
                    sb.AppendLine($"            return InvokeWithResultNoReturnValue(\"{method.MethodName}\", {method.MethodId}, {ctArg});");
                }
                else
                {
                    var argList = string.Join(", ", nonCancellationParams.Select(p => p.Name));
                    sb.AppendLine($"            return InvokeWithResultNoReturnValue(\"{method.MethodName}\", {method.MethodId}, {argList}, {ctArg});");
                }
            }
            else
            {
                // Task<T> or ValueTask<T> - use InvokeWithResult<T1, ..., TResult>
                // The generic parameters are: T1, T2, ..., TResult (argument types followed by result type)
                if (nonCancellationParams.Count == 0)
                {
                    sb.AppendLine($"            return InvokeWithResult<{method.AsyncResultType}>(\"{method.MethodName}\", {method.MethodId}, {ctArg});");
                }
                else
                {
                    var argList = string.Join(", ", nonCancellationParams.Select(p => p.Name));
                    // Generic type parameters: T1, T2, ..., TResult
                    var typeParams = string.Join(", ", nonCancellationParams.Select(p => p.Type)) + ", " + method.AsyncResultType;
                    sb.AppendLine($"            return InvokeWithResult<{typeParams}>(\"{method.MethodName}\", {method.MethodId}, {argList}, {ctArg});");
                }
            }
        }

        sb.AppendLine($"        }}");
        sb.AppendLine();
    }

    static string GetSafeTypeName(string typeName)
    {
        return typeName.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
    }
}
