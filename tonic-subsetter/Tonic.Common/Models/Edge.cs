using System;

namespace Tonic.Common.Models;

public class Edge<T> where T : notnull
{
    public readonly T Parent;
    public readonly T Child;

    public Edge(T parent, T child)
    {
        this.Parent = parent;
        this.Child = child;
    }

    public override bool Equals(object? obj)
    {
        if (obj is Edge<T> other)
        {
            return Parent.Equals(other.Parent) && Child.Equals(other.Child);
        }

        return false;
    }

    public override string ToString()
    {
        return $"Parent: {Parent} Child: {Child}";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Parent, Child);
    }
}