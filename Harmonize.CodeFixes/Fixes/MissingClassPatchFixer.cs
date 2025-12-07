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
public class MissingClassPatchFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Diagnostics.MissingClassPatch.Id];

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
        ClassDeclarationSyntax? syntax = root.FindNode(span)
            .FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (syntax == null)
        {
            return;
        }

        INamedTypeSymbol? symbol = model.GetDeclaredSymbol(syntax);
        if (symbol == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add HarmonyPatch attribute",
                createChangedDocument: c =>
                    AddPatchAttributeToClass(context.Document, model, syntax, c),
                equivalenceKey: nameof(MissingClassPatchFixer)
            ),
            diagnostic
        );
    }

    private async Task<Document> AddPatchAttributeToClass(
        Document document,
        SemanticModel model,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken
    )
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);

        AttributeSyntax attribute = HarmonySyntaxFactory.CreateHarmonyPatchAttribute(
            editor.Generator,
            null,
            null,
            null,
            null,
            false
        );
        editor.ReplaceNode(
            classDeclaration,
            classDeclaration.AddAttributeLists(
                SyntaxFactory
                    .AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))
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
