using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Harmonize.CompletionProviders.Injections;

public interface IInjection
{
    public bool HasCompletions(HarmonyPatchContext context);
    public ImmutableArray<CompletionItem> GetCompletions(
        HarmonyPatchContext context,
        SemanticModel semanticModel,
        TextSpan completionSpan
    );
    public CompletionDescription DescribeCompletion(CompletionItem item);
}
