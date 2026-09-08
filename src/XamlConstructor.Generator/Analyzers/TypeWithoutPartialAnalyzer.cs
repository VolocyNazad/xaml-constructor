using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using XamlConstructor.Generator.Extensions;

namespace XamlConstructor.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeWithoutPartialAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.TypeWithoutPartialRule,
        DiagnosticDescriptors.NestedTypeNotSupportedRule,
        DiagnosticDescriptors.TypeNameDoesNotEndWithViewModelRule,
        DiagnosticDescriptors.ConflictingConstructorRule,
        DiagnosticDescriptors.NoPrivateReadonlyFieldsRule);

    public override void Initialize(AnalysisContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        INamedTypeSymbol symbol = (INamedTypeSymbol)context.Symbol;

        if (symbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) is not TypeDeclarationSyntax typeDeclarationSyntax
            || !symbol.HasAttribute(Source.AttributeFullName))
        {
            return;
        }

        Location location = typeDeclarationSyntax.Identifier.GetLocation();

        if (!TypeDeclarationAnalysis.IsPartial(typeDeclarationSyntax))
        {
            Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeWithoutPartialRule, location);
            context.ReportDiagnostic(diagnostic);
        }

        if (!TypeDeclarationAnalysis.HasViewModelSuffix(typeDeclarationSyntax))
        {
            Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeNameDoesNotEndWithViewModelRule, location);
            context.ReportDiagnostic(diagnostic);
        }

        if (TypeDeclarationAnalysis.IsNestedType(typeDeclarationSyntax))
        {
            Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.NestedTypeNotSupportedRule, location);
            context.ReportDiagnostic(diagnostic);
        }

        if (TypeDeclarationAnalysis.HasParameterlessConstructor(typeDeclarationSyntax))
        {
            Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.ConflictingConstructorRule, location);
            context.ReportDiagnostic(diagnostic);
        }

        if (!TypeDeclarationAnalysis.FindReadonlyFields(typeDeclarationSyntax).Any())
        {
            Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.NoPrivateReadonlyFieldsRule, location);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
