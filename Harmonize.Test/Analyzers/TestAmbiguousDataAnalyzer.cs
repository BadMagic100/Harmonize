using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Harmonize.Test.Analyzers.Scaffolding.CSharpAnalyzerVerifier<Harmonize.Analyzers.AmbiguousDataAnalyzer>;

namespace Harmonize.Test.Analyzers;

public class TestAmbiguousDataAnalyzer
{
    [Fact(DisplayName = "should report diagnostic for ambiguous type")]
    public async Task ReportsDiagnosticForAmbiguousType()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(string))]
                [HarmonyPatch(typeof(bool))]
                public static void {|#0:Prefix|}() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousData)
            .WithLocation(0);
        await VerifyCS.VerifyAnalyzerOnlyAsync(
            source,
            [expectedDiagnostic],
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "should report diagnostic for ambiguous name")]
    public async Task ReportsDiagnosticForAmbiguousName()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch("foo")]
                [HarmonyPatch("bar")]
                public static void {|#0:Prefix|}() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousData)
            .WithLocation(0);
        await VerifyCS.VerifyAnalyzerOnlyAsync(
            source,
            [expectedDiagnostic],
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "should report diagnostic for ambiguous method type")]
    public async Task ReportsDiagnosticForAmbiguousMethodType()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(MethodType.Normal)]
                [HarmonyPatch(MethodType.Getter)]
                public static void {|#0:Prefix|}() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousData)
            .WithLocation(0);
        await VerifyCS.VerifyAnalyzerOnlyAsync(
            source,
            [expectedDiagnostic],
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "should report diagnostic for ambiguous args")]
    public async Task ReportsDiagnosticForAmbiguousArguments()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch([typeof(string)])]
                [HarmonyPatch([typeof(bool)])]
                public static void {|#0:Prefix|}() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousData)
            .WithLocation(0);
        await VerifyCS.VerifyAnalyzerOnlyAsync(
            source,
            [expectedDiagnostic],
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "should not report diagnostic for single values")]
    public async Task NoDiagnosticForSingleValues()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                [HarmonyPatch(typeof(string), "Foo", [typeof(string)], [ArgumentType.Normal])]
                [HarmonyPatch(MethodType.Normal)]
                public static void Prefix() { }
            }
            """;

        await VerifyCS.VerifyAnalyzerOnlyAsync(source, [], TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "should not report diagnostic for no values")]
    public async Task NoDiagnosticForNoData()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patches
            {
                public static void Prefix() { }
            }
            """;

        await VerifyCS.VerifyAnalyzerOnlyAsync(source, [], TestContext.Current.CancellationToken);
    }
}
