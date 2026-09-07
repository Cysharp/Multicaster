using System.Collections.Generic;
using System.Linq;

namespace Cysharp.Runtime.Multicast.CodeGen;

internal static class ReceiverValidator
{
    public static IReadOnlyList<CodeGenerationDiagnostic> Validate(ReceiverDefinition receiver)
    {
        var diagnostics = new List<CodeGenerationDiagnostic>();
        var methodIds = new Dictionary<int, string>();

        foreach (var method in receiver.Methods)
        {
            if (method.Parameters.Count > 15)
            {
                diagnostics.Add(new CodeGenerationDiagnostic("MCAST004", $"Receiver method '{method.Name}' has {method.Parameters.Count} parameters; at most 15 are supported."));
            }

            if (method.ReturnKind != ReceiverReturnKind.Void && method.Parameters.Count(x => x.IsCancellationToken) > 1)
            {
                diagnostics.Add(new CodeGenerationDiagnostic("MCAST006", $"Client-result receiver method '{method.Name}' has more than one CancellationToken parameter."));
            }

            string? existing;
            if (methodIds.TryGetValue(method.MethodId, out existing))
            {
                diagnostics.Add(new CodeGenerationDiagnostic("MCAST007", $"Receiver methods '{existing}' and '{method.Name}' use the same method ID {method.MethodId}."));
            }
            else
            {
                methodIds[method.MethodId] = method.Name;
            }
        }

        return diagnostics;
    }
}
