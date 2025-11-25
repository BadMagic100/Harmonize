using Harmonize.CompletionProviders.Injections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Harmonize.CompletionProviders;

[ExportCompletionProvider(nameof(HarmonyInjectionCompletionProvider), LanguageNames.CSharp)]
public class HarmonyInjectionCompletionProvider : CompletionProvider
{
    private const string PROP_INJECTION_NAME = "InjectionName";
    private static readonly IEnumerable<IInjection> injections = [
        new InstanceInjection()
    ];
    private static readonly ImmutableDictionary<string, IInjection> injectionLookup = injections.ToImmutableDictionary(x => x.Name);

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        if (!context.Document.SupportsSemanticModel)
        {
            return;
        }

        SyntaxNode? syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (syntaxRoot == null)
        {
            return;
        }

        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
        if (semanticModel == null)
        {
            return;
        }

        // provide completions only from within a parameter list
        ParameterListSyntax? paramList = syntaxRoot.FindNode(context.CompletionListSpan).FirstAncestorOrSelf<ParameterListSyntax>();
        MethodDeclarationSyntax? decl = paramList?.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (decl == null)
        {
            return;
        }

        HarmonyPatchContext? harmonyContext = HarmonyPatchContext.GetContextForNode(decl, semanticModel, context.CancellationToken);

        if (harmonyContext == null)
        {
            return;
        }

        foreach (IInjection injection in injections)
        {
            if (!injection.HasCompletions(harmonyContext))
            {
                continue;
            }

            context.AddItems(injection.GetCompletions(harmonyContext, semanticModel, context.CompletionListSpan));
            foreach (CompletionItem item in injection.GetCompletions(harmonyContext, semanticModel, context.CompletionListSpan))
            {
                context.AddItem(item.AddProperty(PROP_INJECTION_NAME, injection.Name));
            }
        }
    }

    public override Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
    {
        string key = item.Properties[PROP_INJECTION_NAME];
        IInjection injection = injectionLookup[key];
        return Task.FromResult<CompletionDescription?>(injection.DescribeCompletion(item));
    }
}
