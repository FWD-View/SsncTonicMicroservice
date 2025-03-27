using System;

namespace Tonic.Common.Models;

public class Column : ColumnKey
{
    public string DataType { get; }
    public bool IsNullable { get; }

    public Column(string hostCategoryName, string tableName, string columnName, string dataType, bool isNullable)
        : base(hostCategoryName, tableName, columnName)
    {
        DataType = dataType;
        IsNullable = isNullable;
    }

    public Column(Table table, string columnName, string dataType, bool isNullable = false) : base(table.HostCategory, table.TableName, columnName)
    {
        DataType = dataType;
        IsNullable = isNullable;
    }

    public override bool Equals(object? obj)
    {
        if (obj is Column other)
        {
            return HostCategoryName == other.HostCategoryName
                   && TableName == other.TableName
                   && ColumnName == other.ColumnName
                   && DataType == other.DataType;
        }

        return base.Equals(obj);
    }

    public string ColumnSelector => $"{TableName}.{ColumnName}";

    public override int GetHashCode()
    {
        return HashCode.Combine(HostCategoryName, TableName, ColumnName, DataType);
    }

    public Column WithTable(string table)
    {
        return new Column(HostCategoryName, table, ColumnName, DataType, IsNullable);
    }

    public override string ToString() =>
        $"Column[LHC=\"{HostCategoryName}\", TN=\"{TableName}\", CN=\"{ColumnName}\" DT=\"{DataType}]\"";
}