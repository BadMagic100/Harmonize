using Microsoft.CodeAnalysis;

namespace Harmonize;

internal static class AuxilaryMethodClassifier
{
    public static bool IsPrepareCandidate(IMethodSymbol method)
    {
        if (method.Name == "Prepare")
        {
            return true;
        }

        foreach (AttributeData attr in method.GetAttributes())
        {
            if (
                attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::HarmonyLib.HarmonyPrepare"
            )
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsTargetMethodCandidate(IMethodSymbol method)
    {
        if (method.Name == "TargetMethod")
        {
            return true;
        }

        foreach (AttributeData attr in method.GetAttributes())
        {
            if (
                attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::HarmonyLib.HarmonyTargetMethod"
            )
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsTargetMethodsCandidate(IMethodSymbol method)
    {
        if (method.Name == "TargetMethods")
        {
            return true;
        }

        foreach (AttributeData attr in method.GetAttributes())
        {
            if (
                attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::HarmonyLib.HarmonyTargetMethods"
            )
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsCleanupCandidate(IMethodSymbol method)
    {
        if (method.Name == "Cleanup")
        {
            return true;
        }

        foreach (AttributeData attr in method.GetAttributes())
        {
            if (
                attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::HarmonyLib.HarmonyCleanup"
            )
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsAnyAuxilaryMethodCandidate(IMethodSymbol method)
    {
        return IsPrepareCandidate(method)
            || IsTargetMethodCandidate(method)
            || IsTargetMethodsCandidate(method)
            || IsCleanupCandidate(method);
    }
}
