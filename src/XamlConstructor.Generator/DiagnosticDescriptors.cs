using Microsoft.CodeAnalysis;

namespace XamlConstructor.Generator;

public static class DiagnosticDescriptors
{
    public const string TypeWithoutPartialDiagnosticId = "XCONS01";
    public const string ConstructorCreationFailedDiagnosticId = "XCONS02";
    public const string NoPrivateReadonlyFieldsDiagnosticId = "XCONS03";

    public static readonly DiagnosticDescriptor TypeWithoutPartialRule = new(
        TypeWithoutPartialDiagnosticId,
        "Couldn't generate constructor",
        $"Type decorated with {Source.AttributeFullName} must be also declared partial",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        null,
        $"https://github.com/VolocyNazad/XamlConstructor#{TypeWithoutPartialDiagnosticId}",
        WellKnownDiagnosticTags.Build);

    public static readonly DiagnosticDescriptor ConstructorCreationFailedRule = new(
        ConstructorCreationFailedDiagnosticId,
        "Constructor generation failed",
        "Failed to generate constructor for type '{0}': {1}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        null,
        $"https://github.com/VolocyNazad/XamlConstructor#{ConstructorCreationFailedDiagnosticId}",
        WellKnownDiagnosticTags.Build);
}
