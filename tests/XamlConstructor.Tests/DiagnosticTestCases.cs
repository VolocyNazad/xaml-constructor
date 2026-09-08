using XamlConstructor.Generator;
using Xunit;

namespace XamlConstructor.Tests;

/// <summary>
/// Source snippets that should each trigger exactly one XamlConstructor diagnostic - shared between
/// <see cref="TypeWithoutPartialAnalyzerTests"/> and <see cref="XamlConstructorGeneratorTests"/>, since both
/// surfaces report the same rules (see <c>TypeDeclarationAnalysis</c>) for the same reasons.
/// </summary>
internal static class DiagnosticTestCases
{
    private static readonly (string Source, string DiagnosticId, bool BlocksGeneration)[] SingleIssueScenarios =
    [
        (
            """
            namespace Test;

            [XamlConstructor]
            public class NotPartialViewModel
            {
                private readonly string _name;
            }
            """,
            DiagnosticDescriptors.TypeWithoutPartialDiagnosticId,
            true),
        (
            """
            namespace Test;

            [XamlConstructor]
            public partial class MyService
            {
                private readonly string _name;
            }
            """,
            DiagnosticDescriptors.TypeNameDoesNotEndWithViewModelDiagnosticId,
            true),
        (
            """
            namespace Test;

            public partial class ContainerViewModel
            {
                [XamlConstructor]
                public partial class NestedViewModel
                {
                    private readonly string _name;
                }
            }
            """,
            DiagnosticDescriptors.NestedTypeNotSupportedDiagnosticId,
            true),
        (
            """
            namespace Test;

            [XamlConstructor]
            public partial class ConflictingViewModel
            {
                private readonly string _name;

                public ConflictingViewModel()
                {
                    _name = "";
                }
            }
            """,
            DiagnosticDescriptors.ConflictingConstructorDiagnosticId,
            true),
        (
            """
            namespace Test;

            [XamlConstructor]
            public partial class EmptyViewModel
            {
                private string _name = "";
            }
            """,
            DiagnosticDescriptors.NoPrivateReadonlyFieldsDiagnosticId,
            false),
    ];

    /// <summary>All scenarios - the analyzer reports every applicable rule regardless of severity.</summary>
    public static TheoryData<string, string> ForAnalyzer => ToTheoryData(SingleIssueScenarios);

    /// <summary>Only the scenarios that stop the generator from emitting a constructor.</summary>
    public static TheoryData<string, string> ForGenerator => ToTheoryData(SingleIssueScenarios.Where(s => s.BlocksGeneration));

    private static TheoryData<string, string> ToTheoryData(IEnumerable<(string Source, string DiagnosticId, bool BlocksGeneration)> scenarios)
    {
        TheoryData<string, string> data = new();

        foreach ((string source, string diagnosticId, bool _) in scenarios)
        {
            data.Add(source, diagnosticId);
        }

        return data;
    }
}
