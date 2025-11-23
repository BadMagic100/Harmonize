using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Harmonize;

public enum PatchType
{
    Unknown,
    Prefix,
    Postfix,
    Transpiler
}

public record HarmonyContext(
    INamedTypeSymbol? TargetType,
    IMethodSymbol? TargetMethod,
    ImmutableArray<INamedTypeSymbol> CandidateTypes,
    ImmutableArray<IMethodSymbol> CandidateMethods,
    PatchType PatchType)
{
    public static HarmonyContext? GetContextForNode(MethodDeclarationSyntax decl, SemanticModel model, CancellationToken cancellationToken)
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

        PatchType patchType = symbol.Name switch
        {
            "Prefix" => PatchType.Prefix,
            "Postfix" => PatchType.Postfix,
            "Transpiler" => PatchType.Transpiler,
            _ => PatchType.Unknown,
        };

        ImmutableArray<INamedTypeSymbol>.Builder candidateTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        ImmutableArray<IMethodSymbol>.Builder candidateMethods = ImmutableArray.CreateBuilder<IMethodSymbol>();

        ImmutableArray<AttributeData> attrs = symbol.GetAttributes();
        foreach (AttributeData attr in attrs)
        {
            string? attributeFullName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attributeFullName == null)
            {
                continue;
            }

            if (patchType == PatchType.Unknown && attributeFullName == "global::HarmonyLib.HarmonyPrefix")
            {
                patchType = PatchType.Prefix;
            }
            if (patchType == PatchType.Unknown && attributeFullName == "global::HarmonyLib.HarmonyPostfix")
            {
                patchType = PatchType.Postfix;
            }
            if (patchType == PatchType.Unknown && attributeFullName == "global::HarmonyLib.HarmonyTranspiler")
            {
                patchType = PatchType.Transpiler;
            }

            if (attributeFullName != "global::HarmonyLib.HarmonyPatch")
            {
                continue;
            }

            // handle exactly the Type, string overload for now
            if (attr.ConstructorArguments.Length == 2)
            {
                TypedConstant arg1 = attr.ConstructorArguments[0];
                TypedConstant arg2 = attr.ConstructorArguments[1];
                if (arg1.Kind == TypedConstantKind.Type && arg1.Value is INamedTypeSymbol type
                    && arg2.Kind == TypedConstantKind.Primitive && arg2.Value is string name)
                {
                    candidateTypes.Add(type);
                    candidateMethods.AddRange(type.GetMembers(name).OfType<IMethodSymbol>());
                }
            }
        }

        // todo: if there's something missing from the method itself go look at the containing class
        INamedTypeSymbol? unambiguousTargetType = candidateTypes.Count == 1 ? candidateTypes[0] : null;
        IMethodSymbol? unambiguousTargetMethod = candidateMethods.Count == 1 ? candidateMethods[0] : null;

        return new HarmonyContext(
            unambiguousTargetType,
            unambiguousTargetMethod,
            candidateTypes.ToImmutable(),
            candidateMethods.ToImmutable(),
            patchType
        );
    }
}
