using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Harmonize.CompletionProviders.Injections;

internal interface IInjection
{
    public string Name { get; }
    public bool HasCompletions(HarmonyPatchContext context);
    public ImmutableArray<CompletionItem> GetCompletions(
        HarmonyPatchContext ctx,
        SemanticModel semanticModel,
        TextSpan completionSpan
    );
    public CompletionDescription DescribeCompletion(CompletionItem item);
}
