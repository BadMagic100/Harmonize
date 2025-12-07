using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Harmonize;

public static class SymbolExtensions
{
    extension(IMethodSymbol symbol)
    {
        private HashSet<PatchType> GetAllCandidates()
        {
            HashSet<PatchType> candidates = [];

            if (symbol.Name == "Prefix")
            {
                candidates.Add(PatchType.Prefix);
            }
            else if (symbol.Name == "Postfix")
            {
                candidates.Add(PatchType.Postfix);
            }
            else if (symbol.Name == "Transpiler")
            {
                candidates.Add(PatchType.Transpiler);
            }

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
                    candidates.Add(PatchType.Prefix);
                }
                if (attributeFullName == "global::HarmonyLib.HarmonyPostfix")
                {
                    candidates.Add(PatchType.Postfix);
                }
                if (attributeFullName == "global::HarmonyLib.HarmonyTranspiler")
                {
                    candidates.Add(PatchType.Transpiler);
                }
            }
            return candidates;
        }

        public PatchType GetPatchType()
        {
            HashSet<PatchType> candidates = symbol.GetAllCandidates();
            return candidates.Count == 1 ? candidates.First() : PatchType.Unknown;
        }

        public bool HasAnyPatchType()
        {
            HashSet<PatchType> candidates = symbol.GetAllCandidates();
            return candidates.Count > 0;
        }
    }
}
