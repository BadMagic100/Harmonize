using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
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
        SemanticModel semanticModel,
        TextSpan originalSpan
    )
    {
        string displayPrefix = context.TargetType.ToMinimalDisplayString(
            semanticModel,
            originalSpan.Start
        );
        CompletionItem item = CompletionItem
            .Create(
                $"{displayPrefix} __instance",
                tags: ImmutableArray.Create(WellKnownTags.Parameter)
            )
            .WithSortText("__instance")
            .WithFilterText("__instance");
        return ImmutableArray.Create(item);
    }

    public CompletionDescription DescribeCompletion(CompletionItem item)
    {
        return CompletionDescription.Create(
            ImmutableArray.Create(
                new TaggedText(TextTags.Text, "The instance being patched. Acts similar to "),
                new TaggedText(TextTags.Keyword, "this"),
                new TaggedText(TextTags.Text, " from within the patched method")
            )
        );
    }
}
