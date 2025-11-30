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

        // apply defaults. default for arguments is "whatever matches" which we can't represent here
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
            || targetInfo.Arguments?.IsAmbiguous == true
        )
        {
            return null;
        }

        ImmutableArray<IMethodSymbol> candidateMethods = GetCandidateMethods(
            targetInfo.TargetType.Value,
            targetInfo.TargetMethodName.Value,
            targetInfo.MethodKind.Value,
            targetInfo.Arguments?.Value
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
        ImmutableEquatableArray<ArgumentDescriptor>? args
    )
    {
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

        if (args != null)
        {
            candidates = candidates.Where(m => AllArgsMatch(m.Parameters, args));
        }

        return candidates.ToImmutableArray();
    }

    private static bool AllArgsMatch(
        ImmutableEquatableArray<IParameterSymbol> parameters,
        ImmutableEquatableArray<ArgumentDescriptor> args
    )
    {
        if (parameters.Count != args.Count)
        {
            return false;
        }

        for (int i = 0; i < parameters.Count; i++)
        {
            IParameterSymbol p = parameters[i];
            ArgumentKind k = args[i].Kind;
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
            if (!p.Type.Equals(args[i].Type, SymbolEqualityComparer.Default))
            {
                return false;
            }
        }
        return true;
    }
}
