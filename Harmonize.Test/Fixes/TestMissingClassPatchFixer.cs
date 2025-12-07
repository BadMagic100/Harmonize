using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Harmonize.Test.Fixes.Scaffolding.CSharpCodeFixVerifier<
    Harmonize.Analyzers.MissingClassPatchAnalyzer,
    Harmonize.Fixes.MissingClassPatchFixer
>;

namespace Harmonize.Test.Fixes;

public class TestMissingClassPatchFixer
{
    [Fact(
        DisplayName = "should apply HarmonyPatch attribute to class iff present on method and missing on class"
    )]
    public async Task TestFix()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches1
            {
                [HarmonyPatch]
                public static void {|#0:Test|}() { }
            }

            class Patches2
            {
                public static void Test() { }
            }

            [HarmonyPatch]
            class Patches3
            {
                [HarmonyPatch]
                public static void Test() { }
            }
            """;
        string fixSource = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches1
            {
                [HarmonyPatch]
                public static void Test() { }
            }

            class Patches2
            {
                public static void Test() { }
            }

            [HarmonyPatch]
            class Patches3
            {
                [HarmonyPatch]
                public static void Test() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.MissingClassPatch)
            .WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fixSource,
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "should fix all correctly")]
    public async Task TestFixAll()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches1
            {
                [HarmonyPatch]
                public static void {|#0:Test|}() { }

                [HarmonyPatch]
                public static void {|#1:Test2|}() { }
            }

            class Patches2
            {
                [HarmonyPatch]
                public static void {|#2:Test|}() { }
            }

            class Patches3
            {
                [HarmonyPatch]
                public static void {|#3:Test|}() { }
            }
            """;
        string fixSource = /*lang=c#-test*/
            """
            using HarmonyLib;

            [HarmonyPatch]
            class Patches1
            {
                [HarmonyPatch]
                public static void Test() { }

                [HarmonyPatch]
                public static void Test2() { }
            }

            [HarmonyPatch]
            class Patches2
            {
                [HarmonyPatch]
                public static void Test() { }
            }

            [HarmonyPatch]
            class Patches3
            {
                [HarmonyPatch]
                public static void Test() { }
            }
            """;

        DiagnosticResult expectedDiagnostic0 = VerifyCS
            .Diagnostic(Diagnostics.MissingClassPatch)
            .WithLocation(0);
        DiagnosticResult expectedDiagnostic1 = VerifyCS
            .Diagnostic(Diagnostics.MissingClassPatch)
            .WithLocation(1);
        DiagnosticResult expectedDiagnostic2 = VerifyCS
            .Diagnostic(Diagnostics.MissingClassPatch)
            .WithLocation(2);
        DiagnosticResult expectedDiagnostic3 = VerifyCS
            .Diagnostic(Diagnostics.MissingClassPatch)
            .WithLocation(3);
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic0, expectedDiagnostic1, expectedDiagnostic2, expectedDiagnostic3],
            fixSource,
            TestContext.Current.CancellationToken
        );
    }
}
