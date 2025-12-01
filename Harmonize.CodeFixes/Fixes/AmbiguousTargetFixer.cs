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
public class AmbiguousTargetFixer : CodeFixProvider
{
    private SymbolDisplayFormat userDisplayShort = new(
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType
            | SymbolDisplayMemberOptions.IncludeParameters
            | SymbolDisplayMemberOptions.IncludeRef,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        parameterOptions: SymbolDisplayParameterOptions.IncludeModifiers
            | SymbolDisplayParameterOptions.IncludeType
            | SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public override ImmutableArray<string> FixableDiagnosticIds => [Diagnostics.AmbiguousTarget.Id];

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

        IMethodSymbol? symbol = model.GetDeclaredSymbol(syntax);
        if (symbol == null)
        {
            return;
        }

        HarmonyPatchAttributeData? data =
            HarmonyPatchAttributeData.ExtractFromMethodWithInheritance(symbol);
        // either not a patch, or it is but we don't have a concrete target to be useful
        if (data == null || data.TargetType == null || data.TargetType.IsAmbiguous)
        {
            return;
        }

        ImmutableEquatableArray<PatchCandidateWithChanges> suggestions =
            PatchCandidateMatcher.GetCandidates(
                data.TargetType.Value,
                data.TargetMethodName,
                data.MethodKind,
                data.Arguments
            );
        foreach (PatchCandidateWithChanges suggestion in suggestions)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Target '{suggestion.Candidate.ToDisplayString(userDisplayShort)}'",
                    createChangedDocument: c =>
                        RenderSuggestion(
                            context.Document,
                            model,
                            syntax,
                            symbol,
                            suggestion,
                            context.CancellationToken
                        ),
                    equivalenceKey: suggestion.Candidate.ToDisplayString(
                        // verbose format containing parent types and namespaces (fully qualified fails to do this for members)
                        // and parameter types
                        SymbolDisplayFormat.CSharpErrorMessageFormat
                    )
                ),
                diagnostic
            );
        }
    }

    private async Task<Document> RenderSuggestion(
        Document document,
        SemanticModel model,
        MethodDeclarationSyntax syntax,
        IMethodSymbol symbol,
        PatchCandidateWithChanges suggestion,
        CancellationToken cancellationToken
    )
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        bool supportsCollectionExpressions =
            syntax.SyntaxTree.Options is CSharpParseOptions cspo
            && cspo.LanguageVersion >= LanguageVersion.CSharp12;

        // in the context of this fix, we can assume that the attribute data itself is unambiguous
        // because this diagnostic is only triggered after resolving a HarmonyPatchContext. This means
        // the only changes recommended to us should require additions. To do an addition, we can either
        // add a new attribute, or we can add the needed arguments to an existing attribute. If there
        // is only one HarmonyPatch attribute attached, we should prefer to edit it, otherwise we add one

        List<AttributeData> existingPatchAttributes =
        [
            .. symbol
                .GetAttributes()
                .Where(attr =>
                    attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    == "global::HarmonyLib.HarmonyPatch"
                ),
        ];
        MetadataChange change = suggestion.Change;

        if (existingPatchAttributes.Count == 1)
        {
            AttributeData attr = existingPatchAttributes[0];
            AttributeSyntax originalAttributeNode = (AttributeSyntax)
                await attr.ApplicationSyntaxReference!.GetSyntaxAsync(cancellationToken);
            // we know all these values will be null or non-ambiguous because they are not merged with other attrs
            HarmonyPatchAttributeData existingData = HarmonyPatchAttributeData.ExtractFromAttribute(
                attr
            );
            AttributeSyntax newAttribute = HarmonySyntaxFactory.EditHarmonyPatchAttribute(
                editor.Generator,
                originalAttributeNode,
                model,
                existingData.TargetType?.Value,
                existingData.TargetMethodName?.Value,
                change.Name,
                existingData.MethodKind?.Value,
                change.Kind,
                existingData.Arguments?.Value,
                change.Arguments,
                supportsCollectionExpressions
            );

            editor.ReplaceNode(
                originalAttributeNode,
                newAttribute.WithAdditionalAnnotations(
                    Simplifier.Annotation,
                    Simplifier.SpecialTypeAnnotation,
                    Formatter.Annotation
                )
            );
        }
        else
        {
            // adding a new one. arguments will always be in the order: name, kind, arguments. argument types can be omitted if all are normal
            AttributeSyntax newAttribute = HarmonySyntaxFactory.CreateHarmonyPatchAttribute(
                editor.Generator,
                null,
                change.Name,
                change.Kind,
                change.Arguments,
                supportsCollectionExpressions
            );
            editor.ReplaceNode(
                syntax,
                syntax.AddAttributeLists(
                    SyntaxFactory
                        .AttributeList(SyntaxFactory.SingletonSeparatedList(newAttribute))
                        .WithAdditionalAnnotations(
                            Simplifier.Annotation,
                            Simplifier.SpecialTypeAnnotation,
                            Formatter.Annotation
                        )
                )
            );
        }
        return editor.GetChangedDocument();
    }
}
