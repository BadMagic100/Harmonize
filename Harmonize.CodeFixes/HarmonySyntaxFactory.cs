using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Harmonize;

internal static class HarmonySyntaxFactory
{
    public static AttributeSyntax EditHarmonyPatchAttribute(
        SyntaxGenerator generator,
        AttributeSyntax originalNode,
        SemanticModel model,
        INamedTypeSymbol? existingDeclaredType,
        string? existingName,
        string? newName,
        MethodKind? existingKind,
        MethodKind? newKind,
        ImmutableEquatableArray<ArgumentDescriptor>? existingArguments,
        ImmutableEquatableArray<ArgumentDescriptor>? newArguments,
        bool useCollectionExpressions
    )
    {
        List<ExpressionSyntax> attrArgs = [];

        // we want to preserve the existing syntax nodes where we can because the user
        // may have stuff like nameof that is sensible to preserve and hard to generate
        int existingCtorIndex = 0;
        if (existingDeclaredType != null)
        {
            attrArgs.Add(originalNode.ArgumentList!.Arguments[existingCtorIndex++].Expression);
        }

        if (existingName != null)
        {
            attrArgs.Add(originalNode.ArgumentList!.Arguments[existingCtorIndex++].Expression);
        }
        else if (newName != null)
        {
            attrArgs.Add((ExpressionSyntax)generator.LiteralExpression(newName));
        }

        if (existingKind != null)
        {
            attrArgs.Add(originalNode.ArgumentList!.Arguments[existingCtorIndex++].Expression);
        }
        else if (newKind != null)
        {
            attrArgs.Add(CreateMethodType(generator, newKind.Value));
        }

        if (existingArguments != null)
        {
            // special case - argumentdescriptor encompasses 2 arguments, so it's possible we still have to render
            // a change here. It's also the case that the existing data can contain a `params Type[]` in which case we would need
            // to re-package it as an array before adding the new argument

            if (
                existingArguments.All(d => d.Kind == null)
                && newArguments != null
                && newArguments.Any(d => d.Kind != null)
            )
            {
                // check the arg at the current index, what type is it
                ExpressionSyntax expr = originalNode
                    .ArgumentList!
                    .Arguments[existingCtorIndex]
                    .Expression;
                TypeInfo exprType = model.GetTypeInfo(expr);
                if (exprType.ConvertedType is IArrayTypeSymbol)
                {
                    // great, already an array, so we just take it as is
                    attrArgs.Add(
                        originalNode.ArgumentList!.Arguments[existingCtorIndex++].Expression
                    );
                }
                else
                {
                    // it's the params so we have to repackage as an array
                    List<ExpressionSyntax> repackaged = [];
                    for (
                        ;
                        existingCtorIndex < originalNode.ArgumentList!.Arguments.Count;
                        existingCtorIndex++
                    )
                    {
                        repackaged.Add(
                            originalNode.ArgumentList!.Arguments[existingCtorIndex].Expression
                        );
                    }
                    attrArgs.Add(
                        CreateSimplifiedArrayExpresion(
                            "System.Type",
                            repackaged,
                            generator,
                            useCollectionExpressions
                        )
                    );
                }
                // have to add the kinds regardless of how we came up with the types
                IEnumerable<ExpressionSyntax> kindArrayMembers = newArguments.Select(x =>
                    CreateArgumentType(generator, x.Kind.GetValueOrDefault(ArgumentKind.Normal))
                );
                attrArgs.Add(
                    CreateSimplifiedArrayExpresion(
                        "HarmonyLib.ArgumentType",
                        kindArrayMembers,
                        generator,
                        useCollectionExpressions
                    )
                );
            }
            else
            {
                // it's not the edge case, so we can just rip all the arguments
                for (
                    ;
                    existingCtorIndex < originalNode.ArgumentList!.Arguments.Count;
                    existingCtorIndex++
                )
                {
                    attrArgs.Add(
                        originalNode.ArgumentList!.Arguments[existingCtorIndex].Expression
                    );
                }
            }
        }
        else if (newArguments != null)
        {
            IEnumerable<ExpressionSyntax> typeArrayMembers = newArguments.Select(x =>
                (ExpressionSyntax)generator.TypeOfExpression(generator.TypeExpression(x.Type))
            );
            attrArgs.Add(
                CreateSimplifiedArrayExpresion(
                    "System.Type",
                    typeArrayMembers,
                    generator,
                    useCollectionExpressions
                )
            );

            if (newArguments.Any(a => a.Kind != null))
            {
                SyntaxNode enumRoot = ParseName("HarmonyLib.ArgumentType");
                IEnumerable<ExpressionSyntax> kindArrayMembers = newArguments.Select(x =>
                    CreateArgumentType(generator, x.Kind.GetValueOrDefault(ArgumentKind.Normal))
                );
                attrArgs.Add(
                    CreateSimplifiedArrayExpresion(
                        "HarmonyLib.ArgumentType",
                        kindArrayMembers,
                        generator,
                        useCollectionExpressions
                    )
                );
            }
        }

        return Attribute(
            originalNode.Name,
            attrArgs.Count == 0
                ? null
                : AttributeArgumentList(SeparatedList(attrArgs.Select(AttributeArgument)))
        );
    }

    /// <summary>
    /// Synthesizes an attribute declaration node for a HarmonyPatch attribute. It is the caller's responsibility
    /// to provide a combination of arguments that produces a valid constructor.
    /// </summary>
    public static AttributeSyntax CreateHarmonyPatchAttribute(
        SyntaxGenerator generator,
        INamedTypeSymbol? declaringType,
        string? name,
        MethodKind? kind,
        ImmutableEquatableArray<ArgumentDescriptor>? arguments,
        bool useCollectionExpressions
    )
    {
        List<ExpressionSyntax> attrArgs = [];
        if (declaringType != null)
        {
            attrArgs.Add(
                (ExpressionSyntax)
                    generator.TypeOfExpression(generator.TypeExpression(declaringType))
            );
        }
        if (name != null)
        {
            attrArgs.Add((ExpressionSyntax)generator.LiteralExpression(name));
        }
        if (kind != null)
        {
            attrArgs.Add(CreateMethodType(generator, kind.Value));
        }
        if (arguments != null)
        {
            IEnumerable<ExpressionSyntax> typeArrayMembers = arguments.Select(x =>
                (ExpressionSyntax)generator.TypeOfExpression(generator.TypeExpression(x.Type))
            );
            attrArgs.Add(
                CreateSimplifiedArrayExpresion(
                    "System.Type",
                    typeArrayMembers,
                    generator,
                    useCollectionExpressions
                )
            );

            if (arguments.Any(a => a.Kind != null))
            {
                SyntaxNode enumRoot = ParseName("HarmonyLib.ArgumentType");
                IEnumerable<ExpressionSyntax> kindArrayMembers = arguments.Select(x =>
                    CreateArgumentType(generator, x.Kind.GetValueOrDefault(ArgumentKind.Normal))
                );
                attrArgs.Add(
                    CreateSimplifiedArrayExpresion(
                        "HarmonyLib.ArgumentType",
                        kindArrayMembers,
                        generator,
                        useCollectionExpressions
                    )
                );
            }
        }

        return Attribute(
            ParseName("HarmonyLib.HarmonyPatch"),
            attrArgs.Count == 0
                ? null
                : AttributeArgumentList(SeparatedList(attrArgs.Select(AttributeArgument)))
        );
    }

    private static ExpressionSyntax CreateMethodType(SyntaxGenerator generator, MethodKind kind)
    {
        SyntaxNode enumRoot = ParseName("HarmonyLib.MethodType");
        return (ExpressionSyntax)
            generator.MemberAccessExpression(
                enumRoot,
                kind switch
                {
                    MethodKind.Getter => "Getter",
                    MethodKind.Setter => "Setter",
                    _ => "Normal",
                }
            );
    }

    private static ExpressionSyntax CreateArgumentType(SyntaxGenerator generator, ArgumentKind kind)
    {
        SyntaxNode enumRoot = ParseName("HarmonyLib.ArgumentType");
        return (ExpressionSyntax)
            generator.MemberAccessExpression(
                enumRoot,
                kind switch
                {
                    ArgumentKind.Out => "Out",
                    ArgumentKind.Ref => "Ref",
                    _ => "Normal",
                }
            );
    }

    private static ExpressionSyntax CreateSimplifiedArrayExpresion(
        string elementTypeName,
        IEnumerable<ExpressionSyntax> exprs,
        SyntaxGenerator generator,
        bool useCollectionExpressions
    )
    {
        if (useCollectionExpressions)
        {
            IEnumerable<ExpressionElementSyntax> collectionMembers = exprs.Select(
                ExpressionElement
            );
            SeparatedSyntaxList<CollectionElementSyntax> syntaxList =
                SeparatedList<CollectionElementSyntax>(collectionMembers);
            return CollectionExpression(syntaxList);
        }
        else
        {
            return (ExpressionSyntax)
                generator.ArrayCreationExpression(ParseName(elementTypeName), exprs);
        }
    }
}
