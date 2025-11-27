using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Harmonize;

public static class SymbolExtensions
{
    extension(IMethodSymbol symbol)
    {
        public PatchType GetPatchType()
        {
            // we've experimentally determined that name-based matching takes precedence over attribute-based
            PatchType patchType = symbol.Name switch
            {
                "Prefix" => PatchType.Prefix,
                "Postfix" => PatchType.Postfix,
                "Transpiler" => PatchType.Transpiler,
                _ => PatchType.Unknown,
            };

            if (patchType == PatchType.Unknown)
            {
                ImmutableArray<AttributeData> attrs = symbol.GetAttributes();
                foreach (AttributeData attr in attrs)
                {
                    string? attributeFullName = attr.AttributeClass?.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );
                    if (attributeFullName == null)
                    {
                        continue;
                    }

                    if (attributeFullName == "global::HarmonyLib.HarmonyPrefix")
                    {
                        patchType = PatchType.Prefix;
                        break;
                    }
                    if (attributeFullName == "global::HarmonyLib.HarmonyPostfix")
                    {
                        patchType = PatchType.Postfix;
                        break;
                    }
                    if (attributeFullName == "global::HarmonyLib.HarmonyTranspiler")
                    {
                        patchType = PatchType.Transpiler;
                        break;
                    }
                }
            }
            return patchType;
        }
    }
}
