using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Harmonize.CompletionProviders.Injections;

public class ArgumentInjection : IInjection
{
    public const string PROP_PARAMETER_NAME = "ParameterName";

    public bool HasCompletions(HarmonyPatchContext context)
    {
        return (context.PatchType == PatchType.Prefix || context.PatchType == PatchType.Postfix)
            && context.TargetMethod != null
            && !context.TargetMethod.IsAmbiguous;
    }

    public ImmutableArray<CompletionItem> GetCompletions(
        HarmonyPatchContext context,
        SemanticModel semanticModel,
        TextSpan completionSpan
    )
    {
        IMethodSymbol target = context.TargetMethod!.Value;
        ImmutableArray<CompletionItem>.Builder builder =
            ImmutableArray.CreateBuilder<CompletionItem>();
        for (int i = 0; i < target.Parameters.Length; i++)
        {
            IParameterSymbol param = target.Parameters[i];
            string type;
            if (param.Type is INamedTypeSymbol ns)
            {
                type = ns.ToMinimalDisplayString(semanticModel, completionSpan.Start);
            }
            else
            {
                type = "object";
            }
            CompletionItem baseCompletion = CompletionItem
                .Create(null, tags: ImmutableArray.Create(WellKnownTags.Parameter))
                .AddProperty(PROP_PARAMETER_NAME, param.Name);
            builder.Add(
                baseCompletion
                    .WithDisplayText($"{type} __{i}")
                    .WithSortText($"__{i}")
                    .WithFilterText($"__{i}")
            );
            builder.Add(
                baseCompletion
                    .WithDisplayText($"{type} {param.Name}")
                    .WithSortText($"_{param.Name}")
                    .WithFilterText($"_{param.Name}")
            );
        }
        return builder.ToImmutable();
    }

    public CompletionDescription DescribeCompletion(CompletionItem item)
    {
        return CompletionDescription.Create(
            ImmutableArray.Create(
                new TaggedText(TextTags.Text, "The "),
                new TaggedText(TextTags.Parameter, item.Properties[PROP_PARAMETER_NAME]),
                new TaggedText(TextTags.Text, " parameter")
            )
        );
    }
}
