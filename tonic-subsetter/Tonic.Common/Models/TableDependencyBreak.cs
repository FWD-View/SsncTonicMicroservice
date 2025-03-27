namespace Tonic.Common.Models;

public class TableDependencyBreak
{
    private readonly Table _fkTable;
    private readonly Table _pkTable;

    private TableDependencyBreak(Table fkTable, Table pkTable)
    {
        _fkTable = fkTable;
        _pkTable = pkTable;
    }

    public Table ForeignKeyTable()
    {
        return _fkTable;
    }

    public override bool Equals(object? obj)
    {
        if (obj is TableDependencyBreak other)
        {
            return _fkTable.Equals(other._fkTable) && _pkTable.Equals(other._pkTable);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return _fkTable.GetHashCode() ^ _pkTable.GetHashCode();
    }

    public static TableDependencyBreak FromTables(Table fkTable, Table pkTable)
    {
        return new TableDependencyBreak(fkTable, pkTable);
    }

    public override string ToString()
    {
        return $"{_fkTable} -> {_pkTable}";
    }
}