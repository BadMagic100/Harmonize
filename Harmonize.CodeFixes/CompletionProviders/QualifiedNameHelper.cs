using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Harmonize.CompletionProviders;

internal class QualifiedNameHelper
{
    public static string GetMinimallyQualifiedTypeName(
        INamedTypeSymbol type,
        SemanticModel semanticModel,
        int position
    )
    {
        ImmutableArray<ISymbol> availableTypes = semanticModel.LookupNamespacesAndTypes(
            position,
            name: type.Name
        );
        string displayName = type.ToDisplayString();
        foreach (ISymbol symbol in availableTypes)
        {
            if (symbol is ITypeSymbol ts && ts.Equals(type, SymbolEqualityComparer.Default))
            {
                displayName = symbol.Name;
                break;
            }
        }
        return displayName;
    }

    public static string GetMinimallyQualifiedTypeName(
        INamedTypeSymbol type,
        SemanticModel semanticModel,
        TextSpan span
    )
    {
        return GetMinimallyQualifiedTypeName(type, semanticModel, span.Start);
    }
}
