using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Harmonize;

public class ImmutableEquatableArray<T>(ImmutableArray<T> inner)
    : IReadOnlyList<T>,
        IEquatable<ImmutableEquatableArray<T>>
{
    private readonly ImmutableArray<T> inner = inner;

    public T this[int index] => ((IReadOnlyList<T>)inner)[index];

    public int Count => ((IReadOnlyCollection<T>)inner).Count;

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)inner).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)inner).GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj is not ImmutableEquatableArray<T> other)
        {
            return false;
        }
        return Equals(other);
    }

    public bool Equals(ImmutableEquatableArray<T> other)
    {
        if (other.inner.Length != this.inner.Length)
        {
            return false;
        }
        for (int i = 0; i < this.inner.Length; i++)
        {
            if (!object.Equals(this[i], other[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override string ToString()
    {
        return $"[{string.Join(", ", inner)}]";
    }

    public static implicit operator ImmutableEquatableArray<T>(ImmutableArray<T> inner)
    {
        return new ImmutableEquatableArray<T>(inner);
    }
}

public static class ImmutableEquatableArray
{
    public static ImmutableEquatableArray<T> Create<T>(params T[] values)
    {
        return new ImmutableEquatableArray<T>(ImmutableArray.Create(values));
    }
}
