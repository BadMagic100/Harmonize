using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Harmonize;

// mapping of https://harmony.pardeike.net/api/HarmonyLib.MethodType.html
public enum MethodKind
{
    Normal,
    Getter,
    Setter,
    Unsupported,
}

// mapping of https://harmony.pardeike.net/api/HarmonyLib.ArgumentType.html
public enum ArgumentKind
{
    Normal,
    Out,
    Ref,
    Unsupported,
}

file static class EnumMapping
{
    public static MethodKind ParseMethodKind(ITypeSymbol enumType, object? constantValue)
    {
        IFieldSymbol? matchedValue = enumType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue && f.ConstantValue == constantValue)
            .FirstOrDefault();
        return matchedValue?.Name switch
        {
            "Normal" => MethodKind.Normal,
            "Getter" => MethodKind.Getter,
            "Setter" => MethodKind.Setter,
            _ => MethodKind.Unsupported,
        };
    }

    public static ImmutableArray<ArgumentKind> ParseArgumentKinds(
        ITypeSymbol enumType,
        ImmutableArray<TypedConstant> values
    )
    {
        Dictionary<object?, string> lookup = enumType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .ToDictionary(f => f.ConstantValue, f => f.Name);
        ImmutableArray<ArgumentKind>.Builder builder = ImmutableArray.CreateBuilder<ArgumentKind>(
            values.Length
        );
        foreach (TypedConstant value in values)
        {
            if (lookup.TryGetValue(value.Value, out string name))
            {
                builder.Add(
                    name switch
                    {
                        "Normal" => ArgumentKind.Normal,
                        "Out" => ArgumentKind.Out,
                        "Ref" => ArgumentKind.Ref,
                        _ => ArgumentKind.Unsupported,
                    }
                );
            }
            else
            {
                builder.Add(ArgumentKind.Unsupported);
            }
        }
        return builder.ToImmutable();
    }
}

public record HarmonyPatchAttributeData(
    MaybeAmbiguous<INamedTypeSymbol>? TargetType,
    MaybeAmbiguous<string>? TargetMethodName,
    MaybeAmbiguous<MethodKind>? MethodKind,
    MaybeAmbiguous<ImmutableArray<INamedTypeSymbol>>? ArgumentTypes,
    MaybeAmbiguous<ImmutableArray<ArgumentKind>>? ArgumentKinds
)
{
    // overloads at https://harmony.pardeike.net/api/HarmonyLib.HarmonyPatch.html
    public static HarmonyPatchAttributeData ExtractFromAttribute(AttributeData data)
    {
        ImmutableArray<TypedConstant> args = data.ConstructorArguments;
        if (args.Length == 0)
        {
            return new HarmonyPatchAttributeData(null, null, null, null, null);
        }

        INamedTypeSymbol? declaringType;
        string? methodName;
        MethodKind? methodType;
        ImmutableArray<INamedTypeSymbol>? argumentTypes;
        ImmutableArray<ArgumentKind>? argumentKinds;

        if (args.Length == 1)
        {
            if (TryReadMethodType(args[0], out methodType))
            {
                return new(null, null, methodType, null, null);
            }
            else if (TryReadString(args[0], out methodName))
            {
                return new(null, methodName, null, null, null);
            }
            else if (TryReadType(args[0], out declaringType))
            {
                return new(declaringType.ToMA(), null, null, null, null);
            }
            else if (TryReadTypeArray(args[0], out argumentTypes))
            {
                return new(null, null, null, argumentTypes, null);
            }
        }
        else if (args.Length == 2)
        {
            if (TryReadMethodType(args[0], out methodType))
            {
                if (TryReadTypeArray(args[1], out argumentTypes))
                {
                    return new(null, null, methodType, argumentTypes, null);
                }
            }
            else if (TryReadString(args[0], out methodName))
            {
                if (TryReadMethodType(args[1], out methodType))
                {
                    return new(null, methodName, methodType, null, null);
                }
                else if (TryReadTypeArray(args[1], out argumentTypes))
                {
                    return new(null, methodName, null, argumentTypes, null);
                }
            }
            else if (TryReadType(args[0], out declaringType))
            {
                if (TryReadMethodType(args[1], out methodType))
                {
                    return new(declaringType.ToMA(), null, methodType, null, null);
                }
                else if (TryReadString(args[1], out methodName))
                {
                    return new(declaringType.ToMA(), methodName, null, null, null);
                }
                else if (TryReadTypeArray(args[1], out argumentTypes))
                {
                    return new(declaringType.ToMA(), null, null, argumentTypes, null);
                }
            }
            else if (TryReadTypeArray(args[0], out argumentTypes))
            {
                if (TryReadArgumentTypeArray(args[1], out argumentKinds))
                {
                    return new(null, null, null, argumentTypes, argumentKinds);
                }
            }
        }
        else if (args.Length == 3)
        {
            if (TryReadMethodType(args[0], out methodType))
            {
                if (TryReadTypeArray(args[1], out argumentTypes))
                {
                    if (TryReadArgumentTypeArray(args[2], out argumentKinds))
                    {
                        return new(null, null, methodType, argumentTypes, argumentKinds);
                    }
                }
            }
            else if (TryReadString(args[0], out methodName))
            {
                if (TryReadString(args[1], out _))
                {
                    // intentionally doesn't support the (string, string, MethodType) overload because we'd have to search the whole
                    // semantic model to get the declaring type from the name
                    return new(null, null, null, null, null);
                }
                else if (TryReadTypeArray(args[1], out argumentTypes))
                {
                    if (TryReadArgumentTypeArray(args[2], out argumentKinds))
                    {
                        return new(null, methodName, null, argumentTypes, argumentKinds);
                    }
                }
            }
            else if (TryReadType(args[0], out declaringType))
            {
                if (TryReadMethodType(args[1], out methodType))
                {
                    if (TryReadTypeArray(args[2], out argumentTypes))
                    {
                        return new(declaringType.ToMA(), null, methodType, argumentTypes, null);
                    }
                }
                else if (TryReadString(args[1], out methodName))
                {
                    if (TryReadMethodType(args[2], out methodType))
                    {
                        return new(declaringType.ToMA(), methodName, methodType, null, null);
                    }
                    else if (TryReadTypeArray(args[2], out argumentTypes))
                    {
                        return new(declaringType.ToMA(), methodName, null, argumentTypes, null);
                    }
                }
            }
        }
        else if (args.Length == 4)
        {
            // the only clever optimization we will attempt in this function:
            // the 4-arg overloads match the pattern (Type, *, Type[], ArgumentType[])
            // so we will parse the 2nd arg last

            if (
                TryReadType(args[0], out declaringType)
                && TryReadTypeArray(args[2], out argumentTypes)
                && TryReadArgumentTypeArray(args[3], out argumentKinds)
            )
            {
                if (TryReadMethodType(args[1], out methodType))
                {
                    return new(
                        declaringType.ToMA(),
                        null,
                        methodType,
                        argumentTypes,
                        argumentKinds
                    );
                }
                else if (TryReadString(args[1], out methodName))
                {
                    return new(
                        declaringType.ToMA(),
                        methodName,
                        null,
                        argumentTypes,
                        argumentKinds
                    );
                }
            }
        }

        return new HarmonyPatchAttributeData(null, null, null, null, null);
    }

    public HarmonyPatchAttributeData MergeOver(HarmonyPatchAttributeData other)
    {
        return new HarmonyPatchAttributeData(
            TargetType ?? other.TargetType,
            TargetMethodName ?? other.TargetMethodName,
            MethodKind ?? other.MethodKind,
            ArgumentTypes ?? other.ArgumentTypes,
            ArgumentKinds ?? other.ArgumentKinds
        );
    }

    public static HarmonyPatchAttributeData MergeSymmetric(
        HarmonyPatchAttributeData a,
        HarmonyPatchAttributeData b
    )
    {
        MaybeAmbiguous<INamedTypeSymbol>? candidateTypes =
            MaybeAmbiguous<INamedTypeSymbol>.MergeSymmetric(a.TargetType, b.TargetType);
        MaybeAmbiguous<string>? candidateMethodNames = MaybeAmbiguous<string>.MergeSymmetric(
            a.TargetMethodName,
            b.TargetMethodName
        );
        MaybeAmbiguous<MethodKind>? candidateMethodKinds =
            MaybeAmbiguous<MethodKind>.MergeSymmetric(a.MethodKind, b.MethodKind);
        MaybeAmbiguous<ImmutableArray<INamedTypeSymbol>>? candidateArgumentTypes = MaybeAmbiguous<
            ImmutableArray<INamedTypeSymbol>
        >.MergeSymmetric(a.ArgumentTypes, b.ArgumentTypes);
        MaybeAmbiguous<ImmutableArray<ArgumentKind>>? candidateArgumentKinds = MaybeAmbiguous<
            ImmutableArray<ArgumentKind>
        >.MergeSymmetric(a.ArgumentKinds, b.ArgumentKinds);

        return new HarmonyPatchAttributeData(
            candidateTypes,
            candidateMethodNames,
            candidateMethodKinds,
            candidateArgumentTypes,
            candidateArgumentKinds
        );
    }

    private static bool TryReadMethodType(
        TypedConstant arg,
        [NotNullWhen(true)] out MethodKind? methodType
    )
    {
        if (
            arg.Kind == TypedConstantKind.Enum
            && arg.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::HarmonyLib.MethodType"
        )
        {
            methodType = EnumMapping.ParseMethodKind(arg.Type, arg.Value);
            return true;
        }

        methodType = null;
        return false;
    }

    private static bool TryReadString(TypedConstant arg, [NotNullWhen(true)] out string? str)
    {
        if (
            arg.Kind == TypedConstantKind.Primitive
            && !arg.IsNull
            && arg.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "string"
        )
        {
            str = (string)arg.Value!;
            return true;
        }

        str = null;
        return false;
    }

    private static bool TryReadType(
        TypedConstant arg,
        [NotNullWhen(true)] out INamedTypeSymbol? type
    )
    {
        if (arg.Kind == TypedConstantKind.Type && !arg.IsNull)
        {
            type = (INamedTypeSymbol)arg.Value!;
            return true;
        }

        type = null;
        return false;
    }

    private static bool TryReadTypeArray(
        TypedConstant arg,
        [NotNullWhen(true)] out ImmutableArray<INamedTypeSymbol>? typeArray
    )
    {
        if (
            arg.Type is IArrayTypeSymbol arr
            && !arg.IsNull
            && arr.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::System.Type"
        )
        {
            typeArray = arg.Values.Select(v => (INamedTypeSymbol)v.Value!).ToImmutableArray();
            return true;
        }

        typeArray = null;
        return false;
    }

    private static bool TryReadArgumentTypeArray(
        TypedConstant arg,
        [NotNullWhen(true)] out ImmutableArray<ArgumentKind>? argumentTypeArray
    )
    {
        if (
            arg.Type is IArrayTypeSymbol arr
            && !arg.IsNull
            && arr.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::HarmonyLib.ArgumentType"
        )
        {
            argumentTypeArray = EnumMapping.ParseArgumentKinds(arr.ElementType, arg.Values);
            return true;
        }

        argumentTypeArray = null;
        return false;
    }
}
