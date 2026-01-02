using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Harmonize.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnspecifiedPatchTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.UnspecifiedPatchType];

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

        IMethodSymbol? method = context.SemanticModel.GetDeclaredSymbol(syntax);
        if (method == null)
        {
            return;
        }

        HarmonyPatchAttributeData? mergedData =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(method);
        if (mergedData == null)
        {
            return;
        }

        PatchType type = method.GetPatchType();
        if (type != PatchType.Unknown)
        {
            return;
        }

        // see if this method is used as a helper from with the patch class.
        // usage from outside the patch class cannot be detected because SymbolFinder
        // requires the workspace API which is not available to CLI builds.
        IEnumerable<InvocationExpressionSyntax> invocations =
            syntax
                .FirstAncestorOrSelf<ClassDeclarationSyntax>()
                ?.DescendantNodes()
                .OfType<InvocationExpressionSyntax>() ?? [];
        foreach (InvocationExpressionSyntax invocation in invocations)
        {
            SymbolInfo calledSymbol = context.SemanticModel.GetSymbolInfo(
                invocation,
                context.CancellationToken
            );
            if (
                calledSymbol.Symbol != null
                && calledSymbol.Symbol.Equals(method, SymbolEqualityComparer.Default)
            )
            {
                return;
            }
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.UnspecifiedPatchType,
                Location.Create(context.FilterTree, syntax.Identifier.Span)
            )
        );
    }
}
