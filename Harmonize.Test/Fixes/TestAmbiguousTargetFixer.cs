using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Harmonize.Test.Fixes.Scaffolding.CSharpCodeFixVerifier<
    Harmonize.Analyzers.AmbiguousTargetAnalyzer,
    Harmonize.Fixes.AmbiguousTargetFixer
>;

namespace Harmonize.Test.Fixes;

public class TestAmbiguousTargetFixer
{
    [Fact(DisplayName = "should fix diagnostics with no existing HarmonyPatch attributes")]
    public async Task FixWithNoExistingAttribute()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                public static void {|#0:Prefix|}() { }
            }
            """;
        string fix1 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch([])]
                public static void Prefix() { }
            }
            """;
        string fix2 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch([typeof(string[])])]
                public static void Prefix() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousTarget)
            .WithLocation(0)
            .WithArguments("Prefix");
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix1,
            TestContext.Current.CancellationToken,
            "Patched.Foo()"
        );
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix2,
            TestContext.Current.CancellationToken,
            "Patched.Foo(string[])"
        );
    }

    [Fact(DisplayName = "should fix diagnostics with 1 existing HarmonyPatch attribute")]
    public async Task FixWithOneExistingAttribute()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch(typeof(Patched))]
            class Patches
            {
                [HarmonyPatch(nameof(Patched.Foo))]
                public static void {|#0:Prefix|}() { }
            }
            """;
        string fix1 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch(typeof(Patched))]
            class Patches
            {
                [HarmonyPatch(nameof(Patched.Foo), [])]
                public static void Prefix() { }
            }
            """;
        string fix2 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch(typeof(Patched))]
            class Patches
            {
                [HarmonyPatch(nameof(Patched.Foo), [typeof(string[])])]
                public static void Prefix() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousTarget)
            .WithLocation(0)
            .WithArguments("Prefix");
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix1,
            TestContext.Current.CancellationToken,
            "Patched.Foo()"
        );
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix2,
            TestContext.Current.CancellationToken,
            "Patched.Foo(string[])"
        );
    }

    [Fact(DisplayName = "should fix diagnostics with 2 or more existing HarmonyPatch attributes")]
    public async Task FixWithTwoExistingAttributes()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch(typeof(Patched))]
                [HarmonyPatch(nameof(Patched.Foo))]
                public static void {|#0:Prefix|}() { }
            }
            """;
        string fix1 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch(typeof(Patched))]
                [HarmonyPatch(nameof(Patched.Foo))]
                [HarmonyPatch([])]
                public static void Prefix() { }
            }
            """;
        string fix2 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo() { }
                public void Foo(string[] arg) { }
            }

            [HarmonyPatch]
            class Patches
            {
                [HarmonyPatch(typeof(Patched))]
                [HarmonyPatch(nameof(Patched.Foo))]
                [HarmonyPatch([typeof(string[])])]
                public static void Prefix() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousTarget)
            .WithLocation(0)
            .WithArguments("Prefix");
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix1,
            TestContext.Current.CancellationToken,
            "Patched.Foo()"
        );
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix2,
            TestContext.Current.CancellationToken,
            "Patched.Foo(string[])"
        );
    }

    [Fact(
        DisplayName = "should disambiguate between ref kinds when argument type is not specified"
    )]
    public async Task FixWithAmbiguousArgumentKindsNoType()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                public static void {|#0:Prefix|}() { }
            }
            """;
        string fix1 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch([typeof(string)], [ArgumentType.Normal])]
                public static void Prefix() { }
            }
            """;
        string fix2 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch([typeof(string)], [ArgumentType.Ref])]
                public static void Prefix() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousTarget)
            .WithLocation(0)
            .WithArguments("Prefix");
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix1,
            TestContext.Current.CancellationToken,
            "Patched.Foo(string)"
        );
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix2,
            TestContext.Current.CancellationToken,
            "Patched.Foo(ref string)"
        );
    }

    [Fact(
        DisplayName = "should disambiguate between ref kinds when argument type is specified as params"
    )]
    public async Task FixWithAmbiguousArgumentKindsWithParamsTypeArray()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(MethodType.Normal, typeof(string))]
                public static void {|#0:Prefix|}() { }
            }
            """;
        string fix1 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(MethodType.Normal, [typeof(string)], [ArgumentType.Normal])]
                public static void Prefix() { }
            }
            """;
        string fix2 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(MethodType.Normal, [typeof(string)], [ArgumentType.Ref])]
                public static void Prefix() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousTarget)
            .WithLocation(0)
            .WithArguments("Prefix");
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix1,
            TestContext.Current.CancellationToken,
            "Patched.Foo(string)"
        );
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix2,
            TestContext.Current.CancellationToken,
            "Patched.Foo(ref string)"
        );
    }

    [Fact(
        DisplayName = "should disambiguate between ref kinds when argument type is specified as array"
    )]
    public async Task FixWithAmbiguousArgumentKindsWithExplicitTypeArray()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(MethodType.Normal, [typeof(string)])]
                public static void {|#0:Prefix|}() { }
            }
            """;
        string fix1 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(MethodType.Normal, [typeof(string)], [ArgumentType.Normal])]
                public static void Prefix() { }
            }
            """;
        string fix2 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(MethodType.Normal, [typeof(string)], [ArgumentType.Ref])]
                public static void Prefix() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousTarget)
            .WithLocation(0)
            .WithArguments("Prefix");
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix1,
            TestContext.Current.CancellationToken,
            "Patched.Foo(string)"
        );
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix2,
            TestContext.Current.CancellationToken,
            "Patched.Foo(ref string)"
        );
    }

    [Fact(
        DisplayName = "should use array initializer expressions when collection expressions are not available"
    )]
    public async Task FixUsesArrayCSharp11()
    {
        string source = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                public static void {|#0:Prefix|}() { }
            }
            """;
        string fix1 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(new System.Type[] { typeof(string) }, new ArgumentType[] { ArgumentType.Normal })]
                public static void Prefix() { }
            }
            """;
        string fix2 = /*lang=c#-test*/
            """
            using HarmonyLib;

            class Patched
            {
                public void Foo(string arg) { }
                public void Foo(ref string arg) { }
            }

            [HarmonyPatch(typeof(Patched), nameof(Patched.Foo))]
            class Patches
            {
                [HarmonyPatch(new System.Type[] { typeof(string) }, new ArgumentType[] { ArgumentType.Ref })]
                public static void Prefix() { }
            }
            """;

        DiagnosticResult expectedDiagnostic = VerifyCS
            .Diagnostic(Diagnostics.AmbiguousTarget)
            .WithLocation(0)
            .WithArguments("Prefix");
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix1,
            TestContext.Current.CancellationToken,
            "Patched.Foo(string)",
            languageVersion: LanguageVersion.CSharp11
        );
        await VerifyCS.VerifyCodeFixAsync(
            source,
            [expectedDiagnostic],
            fix2,
            TestContext.Current.CancellationToken,
            "Patched.Foo(ref string)",
            languageVersion: LanguageVersion.CSharp11
        );
    }
}
