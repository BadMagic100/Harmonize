using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Harmonize;

public record HarmonyPatchContext(
    INamedTypeSymbol TargetType,
    MaybeAmbiguous<IMethodSymbol>? TargetMethod,
    PatchType PatchType
)
{
    public static HarmonyPatchContext? GetContextForNode(
        MethodDeclarationSyntax decl,
        SemanticModel model,
        CancellationToken cancellationToken
    )
    {
        // easy out - harmony patches are only legal as static methods
        if (!decl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            return null;
        }

        IMethodSymbol? symbol = model.GetDeclaredSymbol(decl, cancellationToken);
        if (symbol == null)
        {
            return null;
        }

        PatchType patchType = symbol.GetPatchType();
        if (patchType == PatchType.Unknown)
        {
            return null;
        }

        HarmonyPatchAttributeData? targetInfo =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(symbol);
        if (targetInfo == null)
        {
            return null;
        }

        // apply defaults. default for argument type/kind is "whatever matches" which we can't represent here
        if (targetInfo.MethodKind == null)
        {
            targetInfo = targetInfo with { MethodKind = MethodKind.Normal };
        }

        // to proceed we need unambiguous values (or defaults) for all fields. a separate analyzer will flag a problem
        // if this is not the case
        if (
            targetInfo.TargetType == null
            || targetInfo.TargetType.IsAmbiguous
            || targetInfo.TargetMethodName == null
            || targetInfo.TargetMethodName.IsAmbiguous
            || targetInfo.MethodKind.IsAmbiguous
            || targetInfo.ArgumentTypes?.IsAmbiguous == true
            || targetInfo.ArgumentKinds?.IsAmbiguous == true
        )
        {
            return null;
        }

        ImmutableArray<IMethodSymbol> candidateMethods = GetCandidateMethods(
            targetInfo.TargetType.Value,
            targetInfo.TargetMethodName.Value,
            targetInfo.MethodKind.Value,
            targetInfo.ArgumentTypes?.Value,
            targetInfo.ArgumentKinds?.Value
        );

        return new HarmonyPatchContext(
            targetInfo.TargetType.Value,
            MaybeAmbiguous<IMethodSymbol>.FromEnumerable(candidateMethods),
            patchType
        );
    }

    private static ImmutableArray<IMethodSymbol> GetCandidateMethods(
        INamedTypeSymbol declaringType,
        string name,
        MethodKind kind,
        ImmutableArray<INamedTypeSymbol>? argTypes,
        ImmutableArray<ArgumentKind>? argKinds
    )
    {
        // shortcut: if argTypes and argKinds are both non-null and don't match in length nothing will save it
        if (argTypes != null && argKinds != null && argTypes.Value.Length != argKinds.Value.Length)
        {
            return ImmutableArray<IMethodSymbol>.Empty;
        }

        IEnumerable<IMethodSymbol> candidates = kind switch
        {
            MethodKind.Normal => declaringType
                .GetMembers(name)
                .OfType<IMethodSymbol>()
                .Where(m => m.CanBeReferencedByName),
            MethodKind.Getter => declaringType
                .GetMembers(name)
                .OfType<IPropertySymbol>()
                .Where(p => p.GetMethod != null)
                .Select(p => p.GetMethod!),
            MethodKind.Setter => declaringType
                .GetMembers(name)
                .OfType<IPropertySymbol>()
                .Where(p => p.SetMethod != null)
                .Select(p => p.SetMethod!),
            _ => [],
        };

        if (argTypes != null)
        {
            candidates = candidates.Where(m => AllArgTypesMatch(m.Parameters, argTypes.Value));
        }
        if (argKinds != null)
        {
            candidates = candidates.Where(m => AllArgKindsMatch(m.Parameters, argKinds.Value));
        }

        return candidates.ToImmutableArray();
    }

    private static bool AllArgTypesMatch(
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<INamedTypeSymbol> types
    )
    {
        if (parameters.Length != types.Length)
        {
            return false;
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            if (!parameters[i].Type.Equals(types[i], SymbolEqualityComparer.Default))
            {
                return false;
            }
        }
        return true;
    }

    private static bool AllArgKindsMatch(
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<ArgumentKind> kinds
    )
    {
        if (parameters.Length != kinds.Length)
        {
            return false;
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            IParameterSymbol p = parameters[i];
            ArgumentKind k = kinds[i];
            if (k == ArgumentKind.Normal && p.RefKind != RefKind.None)
            {
                return false;
            }
            if (k == ArgumentKind.Out && p.RefKind != RefKind.Out)
            {
                return false;
            }
            if (k == ArgumentKind.Ref && p.RefKind != RefKind.Ref)
            {
                return false;
            }
        }
        return true;
    }
}
