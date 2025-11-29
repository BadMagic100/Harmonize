using Harmonize.Test.Helper;
using Microsoft.CodeAnalysis;

namespace Harmonize.Test;

public class TestHarmonyPatchAttributeData
{
    [Fact(DisplayName = "ExtractFromSymbol should return null from unannotated method")]
    public async Task ExtractFromSymbol_Unannotated()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        Assert.Null(actual);
    }

    [Fact(DisplayName = "ExtractFromSymbol should return empty data for no-arg constructor")]
    public async Task ExtractFromSymbol_NoArg()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        Assert.Equal(new HarmonyPatchAttributeData(null, null, null, null, null), actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return MethodKind only for (MethodType) constructor"
    )]
    public async Task ExtractFromSymbol_MethodType()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(MethodType.Getter)]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        Assert.Equal(
            new HarmonyPatchAttributeData(null, null, MethodKind.Getter, null, null),
            actual
        );
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return MethodKind and ArgumentTypes for (MethodType, Type[]) constructor"
    )]
    public async Task ExtractFromSymbol_MethodKind_TypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(MethodType.Getter, [typeof(Patches)])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            null,
            null,
            MethodKind.Getter,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return MethodKind, ArgumentTypes, and ArgumentKinds for (MethodType, Type[], ArgumentType[]) constructor"
    )]
    public async Task ExtractFromSymbol_MethodType_TypeArray_ArgumentTypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(MethodType.Getter, [typeof(Patches)], [ArgumentType.Normal])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            null,
            null,
            MethodKind.Getter,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            ImmutableEquatableArray.Create(ArgumentKind.Normal)
        );
        Assert.Equal(expected, actual);
    }

    [Fact(DisplayName = "ExtractFromSymbol should return MethodName for (string) constructor")]
    public async Task ExtractFromSymbol_String()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch("Foo")]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(null, "Foo", null, null, null);
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return MethodName and MethodKind for (string, MethodType) constructor"
    )]
    public async Task ExtractFromSymbol_String_MethodType()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch("Foo", MethodType.Getter)]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(null, "Foo", MethodKind.Getter, null, null);
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return empty for (string, string, MethodType) constructor"
    )]
    public async Task ExtractFromSymbol_String_String_MethodType()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch("Foo", "Bar", MethodType.Getter)]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(null, null, null, null, null);
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return MethodName and ArgumentTyps for (string, Type[]) constructor"
    )]
    public async Task ExtractFromSymbol_String_TypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch("Foo", [typeof(Patches)])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            null,
            "Foo",
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return MethodName, ArgumentTypes, and ArgumentKinds for (string, Type[], ArgumentType[]) constructor"
    )]
    public async Task ExtractFromSymbol_String_TypeArray_ArgumentTypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch("Foo", [typeof(Patches)], [ArgumentType.Normal])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            null,
            "Foo",
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            ImmutableEquatableArray.Create(ArgumentKind.Normal)
        );
        Assert.Equal(expected, actual);
    }

    [Fact(DisplayName = "ExtractFromSymbol should return TargetType from (Type) constructor")]
    public async Task ExtractFromSymbol_Type()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches))]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            null,
            null,
            null,
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType and MethodKind from (Type, MethodType) constructor"
    )]
    public async Task ExtractFromSymbol_Type_MethodType()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), MethodType.Getter)]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            null,
            MethodKind.Getter,
            null,
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType, MethodKind, and ArgumentTypes from (Type, MethodType, Type[]) constructor"
    )]
    public async Task ExtractFromSymbol_Type_MethodType_TypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), MethodType.Getter, [typeof(Patches)])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            null,
            MethodKind.Getter,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType, MethodKind, ArgumentTypes, and ArgumentKinds from (Type, MethodType, Type[], ArgumentType[]) constructor"
    )]
    public async Task ExtractFromSymbol_Type_MethodType_TypeArray_ArgumentTypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), MethodType.Getter, [typeof(Patches)], [ArgumentType.Normal])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            null,
            MethodKind.Getter,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            ImmutableEquatableArray.Create(ArgumentKind.Normal)
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType and MethodName from (Type, string) constructor"
    )]
    public async Task ExtractFromSymbol_Type_String()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), "Foo")]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            "Foo",
            null,
            null,
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType, MethodName, and MethodKind from (Type, string, MethodType) constructor"
    )]
    public async Task ExtractFromSymbol_Type_String_MethodType()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), "Foo", MethodType.Getter)]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            "Foo",
            MethodKind.Getter,
            null,
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType, MethodName, and ArgumentTypes from (Type, string, Type[]) constructor"
    )]
    public async Task ExtractFromSymbol_Type_String_TypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), "Foo", [typeof(Patches)])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            "Foo",
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType, MethodName, ArgumentTypes, and ArgumentKinds from (Type, string, Type[], ArgumentType[]) constructor"
    )]
    public async Task ExtractFromSymbol_Type_String_TypeArray_ArgumentTypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), "Foo", [typeof(Patches)], [ArgumentType.Normal])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            "Foo",
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            ImmutableEquatableArray.Create(ArgumentKind.Normal)
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return TargetType and ArgumentTypes from (Type, Type[]) constructor"
    )]
    public async Task ExtractFromSymbol_Type_TypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches), [typeof(Patches)])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            null,
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(DisplayName = "ExtractFromSymbol should return ArgumentTypes from (Type[]) constructor")]
    public async Task ExtractFromSymbol_TypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch([typeof(Patches)])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            null,
            null,
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            null
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromSymbol should return ArgumentTypes and ArgumentKinds from (Type[], ArgumentType[]) constructor"
    )]
    public async Task ExtractFromSymbol_TypeArray_ArgumentTypeArray()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch([typeof(Patches)], [ArgumentType.Normal])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            null,
            null,
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            ImmutableEquatableArray.Create(ArgumentKind.Normal)
        );
        Assert.Equal(expected, actual);
    }

    [Theory(DisplayName = "ExtractFromSymbol should correctly map MethodType")]
    [InlineData("Normal", MethodKind.Normal)]
    [InlineData("Getter", MethodKind.Getter)]
    [InlineData("Setter", MethodKind.Setter)]
    [InlineData("Enumerator", MethodKind.Unsupported)]
    [InlineData("Constructor", MethodKind.Unsupported)]
    [InlineData("Finalizer", MethodKind.Unsupported)]
    [InlineData("StaticConstructor", MethodKind.Unsupported)]
    [InlineData("EventAdd", MethodKind.Unsupported)]
    [InlineData("EventRemove", MethodKind.Unsupported)]
    public async Task ExtractFromSymbol_MapsMethodType(string methodType, MethodKind mapped)
    {
        string code = /*lang=c#-test*/
            $$"""
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(MethodType.{{methodType}})]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(null, null, mapped, null, null);
        Assert.Equal(expected, actual);
    }

    [Theory(DisplayName = "ExtractFromSymbol should correctly map ArgumentType")]
    [InlineData("Normal", ArgumentKind.Normal)]
    [InlineData("Ref", ArgumentKind.Ref)]
    [InlineData("Out", ArgumentKind.Out)]
    [InlineData("Pointer", ArgumentKind.Unsupported)]
    public async Task ExtractFromSymbol_MapsArgumentType(string argumentType, ArgumentKind mapped)
    {
        string code = /*lang=c#-test*/
            $$"""
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch([typeof(Patches)], [ArgumentType.{{argumentType}}])]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            null,
            null,
            null,
            ImmutableEquatableArray.Create(symbol.ContainingType),
            ImmutableEquatableArray.Create(mapped)
        );
        Assert.Equal(expected, actual);
    }

    [Fact(DisplayName = "ExtractFromSymbol should merge data from multiple attributes")]
    public async Task ExtractFromSymbol_MergesAttributes()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(Patches))]
                [HarmonyPatch(nameof(Patches.Prefix))]
                [HarmonyPatch(MethodType.Normal)]
                [HarmonyPatch([typeof(string)], [ArgumentType.Normal])]
                [|public static void Prefix(string arg)|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual = HarmonyPatchAttributeData.ExtractFromSymbol(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            "Prefix",
            MethodKind.Normal,
            ImmutableEquatableArray.Create((INamedTypeSymbol)symbol.Parameters[0].Type),
            ImmutableEquatableArray.Create(ArgumentKind.Normal)
        );
        Assert.Equal(expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromMethodWithInheritance should merge non-colliding data from class"
    )]
    public async Task ExtractFromMethodWithInheritance_MergesNonCollidingAttributesFromClass()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch(typeof(Patches))]
            class Patches
            {
                [HarmonyPatch("Foo")]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            "Foo",
            null,
            null,
            null
        );
        Assert.Equal(@expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromMethodWithInheritance should prefer data from method when a collision occurs"
    )]
    public async Task ExtractFromMethodWithInheritance_PrefersCollidingAttributesFromMethod()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch(typeof(Patches), "Bar")]
            class Patches
            {
                [HarmonyPatch("Foo")]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            "Foo",
            null,
            null,
            null
        );
        Assert.Equal(@expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromMethodWithInheritance should use data from method when class is unannotated"
    )]
    public async Task ExtractFromMethodWithInheritance_CanUseMethodDataOnly()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch("Foo")]
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(symbol);
        HarmonyPatchAttributeData expected = new(null, "Foo", null, null, null);
        Assert.Equal(@expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromMethodWithInheritance should use data from class when method is unannotated"
    )]
    public async Task ExtractFromMethodWithInheritance_CanUseClassDataOnly()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch(typeof(Patches))]
            class Patches
            {
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(symbol);
        HarmonyPatchAttributeData expected = new(
            symbol.ContainingType.ToMA(),
            null,
            null,
            null,
            null
        );
        Assert.Equal(@expected, actual);
    }

    [Fact(
        DisplayName = "ExtractFromMethodWithInheritance should return empty when both method and class are unannotated"
    )]
    public async Task ExtractFromMethodWithInheritance_Unannotated_ReturnsNull()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [|public static void Prefix()|] { }
            }
            """;
        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        HarmonyPatchAttributeData? actual =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(symbol);
        Assert.Null(actual);
    }
}
