using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace XamlConstructor.Generator.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class XamlConstructorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(i => i.AddSource(Source.AttributeFullName, SourceText.From(Source.AttributeText, Encoding.UTF8)));

        IncrementalValuesProvider<(TypeDeclarationSyntax TypeDeclaration, Compilation Compilation)> typeDeclarations =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    Source.AttributeFullName,
                    predicate: static (node, _) => IsSyntaxTargetForGeneration(node),
                    transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode)
                .Where(static i => i is not null)
                .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(typeDeclarations, static (context, tuple) =>
        {
            try
            {
                (TypeDeclarationSyntax typeDeclaration, Compilation _) = tuple;

                if (!TypeDeclarationAnalysis.IsPartial(typeDeclaration))
                {
                    Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeWithoutPartialRule, typeDeclaration.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                if (!TypeDeclarationAnalysis.HasViewModelSuffix(typeDeclaration))
                {
                    Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.TypeNameDoesNotEndWithViewModelRule, typeDeclaration.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                if (TypeDeclarationAnalysis.IsNestedType(typeDeclaration))
                {
                    Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.NestedTypeNotSupportedRule, typeDeclaration.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                if (TypeDeclarationAnalysis.HasParameterlessConstructor(typeDeclaration))
                {
                    Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.ConflictingConstructorRule, typeDeclaration.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                List<FieldDeclarationSyntax> readonlyFields = [.. TypeDeclarationAnalysis.FindReadonlyFields(typeDeclaration)];

                if (readonlyFields.Count == 0)
                {
                    Diagnostic diagnostic = Diagnostic.Create(DiagnosticDescriptors.NoPrivateReadonlyFieldsRule, typeDeclaration.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }

                string source = GenerateConstructor(typeDeclaration, readonlyFields);
                string fileName = GenerateUniqueFileName(typeDeclaration);
                context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ConstructorCreationFailedRule,
                    tuple.TypeDeclaration.GetLocation(),
                    tuple.TypeDeclaration.Identifier.Text,
                    ex.Message);
                context.ReportDiagnostic(diagnostic);
            }
        });
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private static string GenerateUniqueFileName(TypeDeclarationSyntax typeDeclaration)
    {
        string className = typeDeclaration.Identifier.Text;

        // Append generic type parameters to the file name, if any
        if (typeDeclaration.TypeParameterList != null)
        {
            IEnumerable<string> parameters = typeDeclaration.TypeParameterList.Parameters
                .Select(p => p.Identifier.Text);
            string genericSuffix = "_" + string.Join("_", parameters);
            return $"{className}{genericSuffix}.XamlConstructor.g.cs";
        }

        return $"{className}.XamlConstructor.g.cs";
    }

    private static string GenerateConstructor(TypeDeclarationSyntax typeDeclaration, List<FieldDeclarationSyntax> readonlyFields)
    {
        string className = typeDeclaration.Identifier.Text;
        string? namespaceName = GetNamespace(typeDeclaration);
        string typeDeclarationWithGenerics = GetTypeDeclarationWithGenerics(typeDeclaration);

        string fieldInitializations = GenerateFieldInitializations(readonlyFields);

        return $$"""
        // <auto-generated>
        //     This code was generated by the XamlConstructor source generator.
        //
        //     Changes to this file may cause incorrect behavior and will be lost if
        //     the code is regenerated.
        // </auto-generated>
        
        {{(namespaceName is not null ? $"namespace {namespaceName};" : "")}}
        
        partial class {{typeDeclarationWithGenerics}}
        {
            /// <summary>
            /// XAML Design Mode constructor - all readonly fields will be null!
            /// </summary>
            [Obsolete("This constructor should only be used by XAML markup, and only in DesignMode.")]
            public {{className}}()
            {
        {{fieldInitializations}}
            }
        }
        """;
    }

    private static string GetTypeDeclarationWithGenerics(TypeDeclarationSyntax typeDeclaration)
    {
        string className = typeDeclaration.Identifier.Text;

        TypeParameterListSyntax? typeParameterList = typeDeclaration.TypeParameterList;
        if (typeParameterList == null)
        {
            return className;
        }

        IEnumerable<string> parameters = typeParameterList.Parameters
            .Select(p => p.Identifier.Text);

        string genericParams = string.Join(", ", parameters);
        return $"{className}<{genericParams}>";
    }

    private static string GenerateFieldInitializations(List<FieldDeclarationSyntax> fields)
    {
        const string indent = "        ";

        if (fields.Count == 0)
        {
            return $"{indent}// No readonly fields found";
        }

        StringBuilder stringBuilder = new();

        foreach (FieldDeclarationSyntax field in fields)
        {
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                string fieldName = variable.Identifier.Text;
                stringBuilder.AppendLine($"{indent}this.{fieldName} = null!;");
            }
        }

        return stringBuilder.ToString().TrimEnd();
    }

    private static string? GetNamespace(TypeDeclarationSyntax typeDeclaration)
    {
        SyntaxNode? potentialNamespaceParent = typeDeclaration.Parent;

        while (potentialNamespaceParent != null)
        {
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                return namespaceDeclaration.Name.ToString();
            }

            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        return null;
    }
}
