using System;
using System.Linq;
using Nett;

namespace Tonic.Common.Models;

public class Table : IComparable
{
    public bool Grouped { get; init; }
    public string HostCategory { get; init; }
    public string TableName { get; init; }

    public Table(string hostCategory, string tableName, bool groupedTable = false)
    {
        HostCategory = hostCategory;
        TableName = tableName;

        // used in primary key remapper to know when operating on a table that's part of a group of tables that
        // use modulus arithmetic to spread IDs among them.
        // we hard code things to go to the 0th such table for now.
        Grouped = groupedTable && tableName.Any(char.IsDigit);
    }

    public static Table FromConfig(TomlTable tableToml)
    {
        var hostCategory = tableToml.Get<string>("HostCategory");
        var tableName = tableToml.Get<string>("Table");
        var grouped = tableToml.ContainsKey("Grouped") && tableToml.Get<bool>("Grouped");
        return new Table(hostCategory, tableName, grouped);
    }

    public int CompareTo(object? obj)
    {
        if (obj is not Table otherTable)
        {
            throw new ArgumentException("Object is not Table instance");
        }

        var hostComparison = string.Compare(HostCategory, otherTable.HostCategory,
            StringComparison.InvariantCultureIgnoreCase);
        return hostComparison != 0
            ? hostComparison
            : string.Compare(TableName, otherTable.TableName, StringComparison.InvariantCultureIgnoreCase);
    }

    public override string ToString()
    {
        return $"Table[LHC=\"{HostCategory}\", TN=\"{TableName}\", GRP={Grouped}]";
    }

    public string HostAndTable() => $"{HostCategory}.{TableName}";

    public override bool Equals(object? obj)
    {
        if (obj is Table otherTable)
        {
            // NOTE: Grouped is intentionally omitted
            return HostCategory.Equals(otherTable.HostCategory,
                       StringComparison.InvariantCultureIgnoreCase)
                   && TableName.Equals(otherTable.TableName, StringComparison.InvariantCultureIgnoreCase);
        }

        return false;
    }

    // NOTE: Grouped is intentionally omitted
    public override int GetHashCode() =>
        HashCode.Combine(HostCategory.ToUpperInvariant(), TableName.ToUpperInvariant());

    public bool IsForeignKeyOf(ForeignKey fk) =>
        HostCategory.Equals(fk.ForeignKeyHostCategory, StringComparison.InvariantCultureIgnoreCase)
        && TableName.Equals(fk.ForeignKeyTable, StringComparison.InvariantCultureIgnoreCase);

    public bool IsPrimaryKeyOf(ForeignKey fk) =>
        HostCategory.Equals(fk.PrimaryKeyHostCategory, StringComparison.InvariantCultureIgnoreCase)
        && TableName.Equals(fk.PrimaryKeyTable, StringComparison.InvariantCultureIgnoreCase);

    public bool HasColumnKey(ColumnKey columnKey) =>
        HostCategory == columnKey.HostCategoryName && TableName == columnKey.TableName;
}