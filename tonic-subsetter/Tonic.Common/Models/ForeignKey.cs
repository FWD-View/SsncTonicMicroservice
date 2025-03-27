using System.Collections.Immutable;
using System.Linq;
using Tonic.Common.Helpers;

namespace Tonic.Common.Models;

public class ForeignKey
{
    public string ForeignKeyHostCategory { get; init; } = string.Empty;
    public string ForeignKeyTable { get; init; } = string.Empty;
    public ImmutableArray<string> ForeignKeyColumns { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableDictionary<string, string> ForeignKeyColumnCastTypes { get; init; } =
        ImmutableDictionary<string, string>.Empty;

    public IForeignKeyProcessor ForeignKeyProcessor { get; init; } = IForeignKeyProcessor.Default;
    public string PrimaryKeyHostCategory { get; init; } = string.Empty;
    public string PrimaryKeyTable { get; init; } = string.Empty;
    public ImmutableArray<string> PrimaryKeyColumns { get; init; } = ImmutableArray<string>.Empty;
    public bool IsPrincipal { get; init; }
    public bool IsSecondary { get; init; }
    public bool Grouped { get; init; }
    public string? StructuredKeyName { get; init; }
    public ImmutableArray<int> MultiTableIndexColumnIndices { get; init; } = ImmutableArray<int>.Empty;

    public override string ToString()
    {
        var columns = ForeignKeyColumns.Zip(PrimaryKeyColumns)
            .Select((pair, _) => $"({pair.First} : {pair.Second})");
        return
            $"{ForeignKeyHostCategory}.{ForeignKeyTable}  ->  {PrimaryKeyHostCategory}.{PrimaryKeyTable} : [{string.Join(", ", columns)}]";
    }
}