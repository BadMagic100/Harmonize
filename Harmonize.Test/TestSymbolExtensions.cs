using Harmonize.Test.Helper;
using Microsoft.CodeAnalysis;

namespace Harmonize.Test;

public class TestSymbolExtensions
{
    [Fact(DisplayName = "GetPatchType should return Prefix when method is named Prefix")]
    public async Task GetPatchType_ReturnsPrefixByName()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patched
            {
                [HarmonyPostfix] // ignored
                [|public void Prefix()|] { }
            }
            """;

        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Prefix, actual);
    }

    [Fact(DisplayName = "GetPatchType should return Postfix when method is named Postfix")]
    public async Task GetPatchType_ReturnsPostfixByName()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patched
            {
                [HarmonyPrefix] // ignored
                [|public void Postfix()|] { }
            }
            """;

        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Postfix, actual);
    }

    [Fact(DisplayName = "GetPatchType should return Transpiler when method is named Transpiler")]
    public async Task GetPatchType_ReturnsTranspilerByName()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patched
            {
                [HarmonyPostfix] // ignored
                [|public void Transpiler()|] { }
            }
            """;

        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Transpiler, actual);
    }

    [Fact(DisplayName = "GetPatchType should return Prefix when annotated with HarmonyPrefix")]
    public async Task GetPatchType_ReturnsPrefixByAttribute()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patched
            {
                [HarmonyPrefix]
                [|public void MyCustomName()|] { }
            }
            """;

        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Prefix, actual);
    }

    [Fact(DisplayName = "GetPatchType should return Postfix when annotated with HarmonyPostfix")]
    public async Task GetPatchType_ReturnsPostfixByAttribute()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patched
            {
                [HarmonyPostfix]
                [|public void MyCustomName()|] { }
            }
            """;

        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Postfix, actual);
    }

    [Fact(
        DisplayName = "GetPatchType should return Transpiler when annotated with HarmonyTranspiler"
    )]
    public async Task GetPatchType_ReturnsTranspilerByAttribute()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patched
            {
                [HarmonyTranspiler]
                [|public void MyCustomName()|] { }
            }
            """;

        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Transpiler, actual);
    }

    [Fact(
        DisplayName = "GetPatchType should return Unknown when not annotated or named appropriately"
    )]
    public async Task GetPatchType_ReturnsUnknownWhenNotLabeled()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patched
            {
                [|public void MyCustomName()|] { }
            }
            """;

        IMethodSymbol symbol = await RoslynHelpers.GetMethodSymbolFromSourceAsync(
            code,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Unknown, actual);
    }
}
