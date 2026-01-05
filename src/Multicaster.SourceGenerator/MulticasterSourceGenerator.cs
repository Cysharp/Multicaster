using Cysharp.Runtime.Multicast.SourceGenerator.CodeAnalysis;
using Cysharp.Runtime.Multicast.SourceGenerator.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cysharp.Runtime.Multicast.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class MulticasterSourceGenerator : IIncrementalGenerator
{
    const string MulticasterProxyGenerationAttributeFullName = "Cysharp.Runtime.Multicast.MulticasterProxyGenerationAttribute";
    const string MulticasterProxyGenerationAttributeName = "MulticasterProxyGenerationAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find classes with [MulticasterProxyGeneration] attribute
        var generationAttr = context.SyntaxProvider.ForAttributeWithMetadataName(
            MulticasterProxyGenerationAttributeFullName,
            predicate: static (node, cancellationToken) => node is ClassDeclarationSyntax,
            transform: static (ctx, cancellationToken) =>
                ((ClassDeclarationSyntax)ctx.TargetNode, ctx.Attributes, ctx.SemanticModel));

        context.RegisterSourceOutput(generationAttr, (sourceProductionContext, value) =>
        {
            var (classDecl, attrs, semanticModel) = value;

            var attr = attrs.FirstOrDefault(x => x.AttributeClass?.Name == MulticasterProxyGenerationAttributeName);
            if (attr is null) return;

            // Validate partial class
            if (!classDecl.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.GenerationAttributeRequiresPartialClass,
                    classDecl.GetLocation(),
                    classDecl.Identifier.Text));
                return;
            }

            // Get namespace and class name
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is null) return;

            var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            // Get receiver interface types from attribute
            var receiverTypes = GetReceiverInterfaceTypes(attr);
            if (receiverTypes.Count == 0)
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NoReceiverInterfacesSpecified,
                    classDecl.GetLocation()));
                return;
            }

            // Collect receiver interface information
            var (receivers, diagnostics) = ReceiverInterfaceCollector.Collect(
                receiverTypes,
                sourceProductionContext.CancellationToken);

            // Report diagnostics
            foreach (var diagnostic in diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (receivers.Count == 0)
            {
                return;
            }

            // Generate code
            var generatedCode = ProxyFactoryGenerator.Generate(
                namespaceName,
                className,
                receivers);

            sourceProductionContext.AddSource($"{className}.g.cs", generatedCode);
        });
    }

    static List<INamedTypeSymbol> GetReceiverInterfaceTypes(AttributeData attr)
    {
        var types = new List<INamedTypeSymbol>();

        if (attr.ConstructorArguments.Length > 0)
        {
            var arg = attr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle params array: [MulticasterProxyGeneration(typeof(T1), typeof(T2))]
                foreach (var item in arg.Values)
                {
                    if (item.Value is INamedTypeSymbol typeSymbol)
                    {
                        types.Add(typeSymbol);
                    }
                }
            }
            else if (arg.Kind == TypedConstantKind.Type && arg.Value is INamedTypeSymbol singleType)
            {
                // Handle single type: [MulticasterProxyGeneration(typeof(T))]
                types.Add(singleType);
            }
        }

        return types;
    }
}
