using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nett;
using Tonic.Common.Configs;
using Tonic.Common.Exceptions;
using Tonic.Common.Helpers;
using Tonic.Common.Models;

namespace Tonic.Common.Extensions;

public static class PrimaryKeyRemapperToml
{
    public static ImmutableDictionary<Table, UpsertTable> ParseUpsertTables(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("UpsertTable")) return ImmutableDictionary<Table, UpsertTable>.Empty;
        var upsertTables = new Dictionary<Table, UpsertTable>();
        var tomlTableArray = (TomlTableArray)tomlConfig["UpsertTable"];
        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var tomlTableElement = tomlTableArray[i];
            var upsertTable = UpsertTable.FromConfig(tomlTableElement);
            upsertTables.Add(upsertTable.AsTable, upsertTable);
        }

        return upsertTables.ToImmutableDictionary();
    }

    public static IEnumerable<TableWithRowKeys> ParseReuseRowsTables(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("ReuseRowsTable")) return new HashSet<TableWithRowKeys>();

        var reuseRowsTables = new HashSet<TableWithRowKeys>();

        var tables = (TomlTableArray)tomlConfig["ReuseRowsTable"];
        for (var i = 0; i < tables.Count; ++i)
        {
            var table = TableWithRowKeys.Parse(tables[i]);
            reuseRowsTables.Add(table);
        }

        return reuseRowsTables;
    }

    public static IEnumerable<TableWithRowKeys> ParseNoReuseRowsTables(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("NoReuseRowsTable")) return new HashSet<TableWithRowKeys>();

        var noReuseRowsTables = new HashSet<TableWithRowKeys>();

        var tables = (TomlTableArray)tomlConfig["NoReuseRowsTable"];
        for (var i = 0; i < tables.Count; ++i)
        {
            var table = TableWithRowKeys.Parse(tables[i]);
            noReuseRowsTables.Add(table);
        }

        return noReuseRowsTables;
    }

    public static Host? ParseRedactedLookupDestinationHost(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey(Constants.RedactedLookupHost)) return null;

        var tomlTable = tomlConfig.Get<TomlTable>(Constants.RedactedLookupHost);
        var config = HostConfigToml.Parse(tomlTable);
        return new Host(Constants.RedactedLookupHost, config);
    }

    public static IDictionary<string, HostSourceAndDest> ParseHosts(this TomlTable tomlConfig)
    {
        var sources = ParseHostsConfigs(tomlConfig, Constants.SourceHost)
            .OrderBy(lhc => lhc.HostCategory);
        var destinations =
            ParseHostsConfigs(tomlConfig, Constants.DestinationHost)
                .OrderBy(lhc => lhc.HostCategory);

        var hostLookup = new Dictionary<string, HostSourceAndDest>();
        foreach (var (source, dest) in sources.Zip(destinations))
        {
            if (source.HostCategory != dest.HostCategory)
            {
                throw new ArgumentException(
                    $"Mismatched host categories. Source={source.HostCategory}; Destination={dest.HostCategory}");
            }

            hostLookup[source.HostCategory] = new HostSourceAndDest(source, dest);
        }

        return hostLookup.ToImmutableDictionary();
    }

    private static IEnumerable<HostConfig> ParseHostsConfigs(TomlTable tomlConfig, string key)
    {
        var hostConfigs = new List<HostConfig>();
        var tomlTableArray = (TomlTableArray)tomlConfig[key];

        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var host = tomlTableArray[i];
            hostConfigs.Add(HostConfigToml.Parse(host));
        }

        return hostConfigs;
    }

    public static IList<ColumnSequence> ParseColumnSequences(this TomlTable tomlConfig)
    {
        var columnSequences = new List<ColumnSequence>();
        if (!tomlConfig.ContainsKey("ColumnSequence"))
        {
            return columnSequences;
        }

        var tomlTableArray = (TomlTableArray)tomlConfig["ColumnSequence"];

        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var columnSequence = tomlTableArray[i];
            columnSequences.Add(new ColumnSequence(columnSequence));
        }

        return columnSequences.ToImmutableList();
    }

    public static ImmutableDictionary<Table, List<RowProcessor>> ParsePostProcessors(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("RowProcessor")) return ImmutableDictionary<Table, List<RowProcessor>>.Empty;
        var processors = new Dictionary<Table, List<RowProcessor>>();
        var tomlTableArray = (TomlTableArray)tomlConfig["RowProcessor"];
        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var processorToml = tomlTableArray[i];
            var (table, rowProcessor) = RowProcessor.FromToml(processorToml);
            processors.TryAdd(table, new List<RowProcessor>());
            processors[table].Add(rowProcessor);
        }

        return processors.ToImmutableDictionary();
    }

    public static IDictionary<Table, ColumnKey> ParseIdColumnKeys(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("IdFileColumnKey"))
            return ImmutableDictionary<Table, ColumnKey>.Empty;

        var columnKeys = new Dictionary<Table, ColumnKey>();
        var tomlTableArray = (TomlTableArray)tomlConfig["IdFileColumnKey"];

        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var lhcName = tomlTableArray[i].Get<string>("HostCategory");
            var tableName = tomlTableArray[i].Get<string>("Table");
            var columnName = tomlTableArray[i].Get<string>("Column");
            var table = new Table(lhcName, tableName);
            var columnKey = new ColumnKey(lhcName, tableName, columnName);
            if (columnKeys.TryGetValue(table, out var other))
            {
                throw new InvalidConfigurationException(
                    $"Duplicate [[IdFileColumnKey]] encountered; may only specify 1 custom ID Column Key per file. {columnKey} conflicts with {other}");
            }
            columnKeys[table] = columnKey;
        }

        return columnKeys.ToImmutableDictionary();
    }
}