using System;
using System.Collections.Immutable;
using Tonic.Common.Configs;
using Tonic.Common.Helpers;

namespace Tonic.Common.Models;

public class HostSourceAndDest
{
    public readonly string Category;
    public readonly IHost Destination;
    public readonly IHost Source;

    public HostSourceAndDest(HostConfig source, HostConfig dest)
    {
        if (source.HostCategory != dest.HostCategory)
            throw new ArgumentException("Mismatched host categories");
        Source = new Host(source.Schema, source, ImmutableArray<SchemaOverride>.Empty);
        Destination = new Host(dest.Schema, dest, ImmutableArray<SchemaOverride>.Empty);
        Category = source.HostCategory;
    }
}