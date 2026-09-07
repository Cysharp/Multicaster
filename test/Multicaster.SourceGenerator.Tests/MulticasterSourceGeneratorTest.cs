using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.SourceGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Multicaster.SourceGenerator.Tests;

public class MulticasterSourceGeneratorTest
{
    [Fact]
    public void GeneratesInMemoryRemoteAndFactorySources()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Cysharp.Runtime.Multicast;

            namespace GeneratorFixture;

            [MulticasterGeneration(typeof(IReceiver))]
            public partial class GeneratedMulticaster
            {
            }

            public interface IReceiver
            {
                void Notify(int value);
                Task<string> QueryAsync(int value, CancellationToken cancellationToken);
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error));
        var generated = Assert.Single(result.Results).GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("InMemoryProxy<TKey>", generated);
        Assert.Contains("RemoteProxy", generated);
        Assert.Contains("IInMemoryProxyFactory", generated);
        Assert.Contains("IRemoteProxyFactory", generated);
        Assert.Contains("public bool TryCreate<TKey, TReceiver>", generated);
        Assert.Contains("InvokeWithResult<int, string>", generated);
    }

    [Fact]
    public void ReportsUnsupportedProperty()
    {
        const string source = """
            using Cysharp.Runtime.Multicast;

            [MulticasterGeneration(typeof(IReceiver))]
            public partial class GeneratedMulticaster
            {
            }

            public interface IReceiver
            {
                string Name { get; }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, x => x.Id == "MCAST003");
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
    }

    [Fact]
    public void ReportsMethodIdCollision()
    {
        const string source = """
            using System;
            using Cysharp.Runtime.Multicast;

            [MulticasterGeneration(typeof(IReceiver))]
            public partial class GeneratedMulticaster
            {
            }

            public interface IReceiver
            {
                [MethodId(42)] void First();
                [MethodId(42)] void Second();
            }

            public sealed class MethodIdAttribute : Attribute
            {
                public MethodIdAttribute(int methodId) => MethodId = methodId;
                public int MethodId { get; }
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, x => x.Id == "MCAST007");
    }

    [Fact]
    public void ReportsOpenGenericReceiver()
    {
        const string source = """
            using Cysharp.Runtime.Multicast;

            [MulticasterGeneration(typeof(IReceiver<>))]
            public partial class GeneratedMulticaster
            {
            }

            public interface IReceiver<T>
            {
                void Receive(T value);
            }
            """;

        var result = RunGenerator(source);

        Assert.Contains(result.Diagnostics, x => x.Id == "MCAST002");
    }

    [Fact]
    public void SameNamedAnchorsInDifferentNamespacesCompile()
    {
        var result = RunGenerator("""
            using Cysharp.Runtime.Multicast;
            public interface IReceiver { void Notify(); }
            namespace A { [MulticasterGeneration(typeof(IReceiver))] public partial class Anchor {} }
            namespace B { [MulticasterGeneration(typeof(IReceiver))] public partial class Anchor {} }
            public class User
            {
                public object First => A.Anchor.InMemoryProxyFactory;
                public object Second => B.Anchor.RemoteProxyFactory;
            }
            """);

        Assert.Equal(2, Assert.Single(result.Results).GeneratedSources.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttributesOnPartialDeclarationsProduceOneCompleteFactory(bool separateFiles)
    {
        const string first = """
            [Cysharp.Runtime.Multicast.MulticasterGeneration(typeof(IA))]
            public partial class Anchor {}
            public interface IA { void First(); }
            """;
        const string second = """
            [Cysharp.Runtime.Multicast.MulticasterGeneration(typeof(IB))]
            public partial class Anchor {}
            public interface IB { void Second(); }
            """;

        var result = RunGenerator(separateFiles ? [first, second] : [first + second]);
        var source = Assert.Single(Assert.Single(result.Results).GeneratedSources).SourceText.ToString();
        Assert.Contains("typeof(global::IA)", source);
        Assert.Contains("typeof(global::IB)", source);
    }

    [Fact]
    public void OverlappingReceiverSetsCompile()
    {
        var result = RunGenerator("""
            using Cysharp.Runtime.Multicast;
            public interface IReceiver { void Notify(); }
            [MulticasterGeneration(typeof(IReceiver))] public partial class FirstAnchor {}
            [MulticasterGeneration(typeof(IReceiver))] public partial class SecondAnchor {}
            """);

        Assert.Equal(2, Assert.Single(result.Results).GeneratedSources.Length);
    }

    [Theory]
    [InlineData("void TKey();")]
    [InlineData("void Notify(dynamic value);")]
    [InlineData("Task<dynamic> Query(List<dynamic> value);")]
    [InlineData("void Notify(); void Invoke(Action<IReceiver> action);")]
    [InlineData("void Notify(Action Invoke);")]
    [InlineData("Task<int> Query(int InvokeWithResult);")]
    [InlineData("void Notify(); private void Helper() {}")]
    [InlineData("void @event(int @class);")]
    public void SupportedMemberShapesCompile(string members)
    {
        var result = RunGenerator($$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Cysharp.Runtime.Multicast;
            [MulticasterGeneration(typeof(IReceiver))] public partial class Anchor {}
            public interface IReceiver { {{members}} }
            """);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(Assert.Single(result.Results).GeneratedSources).SourceText.ToString();
        Assert.DoesNotContain("dynamic", generated);
        Assert.DoesNotContain("Helper", generated);
    }

    [Fact]
    public void InheritedMethodsWithDifferentReturnTypesCompile()
    {
        var result = RunGenerator("""
            using System;
            using System.Threading.Tasks;
            using Cysharp.Runtime.Multicast;
            public class MethodIdAttribute(int id) : Attribute { public int MethodId => id; }
            public interface IA { [MethodId(1)] Task<int> Query(); }
            public interface IB { [MethodId(2)] Task<string> Query(); }
            public interface IReceiver : IA, IB {}
            [MulticasterGeneration(typeof(IReceiver))] public partial class Anchor {}
            """);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void DiamondInheritanceVisitsTheBaseMethodOnce()
    {
        var result = RunGenerator("""
            using Cysharp.Runtime.Multicast;
            public interface IBase { void Notify(); }
            public interface IA : IBase {}
            public interface IB : IBase {}
            public interface IReceiver : IA, IB {}
            [MulticasterGeneration(typeof(IReceiver))] public partial class Anchor {}
            """);

        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("public int MethodId { get; set; } = id;")]
    [InlineData("public int MethodId = id;")]
    public void NamedMethodIdOverridesConstructorValue(string member)
    {
        var result = RunGenerator($$"""
            using System;
            using Cysharp.Runtime.Multicast;
            public class MethodIdAttribute(int id) : Attribute { {{member}} }
            public interface IReceiver { [MethodId(1, MethodId = 42)] void Notify(); }
            [MulticasterGeneration(typeof(IReceiver))] public partial class Anchor {}
            """);

        var generated = Assert.Single(Assert.Single(result.Results).GeneratedSources).SourceText.ToString();
        Assert.Contains("base.Invoke(\"Notify\", 42)", generated);
    }

    [Fact]
    public void ReportsFileLocalAnchor()
    {
        var result = RunGenerator("""
            using Cysharp.Runtime.Multicast;
            public interface IReceiver { void Notify(); }
            [MulticasterGeneration(typeof(IReceiver))] file partial class Anchor {}
            """);

        Assert.Contains(result.Diagnostics, x => x.Id == "MCAST009");
        Assert.Empty(Assert.Single(result.Results).GeneratedSources);
    }

    private static GeneratorDriverRunResult RunGenerator(params string[] sources)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTrees = sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, $"Input{index}.cs"));
        var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path)).ToList();
        trustedPlatformAssemblies.Add(MetadataReference.CreateFromFile(typeof(MulticasterGenerationAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create("GeneratorFixture", syntaxTrees, trustedPlatformAssemblies, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new MulticasterSourceGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var result = driver.GetRunResult();
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Assert.Empty(output.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error));
        using var stream = new MemoryStream();
        var emitResult = output.Emit(stream);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
        return result;
    }
}
