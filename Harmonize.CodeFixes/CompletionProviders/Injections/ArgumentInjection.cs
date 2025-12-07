using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        ParameterSyntax syntax,
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
            string type = param.Type.ToMinimalDisplayString(semanticModel, completionSpan.Start);
            CompletionItem baseCompletion = CompletionItem
                .Create(null, tags: [WellKnownTags.Parameter])
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

            if (!syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword)))
            {
                builder.Add(
                    baseCompletion
                        .WithDisplayText($"ref {type} __{i}")
                        .WithSortText($"__{i}")
                        .WithFilterText($"__{i}")
                );
                builder.Add(
                    baseCompletion
                        .WithDisplayText($"ref {type} {param.Name}")
                        .WithSortText($"_{param.Name}")
                        .WithFilterText($"_{param.Name}")
                );
            }
        }
        return builder.ToImmutable();
    }

    public CompletionDescription DescribeCompletion(CompletionItem item)
    {
        return CompletionDescription.Create([
            new TaggedText(TextTags.Text, "The "),
            new TaggedText(TextTags.Parameter, item.Properties[PROP_PARAMETER_NAME]),
            new TaggedText(TextTags.Text, " parameter"),
        ]);
    }
}
