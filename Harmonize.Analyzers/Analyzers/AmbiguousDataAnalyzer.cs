using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Harmonize.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AmbiguousDataAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.AmbiguousData];

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

        IMethodSymbol? method = context.SemanticModel.GetDeclaredSymbol(
            syntax,
            context.CancellationToken
        );
        if (method == null)
        {
            return;
        }

        HarmonyPatchAttributeData? data =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(method);
        if (data == null)
        {
            return;
        }

        if (
            data.TargetType?.IsAmbiguous == true
            || data.TargetMethodName?.IsAmbiguous == true
            || data.MethodKind?.IsAmbiguous == true
            || data.Arguments?.IsAmbiguous == true
        )
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.AmbiguousData,
                    Location.Create(context.FilterTree, syntax.Identifier.Span)
                )
            );
        }
    }
}
