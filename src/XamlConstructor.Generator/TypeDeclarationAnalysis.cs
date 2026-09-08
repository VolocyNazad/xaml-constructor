using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XamlConstructor.Generator;

/// <summary>
/// Syntax-level checks shared by <see cref="Analyzers.TypeWithoutPartialAnalyzer"/> and
/// <see cref="Generators.XamlConstructorGenerator"/>, so the rules for what a
/// <c>[XamlConstructor]</c>-attributed type must look like are defined once.
/// </summary>
internal static class TypeDeclarationAnalysis
{
    public static bool IsPartial(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    public static bool HasViewModelSuffix(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Identifier.Text.EndsWith("ViewModel");
    }

    public static bool IsNestedType(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Parent is TypeDeclarationSyntax;
    }

    public static bool HasParameterlessConstructor(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .Any(c => c.ParameterList.Parameters.Count == 0);
    }

    public static IEnumerable<FieldDeclarationSyntax> FindReadonlyFields(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Members
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
                && (f.Modifiers.Any(SyntaxKind.PrivateKeyword) || f.Modifiers.Any(SyntaxKind.ProtectedKeyword))
                && !f.Declaration.Variables.Any(v => v.Initializer != null));
    }
}
