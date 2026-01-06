using System.Text;
using Cysharp.Runtime.Multicast.SourceGenerator.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.SourceGenerator.CodeGen;

/// <summary>
/// Generates InMemory proxy implementations for receiver interfaces.
/// </summary>
public static class InMemoryProxyGenerator
{
    public static string Generate(ReceiverInterfaceInfo receiver)
    {
        var sb = new StringBuilder();
        var safeTypeName = GetSafeTypeName(receiver.InterfaceName);

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Generated InMemory proxy for {receiver.InterfaceName}.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    file sealed class {safeTypeName}_InMemoryProxy<TKey> : global::Cysharp.Runtime.Multicast.InMemory.InMemoryProxyBase<TKey, {receiver.InterfaceType}>, {receiver.InterfaceType}");
        sb.AppendLine($"        where TKey : global::System.IEquatable<TKey>");
        sb.AppendLine($"    {{");

        // Constructor
        sb.AppendLine($"        public {safeTypeName}_InMemoryProxy(");
        sb.AppendLine($"            global::Cysharp.Runtime.Multicast.InMemory.IReceiverHolder<TKey, {receiver.InterfaceType}> receivers,");
        sb.AppendLine($"            global::System.Collections.Immutable.ImmutableArray<TKey> excludes,");
        sb.AppendLine($"            global::System.Collections.Immutable.ImmutableArray<TKey>? targets)");
        sb.AppendLine($"            : base(receivers, excludes, targets)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        // Generate methods
        foreach (var method in receiver.Methods)
        {
            GenerateInMemoryMethod(sb, method);
        }

        sb.AppendLine($"    }}");

        return sb.ToString();
    }

    static void GenerateInMemoryMethod(StringBuilder sb, ReceiverMethodInfo method)
    {
        var parameters = method.Parameters;
        var paramList = string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}"));

        sb.AppendLine($"        public {method.ReturnType} {method.MethodName}({paramList})");
        sb.AppendLine($"        {{");

        if (method.IsVoid)
        {
            // Fire-and-forget method
            if (parameters.Count == 0)
            {
                sb.AppendLine($"            Invoke(static (r) => r.{method.MethodName}());");
            }
            else
            {
                // Generate: Invoke(arg1, arg2, static (r, a1, a2) => r.Method(a1, a2));
                var argList = string.Join(", ", parameters.Select(p => p.Name));
                var lambdaParams = "r, " + string.Join(", ", parameters.Select((p, i) => $"a{i}"));
                var lambdaArgs = string.Join(", ", parameters.Select((p, i) => $"a{i}"));
                sb.AppendLine($"            Invoke({argList}, static ({lambdaParams}) => r.{method.MethodName}({lambdaArgs}));");
            }
        }
        else
        {
            // Method with return value (client result)
            if (parameters.Count == 0)
            {
                sb.AppendLine($"            return InvokeWithResult(static (r) => r.{method.MethodName}());");
            }
            else
            {
                // Generate: InvokeWithResult(arg1, arg2, static (r, a1, a2) => r.Method(a1, a2));
                var argList = string.Join(", ", parameters.Select(p => p.Name));
                var lambdaParams = "r, " + string.Join(", ", parameters.Select((p, i) => $"a{i}"));
                var lambdaArgs = string.Join(", ", parameters.Select((p, i) => $"a{i}"));
                sb.AppendLine($"            return InvokeWithResult({argList}, static ({lambdaParams}) => r.{method.MethodName}({lambdaArgs}));");
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
