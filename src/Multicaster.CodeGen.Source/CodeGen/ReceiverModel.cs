using System.Collections.Generic;

namespace Cysharp.Runtime.Multicast.CodeGen;

internal enum ReceiverReturnKind
{
    Void,
    Task,
    TaskOfT,
}

internal sealed class ReceiverParameterDefinition
{
    public ReceiverParameterDefinition(string typeName, string name, bool isCancellationToken)
    {
        TypeName = typeName;
        Name = name;
        IsCancellationToken = isCancellationToken;
    }

    public string TypeName { get; }

    public string Name { get; }

    public bool IsCancellationToken { get; }
}

internal sealed class ReceiverMethodDefinition
{
    public ReceiverMethodDefinition(
        string name,
        string returnTypeName,
        ReceiverReturnKind returnKind,
        string? resultTypeName,
        int methodId,
        IReadOnlyList<ReceiverParameterDefinition> parameters,
        string? declaringTypeName = null)
    {
        Name = name;
        ReturnTypeName = returnTypeName;
        ReturnKind = returnKind;
        ResultTypeName = resultTypeName;
        MethodId = methodId;
        Parameters = parameters;
        DeclaringTypeName = declaringTypeName;
    }

    public string Name { get; }

    public string ReturnTypeName { get; }

    public ReceiverReturnKind ReturnKind { get; }

    public string? ResultTypeName { get; }

    public int MethodId { get; }

    public IReadOnlyList<ReceiverParameterDefinition> Parameters { get; }

    // Defaults to the receiver root for adapters that only describe root-declared methods.
    public string? DeclaringTypeName { get; }
}

internal sealed class ReceiverDefinition
{
    public ReceiverDefinition(string typeName, string proxyName, IReadOnlyList<ReceiverMethodDefinition> methods)
    {
        TypeName = typeName;
        ProxyName = proxyName;
        Methods = methods;
    }

    public string TypeName { get; }

    public string ProxyName { get; }

    public IReadOnlyList<ReceiverMethodDefinition> Methods { get; }
}

internal sealed class CodeGenerationDiagnostic
{
    public CodeGenerationDiagnostic(string id, string message)
    {
        Id = id;
        Message = message;
    }

    public string Id { get; }

    public string Message { get; }
}
