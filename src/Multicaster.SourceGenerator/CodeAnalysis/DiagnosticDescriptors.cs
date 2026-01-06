using Microsoft.CodeAnalysis;

namespace Cysharp.Runtime.Multicast.SourceGenerator.CodeAnalysis;

public static class DiagnosticDescriptors
{
    const string Category = "Multicaster";

    public static readonly DiagnosticDescriptor TypeMustBeInterface = new(
        id: "MULT001",
        title: "Type must be an interface",
        messageFormat: "The type '{0}' must be an interface for proxy generation",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor VoidMethodCannotHaveCancellationToken = new(
        id: "MULT002",
        title: "Void method cannot have CancellationToken",
        messageFormat: "The void method '{0}' has a CancellationToken parameter which is only used for client results",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor GenerationAttributeRequiresPartialClass = new(
        id: "MULT003",
        title: "Generation attribute requires partial class",
        messageFormat: "The class '{0}' with [MulticasterProxyGeneration] attribute must be declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor NoReceiverInterfacesFound = new(
        id: "MULT004",
        title: "No receiver interfaces found",
        messageFormat: "No receiver interfaces were specified or found for proxy generation",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor NoReceiverInterfacesSpecified = new(
        id: "MULT006",
        title: "No receiver interfaces specified",
        messageFormat: "The [MulticasterProxyGeneration] attribute must specify at least one receiver interface type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TooManyParameters = new(
        id: "MULT005",
        title: "Too many parameters",
        messageFormat: "The method '{0}' has {1} parameters, but the maximum supported is 15",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
