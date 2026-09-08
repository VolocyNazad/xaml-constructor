using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using XamlConstructor.Generator;
using XamlConstructor.Generator.Analyzers;
using Xunit;

namespace XamlConstructor.Tests;

public sealed class TypeWithoutPartialAnalyzerTests
{
    [Theory]
    [MemberData(nameof(DiagnosticTestCases.ForAnalyzer), MemberType = typeof(DiagnosticTestCases))]
    public async Task ReportsExactlyOneDiagnostic_ForEachSingleIssueScenario(string source, string expectedDiagnosticId)
    {
        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(expectedDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportDiagnostic_WhenAttributedTypeIsPartial()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public partial class PartialViewModel
            {
                private readonly string _name;
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReportDiagnostic_ForTypesWithoutTheAttribute()
    {
        const string source = """
            namespace Test;

            public class PlainType
            {
                private readonly string _name;
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsAllApplicableDiagnostics_ForATypeWithSeveralIssuesAtOnce()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public class Broken
            {
                public Broken()
                {
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        IEnumerable<string> ids = diagnostics.Select(d => d.Id).Order();
        string[] expectedIds =
        [
            DiagnosticDescriptors.TypeWithoutPartialDiagnosticId,
            DiagnosticDescriptors.NoPrivateReadonlyFieldsDiagnosticId,
            DiagnosticDescriptors.TypeNameDoesNotEndWithViewModelDiagnosticId,
            DiagnosticDescriptors.ConflictingConstructorDiagnosticId,
        ];

        Assert.Equal(expectedIds.Order(), ids);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(string source)
    {
        CSharpCompilation compilation = CompilationFactory.CreateCompilation(
            "AnalyzerTests",
            source,
            CompilationFactory.MinimalAttributeSource);

        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            [new TypeWithoutPartialAnalyzer()]);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
