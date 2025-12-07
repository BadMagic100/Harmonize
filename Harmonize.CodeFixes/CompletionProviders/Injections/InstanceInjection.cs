using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Harmonize.CompletionProviders.Injections;

public class InstanceInjection : IInjection
{
    public bool HasCompletions(HarmonyPatchContext context)
    {
        return (context.PatchType == PatchType.Prefix || context.PatchType == PatchType.Postfix)
            && context.TargetMethod != null
            && !context.TargetMethod.IsAmbiguous
            && !context.TargetMethod.Value.IsStatic;
    }

    public ImmutableArray<CompletionItem> GetCompletions(
        HarmonyPatchContext context,
        ParameterSyntax syntax,
        SemanticModel semanticModel,
        TextSpan originalSpan
    )
    {
        // don't offer a completion if e.g. the user has put ref
        if (syntax.Modifiers.Any())
        {
            return [];
        }
        string displayPrefix = context.TargetType.ToMinimalDisplayString(
            semanticModel,
            originalSpan.Start
        );
        CompletionItem item = CompletionItem
            .Create($"{displayPrefix} __instance", tags: [WellKnownTags.Parameter])
            .WithSortText("__instance")
            .WithFilterText("__instance");
        return [item];
    }

    public CompletionDescription DescribeCompletion(CompletionItem item)
    {
        return CompletionDescription.Create([
            new TaggedText(TextTags.Text, "The instance being patched. Acts similar to "),
            new TaggedText(TextTags.Keyword, "this"),
            new TaggedText(TextTags.Text, " from within the patched method"),
        ]);
    }
}
