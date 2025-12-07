using Harmonize.Test.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Harmonize.Test.Fixes.Scaffolding;

internal class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    private class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        public Test()
        {
            ReferenceAssemblies = RoslynHelpers.DefaultReferences;
        }
    }

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) =>
        CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

    public static async Task VerifyAnalyzerOnlyAsync(
        string source,
        DiagnosticResult[] expected,
        CancellationToken cancellationToken,
        LanguageVersion languageVersion = LanguageVersion.Default
    )
    {
        Test test = new() { TestCode = source };
        test.SolutionTransforms.Add(
            (sln, proj) =>
            {
                return sln.WithProjectParseOptions(
                    proj,
                    CSharpParseOptions.Default.WithLanguageVersion(languageVersion)
                );
            }
        );

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    public static async Task VerifyCodeFixAsync(
        string source,
        DiagnosticResult[] expected,
        string fixedSource,
        CancellationToken cancellationToken,
        string? fixEquivalenceKey = null,
        LanguageVersion languageVersion = LanguageVersion.Default
    )
    {
        Test test = new()
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionEquivalenceKey = fixEquivalenceKey,
        };
        test.SolutionTransforms.Add(
            (sln, proj) =>
            {
                return sln.WithProjectParseOptions(
                    proj,
                    CSharpParseOptions.Default.WithLanguageVersion(languageVersion)
                );
            }
        );

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(cancellationToken);
    }
}
