using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XamlConstructor.Generator;
using XamlConstructor.Generator.Generators;
using Xunit;

namespace XamlConstructor.Tests;

public sealed class XamlConstructorGeneratorTests
{
    [Fact]
    public void GeneratesConstructor_ForPrivateAndProtectedReadonlyFields()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public partial class MyViewModel
            {
                protected readonly object ServiceProvider;
                private readonly string _name;
            }
            """;

        string generated = RunGenerator(source);

        Assert.Contains("public MyViewModel()", generated, StringComparison.Ordinal);
        Assert.Contains("this.ServiceProvider = null!;", generated, StringComparison.Ordinal);
        Assert.Contains("this._name = null!;", generated, StringComparison.Ordinal);
        Assert.Contains("namespace Test;", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratesPlaceholderComment_AndReportsWarning_WhenNoReadonlyFieldsAreFound()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public partial class EmptyViewModel
            {
                private string _name = "";
            }
            """;

        GeneratorRunResult result = RunGeneratorForResult(source).Results[0];

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticDescriptors.NoPrivateReadonlyFieldsDiagnosticId, diagnostic.Id);

        GeneratedSourceResult generatedSource = Assert.Single(
            result.GeneratedSources,
            r => r.HintName.EndsWith(".XamlConstructor.g.cs", StringComparison.Ordinal));

        Assert.Contains("// No readonly fields found", generatedSource.SourceText.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void IgnoresReadonlyFields_ThatAlreadyHaveAnInitializer()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public partial class ViewModel
            {
                private readonly int _initialized = 5;
                private readonly string _uninitialized;
            }
            """;

        string generated = RunGenerator(source);

        Assert.DoesNotContain("_initialized", generated, StringComparison.Ordinal);
        Assert.Contains("this._uninitialized = null!;", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void IgnoresReadonlyFields_ThatArePublic()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public partial class ViewModel
            {
                public readonly int PublicField;
                private readonly string _name;
            }
            """;

        string generated = RunGenerator(source);

        Assert.DoesNotContain("PublicField", generated, StringComparison.Ordinal);
        Assert.Contains("this._name = null!;", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludesGenericTypeParameters_InTheGeneratedDeclarationAndFileName()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public partial class GenericViewModel<T>
            {
                private readonly int _value;
            }
            """;

        GeneratorRunResult result = RunGeneratorForResult(source).Results[0];

        GeneratedSourceResult generatedSource = Assert.Single(
            result.GeneratedSources,
            r => r.HintName.StartsWith("GenericViewModel_T", StringComparison.Ordinal));

        Assert.Contains("partial class GenericViewModel<T>", generatedSource.SourceText.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(DiagnosticTestCases.ForGenerator), MemberType = typeof(DiagnosticTestCases))]
    public void ReportsDiagnostic_AndDoesNotGenerate_ForEachBlockingScenario(string source, string expectedDiagnosticId)
    {
        GeneratorRunResult result = RunGeneratorForResult(source).Results[0];

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedDiagnosticId, diagnostic.Id);
        Assert.DoesNotContain(
            result.GeneratedSources,
            r => r.HintName.EndsWith(".XamlConstructor.g.cs", StringComparison.Ordinal));
    }

    private static string RunGenerator(string source)
    {
        GeneratorRunResult result = RunGeneratorForResult(source).Results[0];

        GeneratedSourceResult generatedSource = Assert.Single(
            result.GeneratedSources,
            r => r.HintName.EndsWith(".XamlConstructor.g.cs", StringComparison.Ordinal));

        return generatedSource.SourceText.ToString();
    }

    private static GeneratorDriverRunResult RunGeneratorForResult(string source)
    {
        CSharpCompilation compilation = CompilationFactory.CreateCompilation("GeneratorTests", source);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new XamlConstructorGenerator());
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult();
    }
}
