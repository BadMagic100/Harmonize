using Harmonize.CompletionProviders;
using Harmonize.CompletionProviders.Injections;
using Harmonize.Test.CompletionProviders.Scaffolding;
using Harmonize.Test.Helper;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

namespace Harmonize.Test.CompletionProviders;

public class TestInstanceInjection
{
    [Fact(DisplayName = "InstanceInjection should provide injection for instance method")]
    public async Task InstanceInjection_ProvidesInjectionForInstanceMethod()
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
                [HarmonyPrefix]
                public static void PatchMePrefix(_$$) { }
            }
            """;

        List<CompletionItem> expectedCompletions =
        [
            CompletionItem
                .Create("Patched __instance", "__instance", "__instance")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(InstanceInjection)
                ),
        ];

        CSharpCompletionProviderTest<HarmonyInjectionCompletionProvider> test = new();
        await test.ExpectCompletions(
            code,
            expectedCompletions,
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "InstanceInjection should use fully qualified name when necessary")]
    public async Task InstanceInjection_UsesFullyQualifiedName()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            namespace Foo
            {
                public class Patched
                {
                    public void PatchMe() { }
                }
            }

            [HarmonyPatch]
            public static class Patches
            {
                [HarmonyPatch(typeof(Foo.Patched), nameof(Foo.Patched.PatchMe))]
                [HarmonyPrefix]
                public static void PatchMePrefix(_$$) { }
            }
            """;

        List<CompletionItem> expectedCompletions =
        [
            CompletionItem
                .Create("Foo.Patched __instance", "__instance", "__instance")
                .AddTag(WellKnownTags.Parameter)
                .AddProperty(
                    HarmonyInjectionCompletionProvider.PROP_INJECTION_NAME,
                    nameof(InstanceInjection)
                ),
        ];

        CSharpCompletionProviderTest<HarmonyInjectionCompletionProvider> test = new();
        await test.ExpectCompletions(
            code,
            expectedCompletions,
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "InstanceInjection should not provide injection for static methods")]
    public async Task InstanceInjection_NoInjectionForStaticMethod()
    {
        string code = /*lang=c#-test*/
            """
            using HarmonyLib;

            public class Patched
            {
                public static void PatchMe() { }
            }

            [HarmonyPatch]
            public static class Patches
            {
                [HarmonyPatch(typeof(Patched), nameof(Patched.PatchMe))]
                [HarmonyPrefix]
                public static void PatchMePrefix(_$$) { }
            }
            """;

        CSharpCompletionProviderTest<HarmonyInjectionCompletionProvider> test = new();
        await test.ExpectCompletionsMatching(
            code,
            InjectionTestHelper.NotProducedByInjection<InstanceInjection>,
            TestContext.Current.CancellationToken
        );
    }

    [Fact(DisplayName = "InstanceInjection should not provide injection for Transpiler patches")]
    public async Task InstanceInjection_NoInjectionForTranspiler()
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
            InjectionTestHelper.NotProducedByInjection<InstanceInjection>,
            TestContext.Current.CancellationToken
        );
    }
}
