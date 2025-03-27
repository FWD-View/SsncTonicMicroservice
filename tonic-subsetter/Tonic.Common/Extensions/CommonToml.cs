using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Nett;
using Serilog;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;

namespace Tonic.Common.Extensions;

public static class CommonToml
{
    public static Table? ParseUsersTable(this TomlTable config)
    {
        if (!config.ContainsKey("UsersTable"))
            return null;

        var usersTableConfig = (TomlTable)config["UsersTable"];
        var hostCategory = usersTableConfig.Get<string>("HostCategory");
        var tableName = usersTableConfig.Get<string>("TableName");
        var table = new Table(hostCategory, tableName);
        return table;
    }

    public static ImmutableHashSet<Table> ParseDuplicateImportsAllowedTables(this TomlTable config)
    {
        const string tomlKey = "DuplicateImportsAllowedTables";
        var tables = new HashSet<Table>();
        if (!config.ContainsKey(tomlKey)) return ImmutableHashSet<Table>.Empty;
        var duplicatesAllowedTables = (TomlTableArray)config[tomlKey];

        for (var i = 0; i < duplicatesAllowedTables.Count; i++)
        {
            var tableConfig = duplicatesAllowedTables[i];
            var hostCategory = tableConfig.Get<string>("HostCategory");
            var tableName = tableConfig.Get<string>("TableName");
            tables.Add(new Table(hostCategory, tableName));
        }

        return tables.ToImmutableHashSet();
    }

    public static ImmutableHashSet<ColumnKey> ParseSkipRemappingColumns(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("SkipRemapColumn")) return ImmutableHashSet<ColumnKey>.Empty;
        var skipColumns = new HashSet<ColumnKey>();
        var tomlTableArray = (TomlTableArray)tomlConfig["SkipRemapColumn"];

        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var tableToml = tomlTableArray[i];
            var hostCategory = tableToml.Get<string>("HostCategory");
            var tableName = tableToml.Get<string>("Table");
            var columnName = tableToml.Get<string>("Column");
            var column = new ColumnKey(hostCategory, tableName, columnName);
            skipColumns.Add(column);
        }

        return skipColumns.ToImmutableHashSet();
    }

    public static Dictionary<string, string> ParseSchemaRestrictions(this TomlTable config)
    {
        const string tomlConfigKey = "SourceHostSchemaTableRestrictions";
        var mappings = new Dictionary<string, string>();
        if (!config.ContainsKey(tomlConfigKey)) return mappings;

        var restrictions = (TomlTable)config[tomlConfigKey];

        foreach (var (schema, restrictedTablesToml) in restrictions)
        {
            var restrictedTables = ((TomlArray)restrictedTablesToml).To<string>();

            foreach (var tableName in restrictedTables)
            {
                if (!mappings.TryAdd(tableName, schema))
                    throw new InvalidDataException($"Error: multiple restrictions found for {tableName}");
            }
        }

        return mappings;
    }

    public static ImmutableDictionary<string, List<string>> ParsePostResetSchemaScript(this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("PostResetSchemaScript"))
            return ImmutableDictionary<string, List<string>>.Empty;

        var resetSchemaScripts = new Dictionary<string, List<string>>();
        var postResetSchemaScriptToml = (TomlTableArray)tomlConfig["PostResetSchemaScript"];

        for (var i = 0; i < postResetSchemaScriptToml.Count; i++)
        {
            var scriptToml = postResetSchemaScriptToml[i]!;
            var host = scriptToml.Get<string>("HostCategory");
            var sqlScript = scriptToml.Get<string>("SqlScript");
            if (string.IsNullOrEmpty(sqlScript))
            {
                throw new InvalidDataException($"Empty script on {host}");
            }

            resetSchemaScripts.TryAdd(host, new List<string>());
            resetSchemaScripts[host].Add(sqlScript);
        }

        return resetSchemaScripts.ToImmutableDictionary();
    }

    public static ImmutableHashSet<string> ParseCompressedColumns(this TomlTable config)
    {
        if (!config.ContainsKey("CompressedColumns"))
            return ImmutableHashSet<string>.Empty;
        var compressedColumns = new HashSet<string>();
        var restrictions = (TomlTable)config["CompressedColumns"];

        foreach (var (tableName, columnNamesToml) in restrictions)
        {
            var columnNames = ((TomlArray)columnNamesToml).To<string>();

            foreach (var columnName in columnNames)
            {
                compressedColumns.Add($"{tableName}.{columnName}");
            }
        }

        return compressedColumns.ToImmutableHashSet();
    }

    public static ImmutableDictionary<(string, int), ImmutableDictionary<Table, int>> ParseShardedTableMappings(
        this TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("DestinationShardedTableMap"))
            return ImmutableDictionary<(string, int), ImmutableDictionary<Table, int>>.Empty;
        var mappings = new Dictionary<(string, int), ImmutableDictionary<Table, int>>();
        var tomlTableArray = (TomlTableArray)tomlConfig["DestinationShardedTableMap"];
        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var map = new Dictionary<Table, int>();
            var tomlTable = tomlTableArray[i];
            var hostCategory = tomlTable.Get<string>("HostCategory");
            var destinationIndex = tomlTable.Get<int>("ShardIndex");
            if (mappings.ContainsKey((hostCategory, destinationIndex)))
                throw new InvalidDataException($"Found duplicate DestinationShardedTableMap for {hostCategory}");
            foreach (var (key, val) in tomlTable)
            {
                switch (key)
                {
                    case "HostCategory" or "ShardIndex":
                        continue;
                    case null:
                        throw new InvalidDataException(
                            $"Invalid table found for {hostCategory} DestinationShardedTableMap");
                }

                var tableSuffix = val.Get<int>();
                var table = new Table(hostCategory, key, true);
                map[table] = tableSuffix;
            }

            mappings[(hostCategory, destinationIndex)] = map.ToImmutableDictionary();
        }

        return mappings.ToImmutableDictionary();
    }

    public static ImmutableDictionary<Table, DestinationShard<Func<IList<ColumnKey>, IReadOnlyList<string>, int, int>>> ParseShardedDestinationTables(this TomlTable tomlConfig, bool debugShardIndexes = false)
    {
        const string keyName = "ShardedDestinationHostTable";
        if (!tomlConfig.ContainsKey(keyName))
            return ImmutableDictionary<Table, DestinationShard<Func<IList<ColumnKey>, IReadOnlyList<string>, int, int>>>
                .Empty;
        var shardLookup =
            new Dictionary<Table, DestinationShard<Func<IList<ColumnKey>, IReadOnlyList<string>, int, int>>>();
        var tomlTableArray = (TomlTableArray)tomlConfig[keyName];
        for (var i = 0; i < tomlTableArray.Count; i++)
        {
            var tableToml = tomlTableArray[i];
            var columnKey = ColumnKey.FromConfig(tableToml);
            var table = Table.FromConfig(tableToml);
            var modulo = tableToml.Get<int>("Modulo");
            var modulus = tableToml.ContainsKey("Modulus") ? tableToml.Get<int>("Modulus") : -1;
            shardLookup[table] = new DestinationShard<Func<IList<ColumnKey>, IReadOnlyList<string>, int, int>>(modulo,
            (columns, row, count) =>
            {
                var index = columns.FindIndex(columnKey.Equals);
                if (index == -1)
                    throw new InvalidDataException(
                        $"Error attempting to determine sharded destination host for {table}: {columnKey} could not be located");
                if (!long.TryParse(row[index], out var targetVal))
                    throw new InvalidCastException(
                        $"Error attempting to determine sharded destination host for {table}: {columnKey} value cannot be parsed to long");
                var shardIndex = (int)(targetVal % count);
                if (debugShardIndexes)
                    Log.Information("Shard index for {Col}: {Val} % {Mod} = {Idx}", columnKey,
                        targetVal, count, shardIndex);
                return shardIndex;
            }, modulus);
        }

        return shardLookup.ToImmutableDictionary();
    }

    public static IList<IHost> ParseHosts(this TomlTable tomlConfig, string hostType)
    {
        var hosts = new List<IHost>();
        var tomlTableArray = (TomlTableArray)tomlConfig[hostType];

        for (var i = 0; i < tomlTableArray.Count; ++i)
        {
            var hostConfig = tomlTableArray[i];
            var config = HostConfigToml.Parse(hostConfig);
            hosts.Add(new Host(config.HostCategory, config));
        }

        return hosts.ToImmutableArray();
    }
}