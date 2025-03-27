using System;
using System.Collections.Immutable;

namespace Tonic.Common.Models;

public class TableRelationship
{
    public readonly Table ForeignKeyTable;
    public readonly Table PrimaryKeyTable;
    private readonly ImmutableArray<string> _fkColumns;
    private readonly ImmutableArray<string> _pkColumns;
    public readonly bool IsPrincipal;
    public readonly bool IsMultiTable;

    public TableRelationship(Table foreignKeyTable, Table primaryKeyTable, bool isPrincipal,
        ImmutableArray<string> fkColumns, ImmutableArray<string> pkColumns, bool isMultiTable)
    {
        ForeignKeyTable = foreignKeyTable;
        PrimaryKeyTable = primaryKeyTable;
        IsPrincipal = isPrincipal;
        _fkColumns = fkColumns;
        _pkColumns = pkColumns;
        IsMultiTable = isMultiTable;
    }

    public override string ToString()
    {
        var columnsStr = $"[{string.Join(", ", _fkColumns)}] -> [{string.Join(", ", _pkColumns)}]]";
        return
            $"TableRelationship[{ForeignKeyTable} -> {PrimaryKeyTable}, IsPrincipal: {IsPrincipal}, Columns: {columnsStr}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is TableRelationship other)
        {
            return ForeignKeyTable.Equals(other.ForeignKeyTable) && PrimaryKeyTable.Equals(other.PrimaryKeyTable) &&
                   IsPrincipal == other.IsPrincipal;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ForeignKeyTable, PrimaryKeyTable, IsPrincipal);
    }
}