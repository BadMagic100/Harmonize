using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Harmonize.CompletionProviders.Injections;

internal interface IInjection
{
    public string Name { get; }
    public bool HasCompletions(HarmonyContext context);
    public ImmutableArray<CompletionItem> GetCompletions(HarmonyContext ctx, SemanticModel semanticModel, TextSpan completionSpan);
    public CompletionDescription DescribeCompletion(CompletionItem item);
}
