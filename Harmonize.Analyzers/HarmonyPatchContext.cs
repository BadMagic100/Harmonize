using System.Collections.Generic;
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

        IEnumerable<IMethodSymbol> candidateMethods =
            PatchCandidateMatcher.GetFullySpecifiedCandidates(
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
}
