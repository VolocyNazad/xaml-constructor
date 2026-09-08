using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XamlConstructor.Tests;

/// <summary>
/// Builds throwaway <see cref="CSharpCompilation"/> instances for generator/analyzer/code-fix
/// tests, referencing every assembly already loaded into the test process instead of pulling in
/// a separate reference-assembly package.
/// </summary>
internal static class CompilationFactory
{
    /// <summary>
    /// A minimal, deliberately old-syntax stand-in for <c>XamlConstructor.Generator.Source.AttributeText</c>,
    /// used when parsing/binding test sources with the exact <c>Microsoft.CodeAnalysis.CSharp</c> version this
    /// repo pins - unlike the real generator (which always runs inside whatever Roslyn version hosts it),
    /// tests load that pinned version directly, and it may predate the primary-constructor syntax the real
    /// attribute source uses.
    /// </summary>
    public const string MinimalAttributeSource = """
        using System;

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
        internal sealed class XamlConstructorAttribute : Attribute
        {
        }
        """;

    public static readonly ImmutableArray<MetadataReference> References =
    [
        .. AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => (MetadataReference)MetadataReference.CreateFromFile(assembly.Location)),
    ];

    public static CSharpCompilation CreateCompilation(string assemblyName, params string[] sources)
    {
        IEnumerable<SyntaxTree> syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source));

        return CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
