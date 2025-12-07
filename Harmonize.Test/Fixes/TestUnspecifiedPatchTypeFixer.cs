using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Harmonize.Test.Fixes.Scaffolding.CSharpCodeFixVerifier<
    Harmonize.Analyzers.UnspecifiedPatchTypeAnalyzer,
    Harmonize.Fixes.UnspecifiedPatchTypeFixer
>;

namespace Harmonize.Test.Fixes;

public class TestUnspecifiedPatchTypeFixer
{
    [Fact(DisplayName = "should provide diagnostic without fix for multiple patch types")]
    public async Task TestMultiplePatchTypes()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch]
                [HarmonyPostfix]
                public static void {|#0:Prefix|}() { }
            }
            """;
        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.UnspecifiedPatchType)
            .WithLocation(0);
        await VerifyCS.VerifyAnalyzerOnlyAsync(
            source,
            [expectedDiagnostic],
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "should apply HarmonyPrefix when no patch type provided")]
    public async Task TestPrefixWithFixAll()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch]
                public static void {|#0:Test|}() { }

                [HarmonyPatch]
                public static void {|#1:Test2|}() { }
            }
            """;
        string fixSource = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch]
                [HarmonyPrefix]
                public static void Test() { }

                [HarmonyPatch]
                [HarmonyPrefix]
                public static void Test2() { }
            }
            """;
        DiagnosticResult expectedDiagnostic0 = VerifyCS
            .Diagnostic(Diagnostics.UnspecifiedPatchType)
            .WithLocation(0);
        DiagnosticResult expectedDiagnostic1 = VerifyCS
            .Diagnostic(Diagnostics.UnspecifiedPatchType)
            .WithLocation(1);
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic0, expectedDiagnostic1],
            fixSource,
            TestContext.Current.CancellationToken,
            fixEquivalenceKey: "HarmonyPrefix"
        );
    }

    [Fact(DisplayName = "should apply HarmonyPostfix when no patch type provided")]
    public async Task TestPostfixWithFixAll()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch]
                public static void {|#0:Test|}() { }

                [HarmonyPatch]
                public static void {|#1:Test2|}() { }
            }
            """;
        string fixSource = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch]
                [HarmonyPostfix]
                public static void Test() { }

                [HarmonyPatch]
                [HarmonyPostfix]
                public static void Test2() { }
            }
            """;
        DiagnosticResult expectedDiagnostic0 = VerifyCS
            .Diagnostic(Diagnostics.UnspecifiedPatchType)
            .WithLocation(0);
        DiagnosticResult expectedDiagnostic1 = VerifyCS
            .Diagnostic(Diagnostics.UnspecifiedPatchType)
            .WithLocation(1);
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic0, expectedDiagnostic1],
            fixSource,
            TestContext.Current.CancellationToken,
            fixEquivalenceKey: "HarmonyPostfix"
        );
    }

    [Fact(DisplayName = "should apply HarmonyTranspiler when no patch type provided")]
    public async Task TestTranspilerWithFixAll()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch]
                public static void {|#0:Test|}() { }

                [HarmonyPatch]
                public static void {|#1:Test2|}() { }
            }
            """;
        string fixSource = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch]
                [HarmonyTranspiler]
                public static void Test() { }

                [HarmonyPatch]
                [HarmonyTranspiler]
                public static void Test2() { }
            }
            """;
        DiagnosticResult expectedDiagnostic0 = VerifyCS
            .Diagnostic(Diagnostics.UnspecifiedPatchType)
            .WithLocation(0);
        DiagnosticResult expectedDiagnostic1 = VerifyCS
            .Diagnostic(Diagnostics.UnspecifiedPatchType)
            .WithLocation(1);
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic0, expectedDiagnostic1],
            fixSource,
            TestContext.Current.CancellationToken,
            fixEquivalenceKey: "HarmonyTranspiler"
        );
    }
}
