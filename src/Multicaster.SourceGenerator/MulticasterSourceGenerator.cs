using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Cysharp.Runtime.Multicast.CodeGen;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cysharp.Runtime.Multicast.SourceGenerator;

/// <summary>Generates statically compiled Multicaster receiver proxies and factories.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class MulticasterSourceGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "Cysharp.Runtime.Multicast.MulticasterGenerationAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var anchors = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeMetadataName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (syntaxContext, cancellationToken) => CreateAnchor(syntaxContext, cancellationToken))
            .Where(static anchor => anchor != null)
            .Select(static (anchor, _) => anchor!);

        context.RegisterSourceOutput(anchors, static (productionContext, anchor) => Emit(productionContext, anchor));
    }

    private static AnchorDefinition? CreateAnchor(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var anchor = (INamedTypeSymbol)context.TargetSymbol;
        var attributes = anchor.GetAttributes().Where(x => x.AttributeClass?.ToDisplayString() == AttributeMetadataName).ToArray();
        // Attribute discovery runs for each annotated partial declaration. Emit only from the
        // declaration containing the first attribute, while collecting the entire symbol's set.
        var declaration = attributes[0].ApplicationSyntaxReference?.GetSyntax(cancellationToken).FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (declaration == null || declaration.SyntaxTree != context.TargetNode.SyntaxTree || declaration.Span != context.TargetNode.Span)
        {
            return null;
        }

        var location = context.TargetNode.GetLocation();
        var issues = new List<GeneratorIssue>();
        var receivers = new List<ReceiverDefinition>();

        if (anchor.IsFileLocal || anchor.ContainingType != null || anchor.TypeParameters.Length != 0 || !IsPartial(anchor, cancellationToken))
        {
            issues.Add(new GeneratorIssue("MCAST009", "The Multicaster generation anchor must be a non-generic, top-level partial class that is not file-local.", location));
        }

        var receiverSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var attribute in attributes)
        {
            if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is INamedTypeSymbol receiver)
            {
                receiverSymbols.Add(receiver);
            }
        }

        foreach (var receiver in receiverSymbols.OrderBy(x => x.ToDisplayString(), StringComparer.Ordinal))
        {
            var receiverLocation = receiver.Locations.FirstOrDefault() ?? location;
            ReceiverDefinition? definition;
            if (TryCreateReceiver(receiver, receiverLocation, issues, out definition))
            {
                receivers.Add(definition!);
            }
        }

        return new AnchorDefinition(anchor.ContainingNamespace.IsGlobalNamespace ? string.Empty : anchor.ContainingNamespace.ToDisplayString(), anchor.Name, receivers, issues);
    }

    private static bool TryCreateReceiver(INamedTypeSymbol receiver, Location location, List<GeneratorIssue> issues, out ReceiverDefinition? definition)
    {
        definition = null;
        if (receiver.TypeKind != TypeKind.Interface)
        {
            issues.Add(new GeneratorIssue("MCAST001", $"Receiver type '{receiver}' must be an interface.", location));
            return false;
        }

        if (receiver.IsUnboundGenericType || receiver.TypeArguments.Any(x => x.TypeKind == TypeKind.TypeParameter))
        {
            issues.Add(new GeneratorIssue("MCAST002", $"Receiver type '{receiver}' must be a closed interface.", location));
            return false;
        }

        if (!IsAccessible(receiver))
        {
            issues.Add(new GeneratorIssue("MCAST008", $"Receiver type '{receiver}' is not accessible to generated code.", location));
            return false;
        }

        var methods = new List<ReceiverMethodDefinition>();
        var visitedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var interfaces = receiver.AllInterfaces.OrderBy(x => x.ToDisplayString(TypeDisplayFormat), StringComparer.Ordinal).Concat(new[] { receiver });

        foreach (var currentInterface in interfaces)
        {
            foreach (var member in currentInterface.GetMembers().OrderBy(x => x.MetadataName, StringComparer.Ordinal))
            {
                // Non-public default implementations are interface helpers, not receiver contracts.
                if (member.DeclaredAccessibility != Accessibility.Public && !member.IsAbstract)
                {
                    continue;
                }

                if (member is IPropertySymbol || member is IEventSymbol)
                {
                    issues.Add(new GeneratorIssue(
                        "MCAST003",
                        $"Receiver '{receiver}' contains unsupported member '{member.Name}'. Properties and events are not supported.",
                        member.Locations.FirstOrDefault() ?? location));
                    continue;
                }

                var method = member as IMethodSymbol;
                if (method == null || method.MethodKind != MethodKind.Ordinary)
                {
                    continue;
                }

                if (!visitedMethods.Add(method))
                {
                    continue;
                }

                if (method.IsStatic || method.TypeParameters.Length != 0 || method.Parameters.Any(x => x.RefKind != RefKind.None))
                {
                    issues.Add(new GeneratorIssue(
                        "MCAST003",
                        $"Receiver method '{method.Name}' is unsupported. Static methods, generic methods, and ref/out/in parameters are not supported.",
                        method.Locations.FirstOrDefault() ?? location));
                    continue;
                }

                if (method.Parameters.Length > 15)
                {
                    issues.Add(new GeneratorIssue(
                        "MCAST004",
                        $"Receiver method '{method.Name}' has {method.Parameters.Length} parameters; at most 15 are supported.",
                        method.Locations.FirstOrDefault() ?? location));
                    continue;
                }

                ReceiverReturnKind returnKind;
                string? resultTypeName = null;
                if (method.ReturnsVoid)
                {
                    returnKind = ReceiverReturnKind.Void;
                }
                else if (IsTask(method.ReturnType))
                {
                    returnKind = ReceiverReturnKind.Task;
                }
                else if (method.ReturnType is INamedTypeSymbol namedReturn && IsGenericTask(namedReturn))
                {
                    returnKind = ReceiverReturnKind.TaskOfT;
                    resultTypeName = GetTypeName(namedReturn.TypeArguments[0]);
                }
                else
                {
                    issues.Add(new GeneratorIssue("MCAST005", $"Receiver method '{method.Name}' must return void, Task, or Task<T>.", method.Locations.FirstOrDefault() ?? location));
                    continue;
                }

                var parameters = method.Parameters.Select(x => new ReceiverParameterDefinition(GetTypeName(x.Type), x.Name, IsCancellationToken(x.Type))).ToArray();
                var methodDefinition = new ReceiverMethodDefinition(
                    method.Name,
                    GetTypeName(method.ReturnType),
                    returnKind,
                    resultTypeName,
                    GetMethodId(method),
                    parameters,
                    GetTypeName(currentInterface));
                methods.Add(methodDefinition);
            }
        }

        var typeName = GetTypeName(receiver);
        var hash = unchecked((uint)MethodIdCalculator.GetMethodId(typeName)).ToString("X8");
        definition = new ReceiverDefinition(typeName, "__MulticasterProxy_" + hash, methods);
        foreach (var diagnostic in ReceiverValidator.Validate(definition))
        {
            issues.Add(new GeneratorIssue(diagnostic.Id, diagnostic.Message, location));
        }

        return true;
    }

    private static int GetMethodId(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var name = attribute.AttributeClass?.Name;
            if (name != "MethodId" && name != "MethodIdAttribute")
            {
                continue;
            }

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "MethodId" && namedArgument.Value.Value is int namedValue)
                {
                    return namedValue;
                }
            }

            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is int constructorValue)
            {
                return constructorValue;
            }
        }

        return MethodIdCalculator.GetMethodId(method.Name);
    }

    private static bool IsPartial(INamedTypeSymbol anchor, CancellationToken cancellationToken)
        => anchor.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax(cancellationToken) is ClassDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static bool IsAccessible(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public && current.DeclaredAccessibility != Accessibility.Internal)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsTask(ITypeSymbol type)
        => type is INamedTypeSymbol named && named.Arity == 0 && named.Name == "Task" && named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";

    private static bool IsGenericTask(INamedTypeSymbol type)
        => type.Arity == 1 && type.TypeArguments.Length == 1 && type.Name == "Task" && type.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";

    private static bool IsCancellationToken(ITypeSymbol type) => type.Name == "CancellationToken" && type.ContainingNamespace.ToDisplayString() == "System.Threading";

    private static void Emit(SourceProductionContext context, AnchorDefinition anchor)
    {
        foreach (var issue in anchor.Issues)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(issue.Id, "Invalid Multicaster receiver", issue.Message, "Multicaster.SourceGenerator", DiagnosticSeverity.Error, true),
                issue.Location));
        }

        if (anchor.Issues.Count != 0 || anchor.Receivers.Count == 0)
        {
            return;
        }

        var source = ProxyEmitter.Emit(anchor.NamespaceName, anchor.Name, anchor.Receivers);
        var hintName = string.IsNullOrEmpty(anchor.NamespaceName) ? anchor.Name : anchor.NamespaceName + "." + anchor.Name;
        context.AddSource(hintName + ".Multicaster.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    // Dynamic is object in the runtime contract. Normalize it recursively, including generic
    // arguments, to avoid emitting dynamic dispatch or rooting the runtime binder in NativeAOT.
    private static string GetTypeName(ITypeSymbol type)
        => string.Concat(type.ToDisplayParts(TypeDisplayFormat).Select(part => part.Kind == SymbolDisplayPartKind.Keyword && part.ToString() == "dynamic" ? "object" : part.ToString()));

    private static readonly SymbolDisplayFormat TypeDisplayFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                              SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private sealed class AnchorDefinition
    {
        public AnchorDefinition(string namespaceName, string name, IReadOnlyList<ReceiverDefinition> receivers, IReadOnlyList<GeneratorIssue> issues)
        {
            NamespaceName = namespaceName;
            Name = name;
            Receivers = receivers;
            Issues = issues;
        }

        public string NamespaceName { get; }

        public string Name { get; }

        public IReadOnlyList<ReceiverDefinition> Receivers { get; }

        public IReadOnlyList<GeneratorIssue> Issues { get; }
    }

    private sealed class GeneratorIssue
    {
        public GeneratorIssue(string id, string message, Location location)
        {
            Id = id;
            Message = message;
            Location = location;
        }

        public string Id { get; }

        public string Message { get; }

        public Location Location { get; }
    }
}
