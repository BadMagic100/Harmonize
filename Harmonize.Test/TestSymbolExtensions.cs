using Harmonize.Test.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

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
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await RoslynHelpers.CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            TestContext.Current.CancellationToken
        );

        IMethodSymbol symbol = await RoslynHelpers.AssertSpanIsMethodAndGetAsync(
            doc,
            span,
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
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await RoslynHelpers.CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            TestContext.Current.CancellationToken
        );

        IMethodSymbol symbol = await RoslynHelpers.AssertSpanIsMethodAndGetAsync(
            doc,
            span,
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
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await RoslynHelpers.CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            TestContext.Current.CancellationToken
        );

        IMethodSymbol symbol = await RoslynHelpers.AssertSpanIsMethodAndGetAsync(
            doc,
            span,
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
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await RoslynHelpers.CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            TestContext.Current.CancellationToken
        );

        IMethodSymbol symbol = await RoslynHelpers.AssertSpanIsMethodAndGetAsync(
            doc,
            span,
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
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await RoslynHelpers.CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            TestContext.Current.CancellationToken
        );

        IMethodSymbol symbol = await RoslynHelpers.AssertSpanIsMethodAndGetAsync(
            doc,
            span,
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
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await RoslynHelpers.CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            TestContext.Current.CancellationToken
        );

        IMethodSymbol symbol = await RoslynHelpers.AssertSpanIsMethodAndGetAsync(
            doc,
            span,
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
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await RoslynHelpers.CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            TestContext.Current.CancellationToken
        );

        IMethodSymbol symbol = await RoslynHelpers.AssertSpanIsMethodAndGetAsync(
            doc,
            span,
            TestContext.Current.CancellationToken
        );
        PatchType actual = symbol.GetPatchType();
        Assert.Equal(PatchType.Unknown, actual);
    }
}
