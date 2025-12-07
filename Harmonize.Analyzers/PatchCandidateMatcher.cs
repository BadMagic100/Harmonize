using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Harmonize;

public record MetadataChange(
    string? Name,
    MethodKind? Kind,
    ImmutableEquatableArray<ArgumentDescriptor>? Arguments
);

public record PatchCandidateWithChanges(IMethodSymbol Candidate, MetadataChange Change);

public static class PatchCandidateMatcher
{
    /// <summary>
    /// Given fully specified data, filters to the matching methods, which cannot possibly require changes because the data is fully specified.
    /// </summary>
    public static IEnumerable<IMethodSymbol> GetFullySpecifiedCandidates(
        ITypeSymbol declaringType,
        string memberName,
        MethodKind methodKind,
        ImmutableEquatableArray<ArgumentDescriptor>? arguments
    )
    {
        return FilterCandidatesInternal(declaringType, memberName, methodKind, arguments)
            .Select(c => c.Candidate);
    }

    /// <summary>
    /// Given partially specified data, filters to the symbols matching any of the specified filters and
    /// provides the minimum additional data needed on the attribute to unambiguously resolve to each one
    /// </summary>
    public static ImmutableEquatableArray<PatchCandidateWithChanges> GetCandidates(
        ITypeSymbol declaringType,
        // keep a watch on: do we need to allow ambiguity for these, or is just nullable ok
        MaybeAmbiguous<string>? memberName,
        MaybeAmbiguous<MethodKind>? kind,
        MaybeAmbiguous<ImmutableEquatableArray<ArgumentDescriptor>>? arguments
    )
    {
        // apparently MaybeAmbiguous<T>? can't be passed as a parameter for MaybeAmbiguous<T?>? for some reason but it should be fine.
        ImmutableEquatableArray<PatchCandidateWithChanges> initialCandidates =
            FilterCandidatesInternal(declaringType, memberName, kind, arguments!);
        return SimplifyChangeBatch(initialCandidates, memberName, kind, arguments!);
    }

    private static ImmutableEquatableArray<PatchCandidateWithChanges> FilterCandidatesInternal(
        ITypeSymbol declaringType,
        MaybeAmbiguous<string>? memberName,
        MaybeAmbiguous<MethodKind>? kind,
        MaybeAmbiguous<ImmutableEquatableArray<ArgumentDescriptor>?>? arguments
    )
    {
        ImmutableArray<PatchCandidateWithChanges>.Builder finalCandidates =
            ImmutableArray.CreateBuilder<PatchCandidateWithChanges>();

        IEnumerable<ISymbol> initialCandidates =
            memberName == null
                ? declaringType.GetMembers()
                : memberName.Candidates.SelectMany(n => declaringType.GetMembers(n));
        initialCandidates = initialCandidates.Where(c => c.CanBeReferencedByName);

        IEnumerable<MethodKind> candidateMethodKinds =
            kind == null
                ? Enum.GetValues(typeof(MethodKind)).OfType<MethodKind>().ToImmutableArray()
                : kind.Candidates;

        IEnumerable<ImmutableEquatableArray<ArgumentDescriptor>?> candidateMethodArgs =
            arguments == null ? [null] : arguments.Candidates;

        foreach (ISymbol candidate in initialCandidates)
        {
            foreach (MethodKind candidateKind in candidateMethodKinds)
            {
                foreach (
                    ImmutableEquatableArray<ArgumentDescriptor>? candidateArgs in candidateMethodArgs
                )
                {
                    if (
                        TraverseAndFilterSingleCandidate(candidate, candidateKind, candidateArgs)
                        is IMethodSymbol finalCandidate
                    )
                    {
                        // the values here will either be the values the user already specified or new unambiguous values.
                        // we'll simplify later.
                        ImmutableEquatableArray<ArgumentDescriptor>? newArgs =
                            SynthesizeArgDescriptors(candidate);
                        finalCandidates.Add(
                            new PatchCandidateWithChanges(
                                finalCandidate,
                                new MetadataChange(candidate.Name, candidateKind, newArgs)
                            )
                        );
                    }
                }
            }
        }
        return finalCandidates.ToImmutable();
    }

    private static IMethodSymbol? TraverseAndFilterSingleCandidate(
        ISymbol symbol,
        MethodKind kind,
        ImmutableEquatableArray<ArgumentDescriptor>? arguments
    )
    {
        IMethodSymbol? resolvedCandidate = kind switch
        {
            MethodKind.Normal when symbol is IMethodSymbol m => m,
            MethodKind.Getter when symbol is IPropertySymbol p => p.GetMethod,
            MethodKind.Setter when symbol is IPropertySymbol p => p.SetMethod,
            _ => null,
        };

        if (resolvedCandidate != null && arguments != null)
        {
            if (!AllArgsMatch(resolvedCandidate.Parameters, arguments))
            {
                return null;
            }
        }
        return resolvedCandidate;
    }

    private static bool AllArgsMatch(
        ImmutableEquatableArray<IParameterSymbol> parameters,
        ImmutableEquatableArray<ArgumentDescriptor> args
    )
    {
        if (parameters.Count != args.Count)
        {
            return false;
        }

        for (int i = 0; i < parameters.Count; i++)
        {
            IParameterSymbol p = parameters[i];
            ArgumentKind? k = args[i].Kind;
            if (k == ArgumentKind.Normal && p.RefKind != RefKind.None)
            {
                return false;
            }
            if (k == ArgumentKind.Out && p.RefKind != RefKind.Out)
            {
                return false;
            }
            if (k == ArgumentKind.Ref && p.RefKind != RefKind.Ref)
            {
                return false;
            }
            if (!p.Type.Equals(args[i].Type, SymbolEqualityComparer.Default))
            {
                return false;
            }
        }
        return true;
    }

    private static ImmutableEquatableArray<ArgumentDescriptor>? SynthesizeArgDescriptors(
        ISymbol candidate
    )
    {
        if (candidate is not IMethodSymbol method)
        {
            return null;
        }

        ImmutableArray<ArgumentDescriptor>.Builder builder =
            ImmutableArray.CreateBuilder<ArgumentDescriptor>(method.Parameters.Length);
        foreach (IParameterSymbol param in method.Parameters)
        {
            ArgumentKind argKind = param.RefKind switch
            {
                RefKind.None => ArgumentKind.Normal,
                RefKind.Ref => ArgumentKind.Ref,
                RefKind.Out => ArgumentKind.Out,
                _ => ArgumentKind.Unsupported,
            };
            builder.Add(new ArgumentDescriptor(param.Type, argKind));
        }
        return builder.ToImmutable();
    }

    /// <summary>
    /// Simplifies a batch of received candidate changes to remove all change data that is not needed to semantically disambiguate
    /// the change from other changes in the batch
    /// </summary>
    private static ImmutableEquatableArray<PatchCandidateWithChanges> SimplifyChangeBatch(
        ImmutableEquatableArray<PatchCandidateWithChanges> changes,
        MaybeAmbiguous<string>? userSpecifiedName,
        MaybeAmbiguous<MethodKind>? userSpecifiedKind,
        MaybeAmbiguous<ImmutableEquatableArray<ArgumentDescriptor>?>? userSpecifiedArguments
    )
    {
        ImmutableArray<PatchCandidateWithChanges>.Builder result =
            ImmutableArray.CreateBuilder<PatchCandidateWithChanges>(changes.Count);

        // simplifications we can make
        // 1. For all candidates that share the same name, if all the arguments are the same, argument descriptors can be removed
        // 2. For all candidates with the same name and argument types, if the argument kinds are the same, argument kinds can be removed
        // 2. If MethodKind is Normal, it can be removed
        // 3. If the user already specified a value, it isn't a change so we can remove it
        IEnumerable<IGrouping<string, PatchCandidateWithChanges>> candidateNameGroups =
            changes.GroupBy(c => c.Candidate.Name);
        foreach (IGrouping<string, PatchCandidateWithChanges> group in candidateNameGroups)
        {
            int nUniqueArgDescriptors = group.Select(c => c.Change.Arguments).Distinct().Count();
            Dictionary<ImmutableEquatableArray<ITypeSymbol>, int> argKindGroups = group
                .GroupBy(
                    c =>
                        ImmutableEquatableArray.Create(
                            c.Change.Arguments.Select(d => d.Type).ToArray()
                        ),
                    c =>
                        ImmutableEquatableArray.Create(
                            c.Change.Arguments.Select(d => d.Kind).ToArray()
                        ),
                    (k, g) =>
                        new KeyValuePair<ImmutableEquatableArray<ITypeSymbol>, int>(
                            k,
                            g.Distinct().Count()
                        )
                )
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (PatchCandidateWithChanges candidate in group)
            {
                MetadataChange newChange = candidate.Change;
                if (newChange.Kind == MethodKind.Normal)
                {
                    newChange = newChange with { Kind = null };
                }

                if (nUniqueArgDescriptors == 1 && newChange.Arguments != null)
                {
                    newChange = newChange with { Arguments = null };
                }
                else if (newChange.Arguments != null)
                {
                    ImmutableEquatableArray<ITypeSymbol> types = newChange
                        .Arguments.Select(d => d.Type)
                        .ToImmutableArray();
                    if (argKindGroups[types] == 1)
                    {
                        IEnumerable<ArgumentDescriptor> newArgs = newChange.Arguments.Select(d =>
                            d with
                            {
                                Kind = null,
                            }
                        );
                        newChange = newChange with { Arguments = newArgs.ToImmutableArray() };
                    }
                }

                if (
                    userSpecifiedName != null
                    && !userSpecifiedName.IsAmbiguous
                    && userSpecifiedName.Value == newChange.Name
                )
                {
                    newChange = newChange with { Name = null };
                }
                if (
                    userSpecifiedKind != null
                    && !userSpecifiedKind.IsAmbiguous
                    && userSpecifiedKind.Value == newChange.Kind
                )
                {
                    newChange = newChange with { Kind = null };
                }
                if (
                    userSpecifiedArguments != null
                    && !userSpecifiedArguments.IsAmbiguous
                    && userSpecifiedArguments.Value == newChange.Arguments
                )
                {
                    newChange = newChange with { Arguments = null };
                }

                result.Add(candidate with { Change = newChange });
            }
        }
        return result.ToImmutable();
    }
}
