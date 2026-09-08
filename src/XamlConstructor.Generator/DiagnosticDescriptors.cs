using Microsoft.CodeAnalysis;

namespace XamlConstructor.Generator;

public static class DiagnosticDescriptors
{
    public const string TypeWithoutPartialDiagnosticId = "XCONS01";
    public const string ConstructorCreationFailedDiagnosticId = "XCONS02";
    public const string NoPrivateReadonlyFieldsDiagnosticId = "XCONS03";
    public const string NestedTypeNotSupportedDiagnosticId = "XCONS04";
    public const string TypeNameDoesNotEndWithViewModelDiagnosticId = "XCONS05";
    public const string ConflictingConstructorDiagnosticId = "XCONS06";

    public static readonly DiagnosticDescriptor TypeWithoutPartialRule = CreateRule(
        TypeWithoutPartialDiagnosticId,
        "Couldn't generate constructor",
        $"Type decorated with {Source.AttributeFullName} must be also declared partial");

    public static readonly DiagnosticDescriptor ConstructorCreationFailedRule = CreateRule(
        ConstructorCreationFailedDiagnosticId,
        "Constructor generation failed",
        "Failed to generate constructor for type '{0}': {1}",
        DiagnosticSeverity.Error);

    public static readonly DiagnosticDescriptor NestedTypeNotSupportedRule = CreateRule(
        NestedTypeNotSupportedDiagnosticId,
        "Couldn't generate constructor",
        $"Type decorated with {Source.AttributeFullName} must not be a nested type - nested types are not supported");

    public static readonly DiagnosticDescriptor TypeNameDoesNotEndWithViewModelRule = CreateRule(
        TypeNameDoesNotEndWithViewModelDiagnosticId,
        "Couldn't generate constructor",
        $"Type decorated with {Source.AttributeFullName} must have a name ending with \"ViewModel\" for the constructor to be generated");

    public static readonly DiagnosticDescriptor ConflictingConstructorRule = CreateRule(
        ConflictingConstructorDiagnosticId,
        "Couldn't generate constructor",
        $"Type decorated with {Source.AttributeFullName} already declares a parameterless constructor - remove it or remove the attribute");

    public static readonly DiagnosticDescriptor NoPrivateReadonlyFieldsRule = CreateRule(
        NoPrivateReadonlyFieldsDiagnosticId,
        "Generated constructor has nothing to initialize",
        $"Type decorated with {Source.AttributeFullName} has no private or protected readonly fields without an initializer - the generated constructor will be empty");

    private static DiagnosticDescriptor CreateRule(string id, string title, string messageFormat, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            "Usage",
            severity,
            true,
            null,
            $"https://github.com/VolocyNazad/toolkit.xaml-constructor?tab=readme-ov-file#{id.ToLowerInvariant()}",
            WellKnownDiagnosticTags.Build);
    }
}
