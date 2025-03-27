using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Tonic.Subsetter;
using Tonic.Subsetter.Utils;
using Tonic.Test.Utils;
using Xunit;

namespace Tonic.Test;

[CollectionDefinition("CopyRowsTest Collection", DisableParallelization = true)]
public class CopyRowsTest
{
    [Fact]
    public void TestForeignKeySchemaTableRestrictions()
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "direct_targets_standard.toml",
                "lh_test_schema_override.toml",
                "fk_test_schema_overrides.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var parameters = CopyRowsParameters.Init(config);
        
        foreach (var table in from table in parameters.Tables
                 let configs = config.SourceHostConfigs
                 from lh in configs
                 let isSchemaRestricted = parameters.CopyRowsHelper.IsSchemaRestrictedTable(table, lh.Schema)
                 where isSchemaRestricted
                 select table)
        {
            Assert.StartsWith("BILL", table.TableName);
        }
    }

    [Fact]
    public void TestBuildKeySetCaches()
    {
        const string tableName = "TABLE_2";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles("fks_test_multi_pk_upstreams.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var parameters = CopyRowsParameters.Init(config);
        var tableUnderTest = parameters.Tables.First(t => t.TableName is tableName);

        var runId = Utilities.RunId();
        var referenceKeyCaches = new PrimaryKeyCaches(runId);
        var expectedRows =
            SubsetTestHelper.SeedTableCaches(parameters.PrimaryKeySetLookup.Values.ToHashSet(), referenceKeyCaches);
        var multiStringKeyLookup =
            parameters.PrimaryKeySetLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SubsidiaryMultiStringKeys);
        var pkCacheQueue =
            PopulatePkCacheQueue(multiStringKeyLookup, expectedRows, parameters.Columns, tableUnderTest);
        var buildKeySetCaches =
            parameters.CopyRowsHelper.BuildKeySetCaches(parameters.PrimaryKeySetLookup[tableUnderTest],
                pkCacheQueue);

        buildKeySetCaches.Wait();

        // check all downstream tables primary key sets
        foreach (var keySet in parameters.PrimaryKeySetLookup[tableUnderTest].SubsidiaryMultiStringKeys)
        {
            var actualCache = parameters.KeyCaches.GetCache(keySet);
            var keyColumns = keySet.From(2).Parts;

            var expectedKeySetRows = expectedRows[keySet].OrderBy(k => k[0]);
            var actualKeySetRows = actualCache.GetKeys(keyColumns).Select(k => k);
            foreach (var (expected, actual) in expectedKeySetRows.Zip(actualKeySetRows))
            {
                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void TestEbayBidsBuildKeySetCaches()
    {
        const string tableName = "TABLE_3";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles("fks_test_multi_pk_upstreams.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var parameters = CopyRowsParameters.Init(config);

        var tableUnderTest = parameters.Tables.First(t => t.TableName is tableName);
        var runId = Utilities.RunId();
        var referenceKeyCaches = new PrimaryKeyCaches(runId);

        var expectedRows =
            SubsetTestHelper.SeedTableCaches(parameters.PrimaryKeySetLookup.Values.ToHashSet(), referenceKeyCaches);

        var multiStringKeyLookup =
            parameters.PrimaryKeySetLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SubsidiaryMultiStringKeys);
        var pkCacheQueue =
            PopulatePkCacheQueue(multiStringKeyLookup, expectedRows, parameters.Columns, tableUnderTest);
        var buildKeySetCaches =
            parameters.CopyRowsHelper.BuildKeySetCaches(parameters.PrimaryKeySetLookup[tableUnderTest],
                pkCacheQueue);

        buildKeySetCaches.Wait();

        // check all downstream tables primary key sets
        foreach (var keySet in parameters.PrimaryKeySetLookup[tableUnderTest].SubsidiaryMultiStringKeys)
        {
            var actualCache = parameters.KeyCaches.GetCache(keySet);
            var keyColumns = keySet.From(2).Parts;

            var expectedKeySetRows = expectedRows[keySet].OrderBy(k => k[0]);
            var actualKeySetRows = actualCache.GetKeys(keyColumns).Select(k => k);
            foreach (var (expected, actual) in expectedKeySetRows.Zip(actualKeySetRows))
            {
                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void TestCopyRowsQueries()
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "direct_targets_standard.toml",
                "lh_test_schema_override.toml",
                "fk_test_schema_overrides.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var parameters = CopyRowsParameters.Init(config);
        
        var lhConfigs = config.SourceHostConfigs.Where(c => c.HostCategory == "TestHost").ToList();
        var service = new HostsService(lhConfigs);
        var copyRowsHelper = new CopyRowsService(parameters.RunId, config, service, service, parameters.KeyCaches);
        
        var tables = parameters.Tables;
        
        foreach (var table in tables)
        {
            foreach (var lhc in lhConfigs)
            {
                var isSchemaRestricted = copyRowsHelper.IsSchemaRestrictedTable(table, lhc.Schema);
                if (CopyRowsParameters.TableRestrictions.TryGetValue(table.TableName, out var expectedSchema))
                {
                    if (isSchemaRestricted)
                    {
                        Assert.NotEqual(expectedSchema, lhc.Schema);
                    }
                    else
                    {
                        Assert.Equal(expectedSchema, lhc.Schema);
                    }
                }
                else Assert.False(isSchemaRestricted);
            }
        }
    }

    private static BlockingCollection<string[]> PopulatePkCacheQueue(
        IReadOnlyDictionary<Table, ImmutableHashSet<MultiStringKey>> multiStringKeyLookup,
        IReadOnlyDictionary<MultiStringKey, HashSet<string[]>> expectedRows,
        IReadOnlyDictionary<Table, ImmutableArray<Column>> columns,
        Table tableUnderTest)
    {
        var columnValues = new Dictionary<string, List<string>>();
        var columnNameSet = new HashSet<string>();

        foreach (var msk in multiStringKeyLookup[tableUnderTest])
        {
            var keyColumns = msk.From(2).Parts;
            columnNameSet.UnionWith(keyColumns);
            foreach (var r in expectedRows[msk])
            {
                foreach (var (k, v) in keyColumns.Zip(r))
                {
                    if (!columnValues.ContainsKey(k)) columnValues[k] = new List<string>();
                    if (!columnValues[k].Contains(v)) columnValues[k].Add(v);
                }
            }
        }

        var columnNames = columnNameSet.OrderBy(s => s).ToArray();
        var rowMatrix = new string[columnValues.First().Value.Count, columnValues.Count];
        for (var j1 = 0; j1 < columnValues.First().Value.Count; j1++)
        {
            for (var r = 0; r < columnValues.Count; r++)
                rowMatrix[j1, r] = columnValues[columnNames[r]][j1];
        }

        var rows = Enumerable.Range(0, rowMatrix.GetLength(0))
            .Select(y => Enumerable.Range(0, rowMatrix.GetLength(1)).Select(x => rowMatrix[y, x]).ToArray())
            .ToList();
        var pkCacheQueue = new BlockingCollection<string[]>();
        foreach (var pks in rows)
        {
            var pkIdx = columnNames.Select(pkc => columns[tableUnderTest].FindIndex(c => c.ColumnName == pkc));
            var row = Enumerable.Range(0, columns[tableUnderTest].Length).Select(i => $"{i}").ToArray();
            foreach (var (i, j) in pkIdx.Select((i, j) => (i, j)))
            {
                row[i] = pks[j];
            }

            pkCacheQueue.Add(row);
        }

        pkCacheQueue.CompleteAdding();
        return pkCacheQueue;
    }
}

internal record CopyRowsParameters
{
    public Dictionary<Table, ImmutableArray<Column>> Columns { get; private init; }
    public CopyRowsService CopyRowsHelper { get; private init; }
    public PrimaryKeyCaches KeyCaches { get; private init; }
    public Dictionary<Table, PrimaryKeySet> PrimaryKeySetLookup { get; private init; }
    public string RunId { get; private init; }
    public List<Table> Tables { get; private init; }

    public static CopyRowsParameters Init(ISubsetConfig config)
    {
        var runId = Utilities.RunId();
        var mockHosts = config.SourceHostConfigs
            .Select(cfg => MockBuilder.Host().WithConfig(cfg).Object).ToList();
        var hostManager = MockBuilder.HostManager(mockHosts).WithRunOnMultiplexedCategories();
        var primaryKeyCaches = new PrimaryKeyCaches(runId);
        var tables = SubsetTestHelper.CreateTables(config);
        var columns = SubsetTestHelper.CreateTableColumns(tables, config);
        return new CopyRowsParameters
        {
            Tables = tables,
            RunId = runId,
            KeyCaches = primaryKeyCaches,
            CopyRowsHelper = new CopyRowsService(runId, config, hostManager.Object,
                hostManager.Object, primaryKeyCaches),
            Columns = columns,
            PrimaryKeySetLookup =
                tables.ToDictionary(t => t, t => new PrimaryKeySet(t, columns[t], config.ForeignKeys))
        };
    }

    public static readonly Dictionary<string, string> TableRestrictions = new()
    {
        ["TEST_TABLE_00"] = "TEST10_USER",
        ["TEST_TABLE_01"] = "TEST11_USER",
        ["TEST_TABLE_02"] = "TEST12_USER",
        ["TEST_TABLE_03"] = "TEST13_USER",
        ["TEST_TABLE_04"] = "TEST14_USER",
        ["TEST_TABLE_05"] = "TEST15_USER",
        ["TEST_TABLE_06"] = "TEST16_USER",
        ["TEST_TABLE_07"] = "TEST17_USER",
        ["TEST_TABLE_08"] = "TEST18_USER",
        ["TEST_TABLE_09"] = "TEST19_USER",
        ["TEST_TABLE_10"] = "TEST20_USER",
        ["TEST_TABLE_11"] = "TEST21_USER",
        ["OTHER_TEST_TABLE_00"] = "TEST10_USER",
        ["OTHER_TEST_TABLE_01"] = "TEST11_USER",
        ["OTHER_TEST_TABLE_02"] = "TEST12_USER",
        ["OTHER_TEST_TABLE_03"] = "TEST13_USER",
        ["OTHER_TEST_TABLE_04"] = "TEST14_USER",
        ["OTHER_TEST_TABLE_05"] = "TEST15_USER",
        ["OTHER_TEST_TABLE_06"] = "TEST16_USER",
        ["OTHER_TEST_TABLE_07"] = "TEST17_USER",
        ["OTHER_TEST_TABLE_08"] = "TEST18_USER",
        ["OTHER_TEST_TABLE_09"] = "TEST19_USER",
        ["OTHER_TEST_TABLE_10"] = "TEST20_USER",
        ["OTHER_TEST_TABLE_11"] = "TEST21_USER"
    };
}