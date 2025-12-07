using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Harmonize.Fixes;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class UnspecifiedPatchTypeFixer : CodeFixProvider
{
    private static readonly ImmutableArray<string> PatchAttributes =
    [
        "HarmonyPrefix",
        "HarmonyPostfix",
        "HarmonyTranspiler",
    ];

    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Diagnostics.UnspecifiedPatchType.Id];

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null)
        {
            return;
        }

        SemanticModel? model = await context.Document.GetSemanticModelAsync(
            context.CancellationToken
        );
        if (model == null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan span = context.Span;
        MethodDeclarationSyntax? syntax = root.FindNode(span)
            .FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (syntax == null)
        {
            return;
        }

        IMethodSymbol? symbol = model.GetDeclaredSymbol(syntax, context.CancellationToken);
        if (symbol == null)
        {
            return;
        }

        // we can only provide useful fixes in the case where a type is not specified; we can add one.
        // if it's ambiguous we generally can't offer removals (for example, if it's inferred from the name)
        if (symbol.HasAnyPatchType())
        {
            return;
        }

        foreach (string possibleAttribute in PatchAttributes)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Apply {possibleAttribute}",
                    createChangedDocument: c =>
                        ApplyAttributeToMethod(
                            context.Document,
                            syntax,
                            $"HarmonyLib.{possibleAttribute}",
                            c
                        ),
                    equivalenceKey: possibleAttribute
                ),
                diagnostic
            );
        }
    }

    private async Task<Document> ApplyAttributeToMethod(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        string attrName,
        CancellationToken cancellationToken
    )
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);

        editor.ReplaceNode(
            methodDeclaration,
            methodDeclaration.AddAttributeLists(
                (AttributeListSyntax)
                    editor
                        .Generator.Attribute(attrName)
                        .WithAdditionalAnnotations(
                            Simplifier.Annotation,
                            Simplifier.SpecialTypeAnnotation,
                            Formatter.Annotation
                        )
            )
        );
        return editor.GetChangedDocument();
    }
}
