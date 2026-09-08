using Microsoft.CodeAnalysis;

namespace XamlConstructor.Generator.Extensions;

internal static class SymbolExtension
{
    public static bool HasAttribute(this ISymbol symbol, string attributeName)
    {
        _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

        return symbol.GetAttribute(attributeName) is not null;
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeName)
    {
        _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

        return symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Name == attributeName);
    }
}
