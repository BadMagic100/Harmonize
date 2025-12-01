using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Harmonize.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AmbiguousTargetAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.AmbiguousTarget];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics
        );
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        MethodDeclarationSyntax syntax = (MethodDeclarationSyntax)context.Node;
        HarmonyPatchContext? patchContext = HarmonyPatchContext.GetContextForNode(
            syntax,
            context.SemanticModel,
            context.CancellationToken
        );
        if (patchContext == null)
        {
            return;
        }

        if (patchContext.TargetMethod == null || patchContext.TargetMethod.IsAmbiguous)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.AmbiguousTarget,
                    Location.Create(context.FilterTree, syntax.Identifier.Span),
                    syntax.Identifier.ToString()
                )
            );
        }
    }
}
