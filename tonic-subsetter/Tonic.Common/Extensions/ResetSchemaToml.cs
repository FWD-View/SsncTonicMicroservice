using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nett;
using Serilog;
using Tonic.Common.Models;

namespace Tonic.Common.Extensions;

public static class ResetSchemaToml
{
    public static bool ParseTruncateTablesOnly(this TomlTable resetSchemaOptions)
    {
        if (!resetSchemaOptions.ContainsKey("TableOptions"))
        {
            Log.Information("Could not locate `TableOptions` config item");
            return false;
        }
        var subTableOptions = resetSchemaOptions.Get<TomlTable>("TableOptions");
        if (subTableOptions != null) return subTableOptions.Get<bool>("TruncateOnly");
        Log.Information("Could not locate `[ResetSchema.TableOptions]` config");
        return false;
    }

    public static string ParseDbConfig(this TomlTable resetSchemaOptions, string key)
    {
        var subTable = resetSchemaOptions.Get<TomlTable>("DbaLogin");
        return subTable.Get<string>(key);
    }

    public static bool ParseKeepPrimaryKeys(this TomlTable tomlConfig) =>
        tomlConfig.ContainsKey("KeepPrimaryKeys") && tomlConfig.Get<bool>("KeepPrimaryKeys");

    public static IList<Table> ParseAllReferencedTables(this TomlTable tomlConfig)
    {
        var foreignKeys = tomlConfig.ParseForeignKeys(true);
        var pkTables = foreignKeys.Select(fk => new Table(fk.PrimaryKeyHostCategory, fk.PrimaryKeyTable));
        var fkTables = foreignKeys.Select(fk => new Table(fk.ForeignKeyHostCategory, fk.ForeignKeyTable));

        var directTargets = tomlConfig.ParseDirectTargets(false);
        var dtTables = directTargets.Select(dt => dt.Table);

        return dtTables.Union(pkTables).Union(fkTables).ToImmutableArray();
    }

    public static TableSourceEnum ParseTableSource(this TomlTable resetSchemaOptions)
    {
        if (!resetSchemaOptions.ContainsKey("TableSource"))
            throw new ArgumentException(
                "Must specify TableSource = 'Config' or 'SourceDb' in [ResetSchemaOptions]");

        var tableSource = resetSchemaOptions.Get<string>("TableSource");
        return tableSource.ToLower() switch
        {
            "config" => TableSourceEnum.Config,
            "sourcedb" => TableSourceEnum.SourceDb,
            _ => throw new ArgumentException(
                "Must specify TableSource = 'Config' or 'SourceDb' in [ResetSchemaOptions]")
        };
    }
}