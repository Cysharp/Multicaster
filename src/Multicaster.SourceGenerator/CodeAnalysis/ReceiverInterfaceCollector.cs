using Cysharp.Runtime.Multicast.SourceGenerator.Internal;
using Microsoft.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.SourceGenerator.CodeAnalysis;

/// <summary>
/// Collects receiver interface information from type symbols.
/// </summary>
public static class ReceiverInterfaceCollector
{
    public static (IReadOnlyList<ReceiverInterfaceInfo> Receivers, IReadOnlyList<Diagnostic> Diagnostics) Collect(
        IReadOnlyList<INamedTypeSymbol> interfaceTypes,
        CancellationToken cancellationToken)
    {
        var receivers = new List<ReceiverInterfaceInfo>();
        var diagnostics = new List<Diagnostic>();

        foreach (var interfaceType in interfaceTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryCollectReceiverInterface(interfaceType, out var receiverInfo, out var receiverDiagnostics))
            {
                receivers.Add(receiverInfo);
            }
            diagnostics.AddRange(receiverDiagnostics);
        }

        return (receivers, diagnostics);
    }

    static bool TryCollectReceiverInterface(
        INamedTypeSymbol interfaceType,
        out ReceiverInterfaceInfo receiverInfo,
        out List<Diagnostic> diagnostics)
    {
        receiverInfo = null!;
        diagnostics = new List<Diagnostic>();

        if (interfaceType.TypeKind != TypeKind.Interface)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.TypeMustBeInterface,
                interfaceType.Locations.FirstOrDefault(),
                interfaceType.ToDisplayString()));
            return false;
        }

        var methods = new List<ReceiverMethodInfo>();

        // Collect methods from the interface and all base interfaces
        foreach (var method in GetAllInterfaceMethods(interfaceType))
        {
            if (TryCollectMethod(method, out var methodInfo, out var methodDiagnostic))
            {
                methods.Add(methodInfo);
            }

            if (methodDiagnostic is not null)
            {
                diagnostics.Add(methodDiagnostic);
            }
        }

        var interfaceFullName = interfaceType.GetFullyQualifiedTypeName();
        var interfaceName = interfaceType.Name;
        var ns = interfaceType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : interfaceType.ContainingNamespace.ToDisplayString();

        receiverInfo = new ReceiverInterfaceInfo(interfaceFullName, interfaceName, ns, methods);
        return true;
    }

    static IEnumerable<IMethodSymbol> GetAllInterfaceMethods(INamedTypeSymbol interfaceType)
    {
        foreach (var member in interfaceType.GetMembers())
        {
            if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
            {
                yield return method;
            }
        }

        foreach (var baseInterface in interfaceType.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
                {
                    yield return method;
                }
            }
        }
    }

    static bool TryCollectMethod(
        IMethodSymbol method,
        out ReceiverMethodInfo methodInfo,
        out Diagnostic? diagnostic)
    {
        methodInfo = null!;
        diagnostic = null;

        var methodId = GetMethodId(method);
        var returnType = method.ReturnType.GetFullyQualifiedTypeName();
        var isVoid = method.ReturnsVoid;
        var isAsync = false;
        string? asyncResultType = null;

        // Check for Task/ValueTask return types (client results)
        if (!isVoid && method.ReturnType is INamedTypeSymbol namedReturnType)
        {
            var returnTypeName = namedReturnType.OriginalDefinition.ToDisplayString();
            if (returnTypeName == "System.Threading.Tasks.Task" ||
                returnTypeName == "System.Threading.Tasks.ValueTask")
            {
                isAsync = true;
                asyncResultType = null; // No result type
            }
            else if (returnTypeName == "System.Threading.Tasks.Task<TResult>" ||
                     returnTypeName == "System.Threading.Tasks.ValueTask<TResult>")
            {
                isAsync = true;
                asyncResultType = namedReturnType.TypeArguments[0].GetFullyQualifiedTypeName();
            }
        }

        // Collect parameters
        var parameters = new List<ReceiverParameterInfo>();
        int? cancellationTokenIndex = null;

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var param = method.Parameters[i];
            var paramType = param.Type.GetFullyQualifiedTypeName();
            var isCancellationToken = paramType == "global::System.Threading.CancellationToken";

            if (isCancellationToken)
            {
                cancellationTokenIndex = i;
            }

            parameters.Add(new ReceiverParameterInfo(param.Name, paramType, isCancellationToken));
        }

        // Validate: void methods should not have CancellationToken (it's for client results)
        if (isVoid && cancellationTokenIndex.HasValue)
        {
            diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.VoidMethodCannotHaveCancellationToken,
                method.Locations.FirstOrDefault(),
                method.Name);
            // Continue anyway, just skip the CancellationToken
        }

        methodInfo = new ReceiverMethodInfo(
            methodId,
            method.Name,
            returnType,
            isVoid,
            isAsync,
            asyncResultType,
            parameters,
            cancellationTokenIndex);

        return true;
    }

    static int GetMethodId(IMethodSymbol method)
    {
        // Check for MethodIdAttribute
        var attr = method.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.Name is "MethodIdAttribute" or "MethodId");

        if (attr is not null && attr.ConstructorArguments.Length > 0)
        {
            var value = attr.ConstructorArguments[0].Value;
            if (value is int intValue)
            {
                return intValue;
            }
        }

        return FNV1A32.GetHashCode(method.Name);
    }
}
