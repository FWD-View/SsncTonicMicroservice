using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nett;

namespace Tonic.Common.Models;

public class TableWithRowKeys
{
    public readonly Table Table;
    public readonly IList<string> RowKeys;

    public static TableWithRowKeys Parse(TomlTable config)
    {
        var table = new Table(config.Get<string>("HostCategory"), config.Get<string>("Table"));
        IList<string> primaryKeys;
        if (config.ContainsKey("RowKeys"))
        {
            var tomlPrimaryKeys = config.Get<TomlArray>("RowKeys");
            primaryKeys = tomlPrimaryKeys.To<string>().Select(s => s.ToUpperInvariant()).ToImmutableArray();
        }
        else
            primaryKeys = ImmutableArray<string>.Empty;

        return new TableWithRowKeys(table, primaryKeys);
    }

    public TableWithRowKeys(Table table, IList<string> rowKeys)
    {
        Table = table;
        RowKeys = rowKeys;
    }

    public override bool Equals(object? obj)
    {
        if (obj is TableWithRowKeys rrt)
        {
            return Table.Equals(rrt.Table) && RowKeys.SequenceEqual(rrt.RowKeys);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var pk in RowKeys)
        {
            hash ^= pk.GetHashCode();
        }

        return HashCode.Combine(Table, hash);
    }

    public override string ToString() => $"{Table}: RowKeys [{string.Join(", ", RowKeys)}]";
}