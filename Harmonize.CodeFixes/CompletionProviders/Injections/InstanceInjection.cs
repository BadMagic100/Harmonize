using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Harmonize.CompletionProviders.Injections;

internal class InstanceInjection : IInjection
{
    public string Name => nameof(InstanceInjection);

    public bool HasCompletions(HarmonyContext context)
    {
        return context.TargetMethod != null && !context.TargetMethod.IsStatic;
    }

    public ImmutableArray<CompletionItem> GetCompletions(HarmonyContext ctx, SemanticModel semanticModel, TextSpan originalSpan)
    {
        string displayPrefix = QualifiedNameHelper.GetMinimallyQualifiedTypeName(ctx.TargetType!, semanticModel, originalSpan);
        CompletionItem item = CompletionItem.Create($"{displayPrefix} __instance", tags: ImmutableArray.Create(WellKnownTags.Parameter))
            .WithSortText("__instance")
            .WithFilterText("__instance");
        return ImmutableArray.Create(item);
    }

    public CompletionDescription DescribeCompletion(CompletionItem item)
    {
        return CompletionDescription.Create(ImmutableArray.Create(
            new TaggedText(TextTags.Text, "The instance being patched. Acts similar to "),
            new TaggedText(TextTags.Keyword, "this"),
            new TaggedText(TextTags.Text, " from within the patched method")
        ));
    }
}
