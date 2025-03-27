using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nett;
using Serilog;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;

namespace Tonic.Common.Extensions;

public static class ForeignKeyToml
{
    public static IList<ForeignKey> ParseForeignKeys(this TomlTable config, bool ignoreTableGroups = false)
    {
        if (!config.ContainsKey("ForeignKey")) return new List<ForeignKey>();

        var foreignKeys = new List<ForeignKey>();
        var foreignKeysToml = (TomlTableArray)config["ForeignKey"];
        for (var i = 0; i < foreignKeysToml.Count; ++i)
        {
            var fk = foreignKeysToml[i];
            if (!ignoreTableGroups && fk.ContainsKey("TableGroupSize"))
            {
                var count = fk.Get<int>("TableGroupSize");
                for (var idx = 0; idx < count; ++idx)
                {
                    foreignKeys.Add(FromToml(fk, idx));
                }
            }
            else foreignKeys.Add(FromToml(fk));
        }


        var multiTableKeys = foreignKeys.Where(fk => fk.MultiTableIndexColumnIndices.Any()).ToList();
        if (multiTableKeys.Any())
        {
            VerifyMultiTableForeignKeys(multiTableKeys);
        }


        return foreignKeys.ToImmutableArray();
    }

    private static ForeignKey FromToml(TomlTable toml, int? idx = null)
    {
        var fkTablePattern = toml.Get<string>("ForeignKeyTable");
        var pkTablePattern = toml.Get<string>("PrimaryKeyTable");
        var fkCastTypes = ImmutableDictionary<string, string>.Empty;
        if (toml.ContainsKey("ForeignKeyColumnCastType"))
        {
            var tomlForeignKeyColumnCastTypes = toml.Get<TomlTable>("ForeignKeyColumnCastType");
            foreach (var (key, value) in tomlForeignKeyColumnCastTypes)
            {
                if (value.TomlType != TomlObjectType.String)
                {
                    throw new ArgumentException(
                        "Cast type must be specified as a TOML string type in configuration");
                }

                fkCastTypes = fkCastTypes.Add(key, value.Get<string>().ToUpperInvariant());
            }
        }

        var fk = new ForeignKey()
        {
            ForeignKeyHostCategory = toml.Get<string>("ForeignKeyHostCategory"),
            ForeignKeyTable = idx.HasValue && fkTablePattern.Contains('0')
                ? Utilities.InterpolateTableName(fkTablePattern, idx.Value)
                : fkTablePattern,
            ForeignKeyColumns = toml.Get<TomlArray>("ForeignKeyColumn").To<string>().ToImmutableArray(),
            ForeignKeyColumnCastTypes = fkCastTypes,
            ForeignKeyProcessor = toml.ContainsKey("ForeignKeyProcessor")
                ? IForeignKeyProcessor.FromString(toml.Get<string>("ForeignKeyProcessor"))
                : IForeignKeyProcessor.Default,
            PrimaryKeyHostCategory = toml.Get<string>("PrimaryKeyHostCategory"),
            PrimaryKeyTable = idx.HasValue && pkTablePattern.Contains('0')
                ? Utilities.InterpolateTableName(pkTablePattern, idx.Value)
                : pkTablePattern,
            PrimaryKeyColumns = toml.Get<TomlArray>("PrimaryKeyColumn").To<string>().ToImmutableArray(),
            IsPrincipal = toml.ContainsKey("IsPrincipal") && toml.Get<bool>("IsPrincipal"),
            IsSecondary = toml.ContainsKey("IsSecondary") && toml.Get<bool>("IsSecondary"),
            StructuredKeyName = toml.ContainsKey("StructuredKeyName")
                ? toml.Get<string>("StructuredKeyName")
                : string.Empty,
            Grouped = idx.HasValue,
            MultiTableIndexColumnIndices = toml.ContainsKey("MultiTableKeyColumnIndexes")
                ? toml.Get<TomlArray>("MultiTableKeyColumnIndexes").To<int>().ToImmutableArray()
                : new List<int>().ToImmutableArray(),
        };
        fk.VerifyKeyConfiguration();
        return fk;
    }

    private static void VerifyKeyConfiguration(this ForeignKey foreignKey)
    {
        string? message = null;
        if (foreignKey.IsPrincipal && !foreignKey.ForeignKeyProcessor.Equals(IForeignKeyProcessor.Default))
            message = "Principal foreign keys cannot use a non-default FKProcessor.";
        else if (foreignKey.IsPrincipal && foreignKey.IsSecondary)
            message = "Cannot have a foreign key be both IsPrincipal and IsSecondary";
        else if (!string.IsNullOrEmpty(foreignKey.StructuredKeyName) && foreignKey.PrimaryKeyColumns.Length > 1)
            message = "Cannot have structured composite keys with multiple pk columns";

        if (message != null) throw new ArgumentException(message);
    }

    private static void VerifyMultiTableForeignKeys(List<ForeignKey> foreignKeys)
    {
        if (foreignKeys.Count > 3)
        {
            throw new ArgumentException("Multi-table foreign key limit exceeded");
        }

        var indexes = new List<int>();

        foreach (var fk in foreignKeys)
        {
            if (fk.MultiTableIndexColumnIndices.Length != 1 || fk.PrimaryKeyColumns.Length != 1 ||
                fk.ForeignKeyColumns.Length != 1)
            {
                throw new ArgumentException("Limit 1 multi-table index per [[ForeignKey]] definition");
            }

            if (fk.IsSecondary)
            {
                throw new ArgumentException("Cannot use multi-table indexes with IsSecondary");
            }

            indexes.Add(fk.MultiTableIndexColumnIndices[0]);
        }

        if (!indexes.SequenceEqual(Enumerable.Range(0, indexes.Count)))
        {
            throw new ArgumentException("Invalid column sequence detected for Multi-Table Index");
        }
    }

    public static Dictionary<string, string> ParseHostToWorkspace(this TomlTable tomlConfig)
    {
        Log.Information("ParseHostToWorkspace");
        var lhToWorkspaceConfig = tomlConfig.Get<TomlTable>("HostCategoryToWorkspace");
        Dictionary<string, string> retval = new();
        foreach (var (hostCategory, value) in lhToWorkspaceConfig)
        {
            var nextVal = value.Get<string>();
            Log.Information("ParseHostToWorkspace: {A}: {B}", hostCategory, nextVal);
            retval[hostCategory] = nextVal;
        }

        return retval;
    }
}