using Harmonize.CompletionProviders;
using Harmonize.CompletionProviders.Injections;
using Microsoft.CodeAnalysis.Completion;

namespace Harmonize.Test.CompletionProviders;

public partial class TestHarmonyInjectionCompletionProvider
{
    public bool NotProducedByInjection<T>(CompletionItem item)
        where T : IInjection
    {
        string injectionName = typeof(T).Name;
        return !item.Properties.TryGetValue(
                HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                out string? actualName
            )
            || actualName != injectionName;
    }
}
