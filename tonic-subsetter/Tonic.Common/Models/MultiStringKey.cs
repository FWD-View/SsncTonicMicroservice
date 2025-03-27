using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Tonic.Common.Models;

public sealed class MultiStringKey
{
    public readonly ImmutableArray<string> Parts;

    public MultiStringKey(Table table, IEnumerable<string> parts)
    {
        Parts = new List<string> {table.HostCategory, table.TableName}.Concat(parts).ToImmutableArray();
    }
        
    public MultiStringKey(IEnumerable<string> parts)
    {
        Parts = parts.ToImmutableArray();
    }

    public string this [int index] => Parts[index];
    public int Length => Parts.Length;

    private bool Equals(MultiStringKey other)
    {
        return Parts.SequenceEqual(other.Parts);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((MultiStringKey) obj);
    }

    public override int GetHashCode()
    {
        return Parts.Aggregate(0, HashCode.Combine);
    }

    public MultiStringKey From(int start) => new(Parts.Skip(start));

    public override string ToString() => $"{string.Join("-", Parts)}";
}