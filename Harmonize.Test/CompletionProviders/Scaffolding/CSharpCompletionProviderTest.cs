using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Testing;

namespace Harmonize.Test.CompletionProviders.Scaffolding;

internal class CSharpCompletionProviderTest<T>
    where T : CompletionProvider
{
    public async Task ExpectCompletions(
        string testCode,
        IEnumerable<CompletionItem> expectedCompletions,
        CancellationToken cancellationToken
    )
    {
        void Assertion(CompletionList results)
        {
            List<(CompletionItem expected, CompletionItem? bestMatch)> missingItems = [];
            foreach (CompletionItem expectedItem in expectedCompletions)
            {
                bool found = false;
                CompletionItem? bestMatch = null;
                foreach (CompletionItem actualItem in results.ItemsList)
                {
                    // currently ignores rules because it looks annoying to handle and I don't use it yet
                    if (
                        expectedItem.DisplayText == actualItem.DisplayText
                        && expectedItem.FilterText == actualItem.FilterText
                        && expectedItem.SortText == actualItem.SortText
                        && PropsEqual(expectedItem.Properties, actualItem.Properties)
                        && TagsEqual(expectedItem.Tags, actualItem.Tags)
                        && expectedItem.DisplayTextPrefix == actualItem.DisplayTextPrefix
                        && expectedItem.DisplayTextSuffix == actualItem.DisplayTextSuffix
                        && expectedItem.InlineDescription == actualItem.InlineDescription
                        && expectedItem.IsComplexTextEdit == actualItem.IsComplexTextEdit
                    )
                    {
                        found = true;
                        break;
                    }
                    else if (expectedItem.DisplayText == actualItem.DisplayText)
                    {
                        bestMatch = actualItem;
                    }
                }
                if (!found)
                {
                    missingItems.Add((expectedItem, bestMatch));
                }
            }
            if (missingItems.Count != 0)
            {
                StringBuilder message = new();
                foreach ((CompletionItem expectedItem, CompletionItem? bestMatch) in missingItems)
                {
                    message.Append("  Expected: ");
                    message.Append(FormatItem(expectedItem));
                    message.Append('.');
                    if (bestMatch != null)
                    {
                        message.AppendLine();
                        message.Append("Best match: ");
                        message.Append(FormatItem(bestMatch));
                        message.Append('.');
                    }
                    message.AppendLine();
                    message.AppendLine();
                }
                Assert.Fail(message.ToString());
            }
        }
        await RunAsync(testCode, Assertion, cancellationToken);
    }

    public async Task ExpectCompletionsMatching(
        string testCode,
        Predicate<CompletionItem> predicate,
        CancellationToken cancellationToken
    )
    {
        void Assertion(CompletionList results)
        {
            StringBuilder message = new();
            foreach (CompletionItem item in results.ItemsList)
            {
                if (!predicate(item))
                {
                    message.Append("Expected to match predicate, but did not: ");
                    message.Append(FormatItem(item));
                    message.Append('.');
                    message.AppendLine();
                }
            }
            if (message.Length > 0)
            {
                Assert.Fail(message.ToString());
            }
        }
        await RunAsync(testCode, Assertion, cancellationToken);
    }

    public async Task RunAsync(
        string testCode,
        Action<CompletionList> assertion,
        CancellationToken cancellationToken
    )
    {
        int pos = testCode.IndexOf("{|#0:|}");
        testCode = testCode.Replace("{|#0:|}", "");

        MefHostServices host = MefHostServices.Create([
            .. MefHostServices.DefaultAssemblies,
            typeof(T).Assembly,
        ]);
        Document doc = await CreateDocumentAsync(testCode, host, cancellationToken);
        CompletionService svc = CompletionService.GetService(doc)!;
        CompletionList results = await svc.GetCompletionsAsync(
            doc,
            pos,
            cancellationToken: cancellationToken
        );
        assertion(results);
    }

    private bool PropsEqual(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual
    )
    {
        return expected.Count == actual.Count
            && expected.All(kv =>
                actual.TryGetValue(kv.Key, out string? value) && kv.Value == value
            );
    }

    private bool TagsEqual(ImmutableArray<string> expected, ImmutableArray<string> actual)
    {
        return expected.Length == actual.Length && !expected.Except(actual).Any();
    }

    private string FormatItem(CompletionItem item)
    {
        return $"`{item.DisplayTextPrefix}{item.DisplayText}{item.DisplayTextSuffix}`, "
            + $"FilterText=`{item.FilterText}`, SortText=`{item.SortText}`, "
            + $"Tags=[{string.Join(", ", item.Tags)}], "
            + $"Properties={{{string.Join(", ", item.Properties.Select(x => $"{x.Key}={x.Value}"))}}}";
    }

    private async Task<Document> CreateDocumentAsync(
        string code,
        MefHostServices host,
        CancellationToken cancellationToken
    )
    {
        ReferenceAssemblies refs = ReferenceAssemblies.Default.AddPackages([
            new PackageIdentity("Lib.Harmony", "2.4.2"),
        ]);
        CSharpCompilationOptions options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary
        );

        Project project = new AdhocWorkspace(host)
            .AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(options)
            .AddMetadataReferences(
                await refs.ResolveAsync(LanguageNames.CSharp, cancellationToken)
            );

        Document doc = project.AddDocument("TestDocument", code);
        return doc;
    }
}
