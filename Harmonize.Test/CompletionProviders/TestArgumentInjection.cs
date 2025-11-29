using Harmonize.CompletionProviders;
using Harmonize.CompletionProviders.Injections;
using Harmonize.Test.CompletionProviders.Scaffolding;
using Harmonize.Test.Helper;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

namespace Harmonize.Test.CompletionProviders;

public class TestArgumentInjection
{
    [Fact(DisplayName = "ArgumentInjection should provide injections for methods with arguments")]
    public async Task ArgumentInjection_HappyPath()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;
            using System.Collections.Generic;

            public class Patched
            {
                public void PatchMe(string str, List<string> list) { }
            }

            [HarmonyPatch]
            public static class Patches
            {
                [HarmonyPatch(typeof(Patched), nameof(Patched.PatchMe))]
                [HarmonyPrefix]
                public static void PatchMePrefix(_$$) { }
            }
            """;

        List<CompletionItem> expectedCompletions =
        [
            CompletionItem
                .Create("string str", "_str", "_str")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(ArgumentInjection)
                )
                .AddProperty(ArgumentInjection.PROP_PARAMETER_NAME, "str"),
            CompletionItem
                .Create("string __0", "__0", "__0")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(ArgumentInjection)
                )
                .AddProperty(ArgumentInjection.PROP_PARAMETER_NAME, "str"),
            CompletionItem
                .Create("List<string> list", "_list", "_list")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(ArgumentInjection)
                )
                .AddProperty(ArgumentInjection.PROP_PARAMETER_NAME, "list"),
            CompletionItem
                .Create("List<string> __1", "__1", "__1")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(ArgumentInjection)
                )
                .AddProperty(ArgumentInjection.PROP_PARAMETER_NAME, "list"),
        ];

        CSharpCompletionProviderTest<HarmonyInjectionCompletionProvider> test = new();
        await test.ExpectCompletions(
            code,
            expectedCompletions,
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "ArgumentInjection should use fully qualified names when necessary")]
    public async Task ArgumentInjection_UsesFullyQualifiedName()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            public class Patched
            {
                public void PatchMe(System.Collections.Generic.List<string> list) { }
            }

            [HarmonyPatch]
            public static class Patches
            {
                [HarmonyPatch(typeof(Patched), nameof(Patched.PatchMe))]
                [HarmonyPrefix]
                public static void PatchMePrefix(_$$) { }
            }
            """;

        List<CompletionItem> expectedCompletions =
        [
            CompletionItem
                .Create("System.Collections.Generic.List<string> list", "_list", "_list")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(ArgumentInjection)
                )
                .AddProperty(ArgumentInjection.PROP_PARAMETER_NAME, "list"),
            CompletionItem
                .Create("System.Collections.Generic.List<string> __0", "__0", "__0")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(ArgumentInjection)
                )
                .AddProperty(ArgumentInjection.PROP_PARAMETER_NAME, "list"),
        ];

        CSharpCompletionProviderTest<HarmonyInjectionCompletionProvider> test = new();
        await test.ExpectCompletions(
            code,
            expectedCompletions,
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "ArgumentInjection should not provide injections for no-arg methods")]
    public async Task ArgumentInjection_NoInjectionForNoArgs()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            public class Patched
            {
                public void PatchMe() { }
            }

            [HarmonyPatch]
            public static class Patches
            {
                [HarmonyPatch(typeof(Patched), nameof(Patched.PatchMe))]
                [HarmonyTranspiler]
                public static void PatchMeTranspiler(_$$) { }
            }
            """;

        CSharpCompletionProviderTest<HarmonyInjectionCompletionProvider> test = new();
        await test.ExpectCompletionsMatching(
            code,
            InjectionTestHelper.NotProducedByInjection<ArgumentInjection>,
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "ArgumentInjection should not provide injection for Transpiler patches")]
    public async Task ArgumentInjection_NoInjectionForTranspiler()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            public class Patched
            {
                public void PatchMe(string str) { }
            }

            [HarmonyPatch]
            public static class Patches
            {
                [HarmonyPatch(typeof(Patched), nameof(Patched.PatchMe))]
                [HarmonyTranspiler]
                public static void PatchMeTranspiler(_$$) { }
            }
            """;

        CSharpCompletionProviderTest<HarmonyInjectionCompletionProvider> test = new();
        await test.ExpectCompletionsMatching(
            code,
            InjectionTestHelper.NotProducedByInjection<ArgumentInjection>,
            TestContext.Current.CancellationToken
        );
    }
}
