using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Harmonize;

public class MaybeAmbiguous<T> : IEquatable<MaybeAmbiguous<T>>
{
    private readonly ImmutableArray<T> candidates;

    private MaybeAmbiguous(ImmutableArray<T> candidates)
    {
        this.candidates = candidates;
    }

    public bool IsAmbiguous => candidates.Length != 1;
    public T Value =>
        IsAmbiguous ? throw new InvalidOperationException("Value is ambiguous") : candidates[0];
    public ImmutableArray<T> Candidates => candidates;

    public static MaybeAmbiguous<T>? MergeSymmetric(MaybeAmbiguous<T>? a, MaybeAmbiguous<T>? b)
    {
        if (a == null)
        {
            return b;
        }
        if (b == null)
        {
            return a;
        }

        ImmutableArray<T>.Builder builder = ImmutableArray.CreateBuilder<T>(
            a.candidates.Length + b.candidates.Length
        );
        builder.AddRange(a.candidates);
        builder.AddRange(b.candidates);
        return new MaybeAmbiguous<T>(builder.ToImmutable());
    }

    public static MaybeAmbiguous<T>? FromEnumerable(IEnumerable<T> values)
    {
        MaybeAmbiguous<T>? result = null;
        foreach (T value in values)
        {
            result = MergeSymmetric(value, result);
        }
        return result;
    }

    public bool Equals(MaybeAmbiguous<T> other)
    {
        if (candidates.Length != other.candidates.Length)
        {
            return false;
        }
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i]?.Equals(other.candidates[i]) == false)
            {
                return false;
            }
        }
        return true;
    }

    public static implicit operator MaybeAmbiguous<T>(T orig)
    {
        return new(ImmutableArray.Create(orig));
    }
}

public static class MaybeAmbiguousExtensions
{
    extension<T>(T orig)
    {
        public MaybeAmbiguous<T> ToMA()
        {
            return (MaybeAmbiguous<T>)orig;
        }
    }
}
