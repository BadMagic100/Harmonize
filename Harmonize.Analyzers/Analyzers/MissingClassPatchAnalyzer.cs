using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Harmonize.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingClassPatchAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.MissingClassPatch];

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

        HarmonyPatchAttributeData? methodData = HarmonyPatchAttributeData.ExtractFromSymbol(method);
        if (methodData == null)
        {
            return;
        }

        HarmonyPatchAttributeData? classData = HarmonyPatchAttributeData.ExtractFromSymbol(
            method.ContainingType
        );
        if (classData == null)
        {
            Diagnostic diag = Diagnostic.Create(
                Diagnostics.MissingClassPatch,
                Location.Create(context.FilterTree, syntax.Identifier.Span)
            );
            context.ReportDiagnostic(diag);
        }
    }
}
