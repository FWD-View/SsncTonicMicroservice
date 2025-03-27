using System;
using Nett;

namespace Tonic.Common.Models;

public class ColumnKey
{
    public string HostCategoryName { get; }
    public string TableName { get; }
    public string ColumnName { get; }

    public ColumnKey(string hostCategoryName, string tableName, string columnName)
    {
        HostCategoryName = hostCategoryName;
        TableName = tableName;
        ColumnName = columnName;
    }

    public ColumnKey(ColumnKey column)
    {
        HostCategoryName = column.HostCategoryName;
        TableName = column.TableName;
        ColumnName = column.ColumnName;
    }

    public bool IsOfColumn(object? obj) =>
        obj switch
        {
            Column column => column.HostCategoryName == HostCategoryName &&
                             column.TableName == TableName && column.ColumnName == ColumnName,
            _ => false
        };

    public override bool Equals(object? obj) =>
        obj switch
        {
            ColumnKey other => other.HostCategoryName == HostCategoryName &&
                               other.TableName == TableName && other.ColumnName == ColumnName,
            _ => false
        };

    public override int GetHashCode() => HashCode.Combine(HostCategoryName, TableName, ColumnName);

    public override string ToString() => $"{HostCategoryName}.{TableName}.{ColumnName}";

    public static ColumnKey FromColumn(Column c) => new(c.HostCategoryName, c.TableName, c.ColumnName);
    
    public static ColumnKey FromConfig(TomlTable tableToml)
    {
        var hostCategory = tableToml.Get<string>("HostCategory");
        var tableName = tableToml.Get<string>("Table");
        var columnName = tableToml.Get<string>("Column");
        return new ColumnKey(hostCategory, tableName, columnName);
    }

    public bool IsOnTable(Column column) =>
        HostCategoryName == column.HostCategoryName && TableName == column.TableName;
}