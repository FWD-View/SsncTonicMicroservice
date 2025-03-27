#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tonic.Common.Models;
using Tonic.Subsetter;
using Tonic.Subsetter.Utils;

namespace Tonic.Test.Utils;

public static class SubsetTestHelper
{
    public static readonly char[] Digits = { '_', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    public static List<Table> CreateTables(ISubsetConfig config)
    {
        var tables = config.ForeignKeys
            .SelectMany(
                fk => new[]
                {
                    new Table(fk.PrimaryKeyHostCategory, fk.PrimaryKeyTable),
                    new Table(fk.ForeignKeyHostCategory, fk.ForeignKeyTable)
                }).Distinct().ToList();
        return tables;
    }

    public static Dictionary<Table, ImmutableArray<Column>> CreateTableColumns(IEnumerable<Table> tables,
        ISubsetConfig config)
    {
        var columnIndex = 0;
        return tables.Aggregate(new Dictionary<Table, ImmutableArray<Column>>(), (lookup, table) =>
        {
            if (lookup.ContainsKey(table)) return lookup;
            var primaryKeys = config.ForeignKeys.Where(table.IsForeignKeyOf).SelectMany(fk => fk.ForeignKeyColumns)
                .ToHashSet();
            var foreignKeys = config.ForeignKeys.Where(table.IsPrimaryKeyOf).SelectMany(fk => fk.PrimaryKeyColumns)
                .ToHashSet();
            var tableColumns = primaryKeys.Union(foreignKeys)
                .Union(Enumerable.Range(columnIndex, columnIndex + 3).Select(i => $"column_{i}"));
            var orderedColumns = tableColumns.OrderBy(c => c).ToList();
            if (table.TableName == "ITEMS")
            {
                var sellerColumnIdx = orderedColumns.FindIndex(c => c == "SELLER");
                if (sellerColumnIdx != -1)
                {
                    var sellerColumn = orderedColumns.Single(c => c == "SELLER");
                    orderedColumns.Remove(sellerColumn);
                    orderedColumns = orderedColumns.Prepend(sellerColumn).ToList();
                }
            }

            lookup[table] = orderedColumns
                .Select(c => new Column(table.HostCategory, table.TableName, c, "number", false))
                .ToImmutableArray();
            columnIndex += 3;

            return lookup;
        });
    }

    public static Dictionary<MultiStringKey, HashSet<string?[]>> SeedTableCaches(
        HashSet<PrimaryKeySet> primaryKeySets, IPrimaryKeyCaches primaryKeyCaches,
        SeedCacheOptions? seedOptions = null)
    {
        var options = seedOptions ?? new SeedCacheOptions();
        var pkCacheReference = new Dictionary<MultiStringKey, HashSet<string?[]>>();
        foreach (var primaryKeySet in primaryKeySets)
        {
            var table = primaryKeySet.Table;
            var limit = options.LimitLookup.ContainsKey(table) ? options.LimitLookup[table] : 10;
            foreach (var keySet in primaryKeySet.SubsidiaryMultiStringKeys)
            {
                if (keySet.Length <= 2) continue;
                if (!pkCacheReference.ContainsKey(keySet))
                {
                    pkCacheReference[keySet] = new HashSet<string?[]>();
                }

                var cache = primaryKeyCaches.CreateCache(keySet);
                var primaryKeys = keySet.From(2).Parts;
                using var pkConn = primaryKeyCaches.OpenConnection();
                using var addKeyContext = cache.PrepareAddKeys(pkConn, false);
                for (var i = 0; i < limit; i += options.Stride)
                {
                    var key = new string?[primaryKeys.Length];
                    for (var j = 0; j < primaryKeys.Length; j++)
                    {
                        var isNull = options.ColumnsToNull.Contains((primaryKeys[j], i));
                        key[j] = isNull ? null : $"{primaryKeys[j]}_{i}";
                    }

                    cache.AddKey(key, addKeyContext);
                    pkCacheReference[keySet].Add(key);
                }
            }
        }

        return pkCacheReference;
    }

    public static ImmutableArray<string> ComputePrimaryKeyColumns(IList<ForeignKey> foreignKeys, Table table)
    {
        var pkTableKeys = foreignKeys.Where(table.IsPrimaryKeyOf)
            .SelectMany(fk => fk.PrimaryKeyColumns).ToList();
        if (pkTableKeys.Any())
        {
            return pkTableKeys
                .OrderBy(i => i)
                .Distinct()
                .ToImmutableArray();
        }

        var fkTableKeys =
            foreignKeys.Where(fk => table.IsForeignKeyOf(fk) && fk.MultiTableIndexColumnIndices.Any()).ToList();
        if (fkTableKeys.Any())
        {
            return fkTableKeys.SelectMany(fk => fk.MultiTableIndexColumnIndices.Zip(fk.ForeignKeyColumns))
                .OrderBy(tuple => tuple.First).Select(tuple => tuple.Second).ToImmutableArray();
        }

        return ImmutableArray<string>.Empty;
    }
}