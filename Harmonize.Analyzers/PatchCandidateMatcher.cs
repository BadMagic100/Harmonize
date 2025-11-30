using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Harmonize;

public record MetadataChange(
    string? Name,
    MethodKind? Kind,
    ImmutableEquatableArray<ArgumentDescriptor>? Arguments,
    ImmutableEquatableArray<ArgumentKind>? ArgumentKinds
)
{
    public static readonly MetadataChange NoChange = new(null, null, null, null);
}

public record PatchCandidateWithChanges(IMethodSymbol Candidate, MetadataChange Change);

public static class PatchCandidateMatcher
{
    /// <summary>
    /// Given fully specified data, filters to the matching methods, which require no changes because the data is fully specified.
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
                        // todo: also determine changes needed here
                        finalCandidates.Add(new(finalCandidate, MetadataChange.NoChange));
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
            ArgumentKind k = args[i].Kind;
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
}
