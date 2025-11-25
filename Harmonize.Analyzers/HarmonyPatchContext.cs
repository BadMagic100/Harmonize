using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Harmonize;

public enum PatchType
{
    Unknown,
    Prefix,
    Postfix,
    Transpiler,
}

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

        // we've experimentally determined that name-based matching takes precedence over attribute-based
        PatchType patchType = symbol.Name switch
        {
            "Prefix" => PatchType.Prefix,
            "Postfix" => PatchType.Postfix,
            "Transpiler" => PatchType.Transpiler,
            _ => PatchType.Unknown,
        };

        ImmutableArray<AttributeData> attrs = symbol.GetAttributes();
        List<HarmonyPatchAttributeData> targetInfos = [];
        foreach (AttributeData attr in attrs)
        {
            string? attributeFullName = attr.AttributeClass?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            if (attributeFullName == null)
            {
                continue;
            }

            if (
                patchType == PatchType.Unknown
                && attributeFullName == "global::HarmonyLib.HarmonyPrefix"
            )
            {
                patchType = PatchType.Prefix;
            }
            if (
                patchType == PatchType.Unknown
                && attributeFullName == "global::HarmonyLib.HarmonyPostfix"
            )
            {
                patchType = PatchType.Postfix;
            }
            if (
                patchType == PatchType.Unknown
                && attributeFullName == "global::HarmonyLib.HarmonyTranspiler"
            )
            {
                patchType = PatchType.Transpiler;
            }

            if (attributeFullName != "global::HarmonyLib.HarmonyPatch")
            {
                continue;
            }

            targetInfos.Add(HarmonyPatchAttributeData.ExtractFromAttribute(attr));
        }

        // couldn't figure out what type of patch
        if (patchType == PatchType.Unknown)
        {
            return null;
        }

        ImmutableArray<AttributeData> classAttrs = symbol.ContainingType.GetAttributes();
        List<HarmonyPatchAttributeData> classTargetInfos = [];
        foreach (AttributeData attr in classAttrs)
        {
            string? attributeFullName = attr.AttributeClass?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            if (attributeFullName != "global::HarmonyLib.HarmonyPatch")
            {
                continue;
            }

            classTargetInfos.Add(HarmonyPatchAttributeData.ExtractFromAttribute(attr));
        }

        // not declared as a HarmonyPatch
        if (targetInfos.Count == 0 && classTargetInfos.Count == 0)
        {
            return null;
        }

        // data can be spread across multiple attributes, so merge all the attributes for both the method and the class.
        // method data fields overwrite the value on the class if both are present.
        HarmonyPatchAttributeData mergedTargetInfo = new(null, null, null, null, null);
        for (int i = 0; i < targetInfos.Count; i++)
        {
            mergedTargetInfo = HarmonyPatchAttributeData.MergeSymmetric(
                mergedTargetInfo,
                targetInfos[i]
            );
        }

        HarmonyPatchAttributeData classMergedTargetInfo = new(null, null, null, null, null);
        for (int i = 0; i < classTargetInfos.Count; i++)
        {
            classMergedTargetInfo = HarmonyPatchAttributeData.MergeSymmetric(
                classMergedTargetInfo,
                classTargetInfos[i]
            );
        }

        HarmonyPatchAttributeData finalTargetInfo =
            mergedTargetInfo?.MergeOver(classMergedTargetInfo) ?? classMergedTargetInfo;
        // apply defaults. default for argument type/kind is "whatever matches" which we can't represent here
        if (finalTargetInfo.MethodKind == null)
        {
            finalTargetInfo = finalTargetInfo with { MethodKind = MethodKind.Normal };
        }

        // to proceed we need unambiguous values (or defaults) for all fields. a separate analyzer will flag a problem
        // if this is not the case
        if (
            finalTargetInfo.TargetType == null
            || finalTargetInfo.TargetType.IsAmbiguous
            || finalTargetInfo.TargetMethodName == null
            || finalTargetInfo.TargetMethodName.IsAmbiguous
            || finalTargetInfo.MethodKind.IsAmbiguous
            || finalTargetInfo.ArgumentTypes?.IsAmbiguous == true
            || finalTargetInfo.ArgumentKinds?.IsAmbiguous == true
        )
        {
            return null;
        }

        ImmutableArray<IMethodSymbol> candidateMethods = GetCandidateMethods(
            finalTargetInfo.TargetType.Value,
            finalTargetInfo.TargetMethodName.Value,
            finalTargetInfo.MethodKind.Value,
            finalTargetInfo.ArgumentTypes?.Value,
            finalTargetInfo.ArgumentKinds?.Value
        );

        return new HarmonyPatchContext(
            finalTargetInfo.TargetType.Value,
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
