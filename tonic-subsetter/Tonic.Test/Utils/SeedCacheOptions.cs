using System.Collections.Generic;
using Tonic.Common.Models;

namespace Tonic.Test.Utils;

public record SeedCacheOptions
{
    public Dictionary<Table, int> LimitLookup { get; init; } = new();
    public int Stride { get; init; } = 1;
    public HashSet<(string, int)> ColumnsToNull { get; init; } = new();
}