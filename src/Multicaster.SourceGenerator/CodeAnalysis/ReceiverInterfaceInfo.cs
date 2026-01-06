using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.SourceGenerator.CodeAnalysis;

/// <summary>
/// Represents information about a receiver interface for proxy generation.
/// </summary>
[DebuggerDisplay("Receiver: {InterfaceType,nq}")]
public class ReceiverInterfaceInfo
{
    /// <summary>
    /// Gets the fully qualified type name of the receiver interface.
    /// </summary>
    public string InterfaceType { get; }

    /// <summary>
    /// Gets the short name of the receiver interface.
    /// </summary>
    public string InterfaceName { get; }

    /// <summary>
    /// Gets the namespace of the receiver interface.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the list of methods in the receiver interface.
    /// </summary>
    public IReadOnlyList<ReceiverMethodInfo> Methods { get; }

    public ReceiverInterfaceInfo(string interfaceType, string interfaceName, string @namespace, IReadOnlyList<ReceiverMethodInfo> methods)
    {
        InterfaceType = interfaceType;
        InterfaceName = interfaceName;
        Namespace = @namespace;
        Methods = methods;
    }
}

/// <summary>
/// Represents information about a method in a receiver interface.
/// </summary>
[DebuggerDisplay("Method: {MethodName,nq} (Id={MethodId})")]
public class ReceiverMethodInfo
{
    /// <summary>
    /// Gets the method ID (FNV1A32 hash of method name or from MethodIdAttribute).
    /// </summary>
    public int MethodId { get; }

    /// <summary>
    /// Gets the method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the return type of the method.
    /// </summary>
    public string ReturnType { get; }

    /// <summary>
    /// Gets whether the method returns void.
    /// </summary>
    public bool IsVoid { get; }

    /// <summary>
    /// Gets whether the method returns Task or ValueTask (for client results).
    /// </summary>
    public bool IsAsync { get; }

    /// <summary>
    /// Gets the inner type of Task/ValueTask if async, otherwise the return type.
    /// </summary>
    public string? AsyncResultType { get; }

    /// <summary>
    /// Gets the list of parameters.
    /// </summary>
    public IReadOnlyList<ReceiverParameterInfo> Parameters { get; }

    /// <summary>
    /// Gets the index of CancellationToken parameter if present (for client results).
    /// </summary>
    public int? CancellationTokenParameterIndex { get; }

    public ReceiverMethodInfo(
        int methodId,
        string methodName,
        string returnType,
        bool isVoid,
        bool isAsync,
        string? asyncResultType,
        IReadOnlyList<ReceiverParameterInfo> parameters,
        int? cancellationTokenParameterIndex)
    {
        MethodId = methodId;
        MethodName = methodName;
        ReturnType = returnType;
        IsVoid = isVoid;
        IsAsync = isAsync;
        AsyncResultType = asyncResultType;
        Parameters = parameters;
        CancellationTokenParameterIndex = cancellationTokenParameterIndex;
    }
}

/// <summary>
/// Represents information about a parameter in a receiver method.
/// </summary>
[DebuggerDisplay("Parameter: {Name,nq} ({Type,nq})")]
public class ReceiverParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public bool IsCancellationToken { get; }

    public ReceiverParameterInfo(string name, string type, bool isCancellationToken)
    {
        Name = name;
        Type = type;
        IsCancellationToken = isCancellationToken;
    }
}
