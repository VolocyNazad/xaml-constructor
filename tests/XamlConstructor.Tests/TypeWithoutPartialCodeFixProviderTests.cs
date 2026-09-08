using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using XamlConstructor.CodeFixes;
using XamlConstructor.Generator.Analyzers;
using Xunit;

namespace XamlConstructor.Tests;

public sealed class TypeWithoutPartialCodeFixProviderTests
{
    [Fact]
    public async Task AddsThePartialModifier_ToTheDiagnosedType()
    {
        const string source = """
            namespace Test;

            [XamlConstructor]
            public class NotPartialViewModel
            {
                private readonly string _name;
            }
            """;

        string fixedSource = await ApplyCodeFixAsync(source);

        Assert.Contains("public partial class NotPartialViewModel", fixedSource, StringComparison.Ordinal);
    }

    private static async Task<string> ApplyCodeFixAsync(string source)
    {
        using AdhocWorkspace workspace = new();

        ProjectId projectId = ProjectId.CreateNewId();
        DocumentId documentId = DocumentId.CreateNewId(projectId);
        DocumentId attributeDocumentId = DocumentId.CreateNewId(projectId);

        Solution solution = workspace.CurrentSolution
            .AddProject(projectId, "CodeFixTests", "CodeFixTests", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, CompilationFactory.References)
            .AddDocument(documentId, "Test.cs", SourceText.From(source))
            .AddDocument(attributeDocumentId, "XamlConstructorAttribute.cs", SourceText.From(CompilationFactory.MinimalAttributeSource));

        Document document = solution.GetDocument(documentId)
            ?? throw new InvalidOperationException("Document was not added to the workspace.");

        Compilation? projectCompilation = await document.Project.GetCompilationAsync();
        CompilationWithAnalyzers compilationWithAnalyzers = projectCompilation!.WithAnalyzers(
            [new TypeWithoutPartialAnalyzer()]);

        ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        Diagnostic diagnostic = Assert.Single(diagnostics);

        List<CodeAction> registeredActions = [];
        CodeFixContext context = new(
            document,
            diagnostic,
            (action, _) => registeredActions.Add(action),
            CancellationToken.None);

        await new TypeWithoutPartialCodeFixProvider().RegisterCodeFixesAsync(context);

        CodeAction codeAction = Assert.Single(registeredActions);
        ImmutableArray<CodeActionOperation> operations = await codeAction.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation applyChangesOperation = Assert.IsType<ApplyChangesOperation>(Assert.Single(operations));

        applyChangesOperation.Apply(workspace, CancellationToken.None);

        Document fixedDocument = applyChangesOperation.ChangedSolution.GetDocument(documentId)
            ?? throw new InvalidOperationException("Fixed document was not found.");

        SourceText fixedText = await fixedDocument.GetTextAsync();
        return fixedText.ToString();
    }
}
