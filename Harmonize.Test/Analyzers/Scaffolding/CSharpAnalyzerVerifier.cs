using Harmonize.Test.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Harmonize.Test.Analyzers.Scaffolding;

internal class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        public Test()
        {
            ReferenceAssemblies = RoslynHelpers.DefaultReferences;
        }
    }

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) =>
        CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

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
}
