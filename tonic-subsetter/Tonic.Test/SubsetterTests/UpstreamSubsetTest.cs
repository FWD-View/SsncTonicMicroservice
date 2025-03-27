#nullable enable
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Tonic.Subsetter;
using Tonic.Subsetter.Actions;
using Tonic.Subsetter.Utils;
using Tonic.Test.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Tonic.Test;

public record UpstreamParams
{
    public Dictionary<Table, ImmutableArray<Column>> Columns { get; init; } = null!;
    public PrimaryKeySet PkSet { get; init; } = null!;
    public PrimaryKeyCaches PrimaryKeyCaches { get; init; } = null!;
    public HashSet<PrimaryKeySet> PrimaryKeySets { get; init; } = null!;
    public BlockingCollection<(string, Dictionary<string, string>)> QueriesQueue { get; init; } = null!;
    public List<Table> Tables { get; init; } = null!;
    public Table TableUnderTest { get; init; } = null!;
}

[CollectionDefinition("UpstreamSubsetTest Collection", DisableParallelization = true)]
public class UpstreamSubsetTest
{
    public UpstreamSubsetTest(ITestOutputHelper output)
    {
        var loggerConfiguration = new LoggerConfiguration();
        Log.Logger = loggerConfiguration
            .MinimumLevel
            .Debug()
            .WriteTo.Console()
            .WriteTo.TestOutput(output)
            .CreateLogger();
    }


    private static (UpstreamAction, UpstreamParams) Init(ISubsetConfig config, string tableName,
        SeedCacheOptions? cacheOptions = null)
    {
        var seedCacheOptions = cacheOptions ?? new SeedCacheOptions();
        var tables = SubsetTestHelper.CreateTables(config);
        var columns = SubsetTestHelper.CreateTableColumns(tables, config);
        var primaryKeySets = tables.Select(t => new PrimaryKeySet(t, columns[t], config.ForeignKeys)).ToHashSet();
        var upstreamParams = new UpstreamParams
        {
            Tables = tables,
            Columns = columns,
            TableUnderTest = tables.First(t => t.TableName == tableName),
            PrimaryKeySets = primaryKeySets,
            PrimaryKeyCaches = new PrimaryKeyCaches(Utilities.RunId()),
            QueriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>(),
            PkSet = primaryKeySets.Single(ks => ks.Table.TableName == tableName)
        };
        SubsetTestHelper.SeedTableCaches(primaryKeySets, upstreamParams.PrimaryKeyCaches, seedCacheOptions);
        return (new UpstreamAction(config, upstreamParams.PrimaryKeyCaches, upstreamParams.PkSet), upstreamParams);
    }

    [Fact]
    public void TestEmptyPrimaryKeySetCreation()
    {
        const string tableName = "TEST_TABLE";
        var config = CreateUpstreamConfig("fks_test_empty_pk_key_set.toml");
        var (_, upstreamParams) = Init(config, tableName);

        // "TEST_TABLE_3", "TEST_TABLE_2", "TEST_TABLE_1", "TEST_TABLE_4" have no cols referenced as FK's, should have no key sets
        var expectedEmptyInvyKeySets = new HashSet<string>
                { "TEST_TABLE_3", "TEST_TABLE_2", "TEST_TABLE_1", "TEST_TABLE_4" }
            .ToImmutableHashSet();
        var actualEmptyInvyKeySets = upstreamParams.PrimaryKeySets
            .Where(pks => !pks.SubsidiaryMultiStringKeys.Any() && pks.Table.TableName.StartsWith("TEST_TABLE_"))
            .Select(pks => pks.Table.TableName)
            .ToImmutableHashSet();
        Assert.Equal(expectedEmptyInvyKeySets, actualEmptyInvyKeySets);

        // "TEST_TABLE_3", "TEST_TABLE_2", "TEST_TABLE_1", "TEST_TABLE_4" point to TEST_TABLE
        var actualDistinctPrimaryKeySets = upstreamParams.PrimaryKeySets
            .Where(pks => pks.SubsidiaryMultiStringKeys.Count == 3)
            .SelectMany(pkSet => pkSet.SubsidiaryMultiStringKeys).ToImmutableHashSet();
        var expectedDistinctPrimaryKeySets = new HashSet<MultiStringKey>
        {
            new(upstreamParams.TableUnderTest, new[] { "B", "C" }),
            new(upstreamParams.TableUnderTest, new[] { "A", "B" }),
            new(upstreamParams.TableUnderTest, new[] { "B" })
        }.ToImmutableHashSet();
        Assert.Equal(expectedDistinctPrimaryKeySets, actualDistinctPrimaryKeySets);
    }

    [Fact]
    public void TestMultiTableKeyBatchSimple()
    {
        const string tableName = "MULTI_PK_TEST_TABLE";
        var config = CreateUpstreamConfig("fks_test_multi_pk_upstreams.toml");
        config.QueryBatchSize = 3;
        var (upstreamAction, upstreamParams) = Init(config, tableName);

        var keyBatch = upstreamAction
            .MultiTableKeyBatch(upstreamParams.PkSet.PrincipalForeignKeys, upstreamParams.PkSet.PrincipalKeyTables)
            .ToList();
        Assert.Equal(64, keyBatch.Count);

        var expectedTables = new HashSet<Table>
        {
            new("TestHost", "TABLE_1"),
            new("OtherHost", "TABLE_2"),
            new("OtherHost", "TABLE_3")
        };

        foreach (var batch in keyBatch)
        {
            var actualTables = batch.Keys.ToHashSet();
            Assert.Equal(expectedTables, actualTables);
            var batchTuples = batch.Select((x, i) => (x, i));
            foreach (var ((table, value), i) in batchTuples)
            {
                if (value.Count == 3) continue;
                if (value.Count != 1)
                    throw new InvalidDataException($"Invalid batch value count {value.Count}");
                switch (i)
                {
                    case 0:
                        Assert.Equal("TABLE_1", table.TableName);
                        Assert.Equal("TABLE_1_ID_9", value.First());
                        break;
                    case 1:
                        Assert.Equal("TABLE_2", table.TableName);
                        Assert.Equal("TABLE_2_ID_9", value.First());
                        break;
                    case 2:
                        Assert.Equal("TABLE_3", table.TableName);
                        Assert.Equal("TABLE_3_ID_9", value.First());
                        break;
                    default:
                        throw new InvalidDataException($"Invalid batch value count {value.Count}");
                }
            }
        }
    }

    [Fact]
    public void TestMultiTableKeyBatchJaggedKeys()
    {
        const string tableName = "MULTI_PK_TEST_TABLE";
        var config = CreateUpstreamConfig("fks_test_multi_pk_upstreams.toml");
        config.QueryBatchSize = 3;
        var cacheLimits = new Dictionary<Table, int>
        {
            [new Table("TestHost", "TABLE_1")] = 5,
            [new Table("OtherHost", "TABLE_2")] = 7,
            [new Table("OtherHost", "TABLE_3")] = 4
        };
        var options = new SeedCacheOptions
        {
            LimitLookup = cacheLimits
        };
        var (upstreamAction, upstreamParams) = Init(config, tableName, options);

        var keyBatch = upstreamAction
            .MultiTableKeyBatch(upstreamParams.PkSet.PrincipalForeignKeys, upstreamParams.PkSet.PrincipalKeyTables);

        var expectedTables = new HashSet<Table>
        {
            new("TestHost", "TABLE_1"),
            new("OtherHost", "TABLE_2"),
            new("OtherHost", "TABLE_3")
        };

        foreach (var batch in keyBatch)
        {
            var actualTables = batch.Keys.ToHashSet();
            Assert.Equal(expectedTables, actualTables);
            var batchTuples = batch.Select((x, i) => (x, i));
            foreach (var ((table, value), i) in batchTuples)
            {
                if (value.Count == 3) continue;
                if (value.Count == 2)
                {
                    Assert.Equal("TABLE_1_ID_3", value[0]);
                    Assert.Equal("TABLE_1_ID_4", value[1]);
                    break;
                }

                if (value.Count != 1)
                    throw new InvalidDataException($"Invalid batch value count {value.Count}");
                switch (i)
                {
                    case 0:
                        Assert.Equal("TABLE_1", table.TableName);
                        Assert.Equal("TABLE_1_ID_4", value.First());
                        break;
                    case 1:
                        Assert.Equal("TABLE_2", table.TableName);
                        Assert.Equal("TABLE_2_ID_6", value.First());
                        break;
                    case 2:
                        Assert.Equal("TABLE_3", table.TableName);
                        Assert.Equal("TABLE_3_ID_3", value.First());
                        break;
                    default:
                        throw new InvalidDataException($"Invalid batch value count {value.Count}");
                }
            }
        }
    }

    [Fact]
    public void TestMultiTablePrincipalUpstreamQueries()
    {
        const string tableName = "MULTI_PK_TEST_TABLE";
        var config = CreateUpstreamConfig("fks_test_multi_pk_upstreams.toml");
        var (upstreamAction, upstreamParams) = Init(config, tableName);
        config.QueryBatchSize = 3;
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>();

        upstreamAction.GenerateMultiPrincipalUpstreamQueries(queriesQueue).Wait();
        var upstreamQueries = queriesQueue.ToList();
        Assert.Equal(64, upstreamQueries.Count);
        /*language=text*/
        var regex = new Regex("^SELECT ((MULTI_PK_TEST_TABLE..*)) FROM (.*) WHERE (((?:| AND ).*(?:| AND )))$");
        AssertUpstreamQueriesCorrect(upstreamQueries, regex, upstreamParams.Columns, upstreamParams.TableUnderTest, config,
            upstreamParams.PrimaryKeySets);
    }

    [Fact]
    public void TestCompositeKeyPrincipalUpstreamQueries()
    {
        const string tableName = "TEST_UPSTREAM_TABLE";
        var config = CreateUpstreamConfig("fks_test_composite_key_upstreams.toml");
        var (upstreamAction, upstreamParams) = Init(config, tableName);
        config.QueryBatchSize = 3;
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>();

        upstreamAction.GeneratePrincipalUpstreamQueries(queriesQueue).Wait();
        var upstreamQueries = queriesQueue.ToList();
        Assert.Equal(4, upstreamQueries.Count);
        var regex = new Regex($"^SELECT (({tableName}..*)) FROM (.*) WHERE (.*)$");
        AssertUpstreamQueriesCorrect(upstreamQueries, regex, upstreamParams.Columns, upstreamParams.TableUnderTest, config,
            upstreamParams.PrimaryKeySets);
    }


    [Fact]
    public void TestMultiTablePrincipalParentsUpstreamQueries()
    {
        const string tableName = "TEST_TABLE";
        var config = CreateUpstreamConfig("fks_test_multi_pk_upstream_parents.toml");
        var (_, upstreamParams) = Init(config, tableName);
        config.QueryBatchSize = 3;

        var relevantTables = config.ForeignKeys
            .Where(fk => fk.ForeignKeyHostCategory is "TestHost" && fk.ForeignKeyTable != tableName)
            .Select(fk => upstreamParams.Tables.Single(t => t.TableName == fk.ForeignKeyTable));
        foreach (var table in relevantTables)
        {
            var pkSet = upstreamParams.PrimaryKeySets.Single(ks => ks.Table.Equals(table));
            var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>();
            var upstreamAction = new UpstreamAction(config, upstreamParams.PrimaryKeyCaches, pkSet);
            upstreamAction.GeneratePrincipalUpstreamQueries(queriesQueue).Wait();
            var upstreamQueries = queriesQueue.ToArray();
            Assert.Equal(4, upstreamQueries.Length);
            var regex = new Regex($"^SELECT (({table.TableName}..*)) FROM (.*) WHERE (.*)$");
            AssertUpstreamQueriesCorrect(upstreamQueries, regex, upstreamParams.Columns, table, config,
                upstreamParams.PrimaryKeySets);
        }
    }

    [Fact]
    public void TestUpstreamGroupLimitQueries()
    {
        const string tableName = "TESTTABLE";
        var config = CreateUpstreamConfig("fks_test_upstream_group_limit.toml");
        var (upstreamAction, upstreamParams) = Init(config, tableName);
        config.QueryBatchSize = 3;
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>();

        upstreamAction.GeneratePrincipalUpstreamQueries(queriesQueue).Wait();
        var upstreamQueries = queriesQueue.ToArray();
        Assert.Equal(4, upstreamQueries.Length);
        /*language=text*/
        var regex = new Regex(
            "^SELECT ((TONIC__SUBSELECT..*)) FROM (?:.*) FROM ({{TONIC_SCHEMA_TOKEN}}\\.[A-Z]+)\\s+WHERE (?:[A-Z]+)\\.(.*)\\)",
            RegexOptions.Singleline);
        AssertUpstreamQueriesCorrect(upstreamQueries, regex, upstreamParams.Columns, upstreamParams.TableUnderTest, config,
            upstreamParams.PrimaryKeySets);
    }

    [Fact]
    public void TestFkProcessorUpstreamQueries()
    {
        const string tableName = "FK_PROCESSOR_TABLE";
        var config = CreateUpstreamConfig("fks_test_fkprocessor.toml");
        var (upstreamAction, upstreamParams) = Init(config, tableName);
        config.QueryBatchSize = 3;
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>();

        var queryTask = upstreamAction.GeneratePrincipalUpstreamQueries(queriesQueue);
        queryTask.Wait();
        var upstreamQueries = queriesQueue.ToArray();
        Assert.Equal(4, upstreamQueries.Length);
        var regex = new Regex($"^SELECT (({tableName}..*)) FROM (.*) WHERE (.*)$");
        AssertUpstreamQueriesCorrect(upstreamQueries, regex, upstreamParams.Columns, upstreamParams.TableUnderTest, config,
            upstreamParams.PrimaryKeySets);
    }

    [Fact]
    public void TestUpstreamQuerySchemaOverride()
    {
        const string tableName = "TEST_UPSTREAM_TABLE";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "direct_targets_standard.toml",
                "lh_test_schema_override.toml",
                "fk_test_schema_overrides.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var (upstreamAction, upstreamParams) = Init(config, tableName);

        var queryTask = upstreamAction.GeneratePrincipalUpstreamQueries(upstreamParams.QueriesQueue);
        queryTask.Wait();

        foreach (var hostConfig in config.SourceHostConfigs)
        {
            var host = new Host(hostConfig.HostCategory, hostConfig,
                ImmutableArray<SchemaOverride>.Empty);
            var schemaOverride =
                config.SchemaOverrides.FirstOrDefault(o => host.Configuration.Host == o.Host);
            foreach (var (query, _) in upstreamParams.QueriesQueue)
            {
                Assert.DoesNotContain(Constants.TonicSchemaToken, query);
                var (overriddenQuery, newSid) = host.ReplaceSchemaTokens(query, config.SchemaOverrides);
                if (hostConfig.Host == "localhost2")
                    Assert.Equal("NEW_SID", newSid);
                Assert.DoesNotContain(SchemaOverride.OverrideSchemaToken, overriddenQuery);
                Assert.DoesNotContain(Constants.TonicSchemaToken, overriddenQuery);
                var expectedSchema = schemaOverride?.Schema ?? host.Configuration.Schema;
                Assert.Contains(expectedSchema, overriddenQuery);
            }
        }
    }

    [Fact]
    public void TestUpstreamMultiTableQuerySchemaOverride()
    {
        const string tableName = "MULTI_PK_TEST_TABLE";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "direct_targets_standard.toml",
                "lh_test_schema_override.toml",
                "fk_test_schema_overrides.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var (upstreamAction, upstreamParams) = Init(config, tableName);

        upstreamAction.GenerateMultiPrincipalUpstreamQueries(upstreamParams.QueriesQueue).Wait();

        foreach (var hostConfig in config.SourceHostConfigs)
        {
            var host = new Host(hostConfig.HostCategory, hostConfig,
                ImmutableArray<SchemaOverride>.Empty);
            var schemaOverride =
                config.SchemaOverrides.FirstOrDefault(o => host.Configuration.Host == o.Host);
            foreach (var (query, _) in upstreamParams.QueriesQueue)
            {
                Assert.DoesNotContain(Constants.TonicSchemaToken, query);
                var (overriddenQuery, newSid) = host.ReplaceSchemaTokens(query, config.SchemaOverrides);
                if (host.Configuration.Host == "localhost2")
                    Assert.Equal("NEW_SID", newSid);
                Assert.DoesNotContain(SchemaOverride.OverrideSchemaToken, overriddenQuery);
                Assert.DoesNotContain(Constants.TonicSchemaToken, overriddenQuery);
                var expectedSchema = schemaOverride?.Schema ?? host.Configuration.Schema;
                Assert.Contains(expectedSchema, overriddenQuery);
            }
        }
    }

    private static ISubsetConfig CreateUpstreamConfig(string fileName)
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(fileName)
            .ToConcatenatedStream();
        return configFileStream.ParseSubsetConfig();
    }


    private static void AssertUpstreamQueriesCorrect(
        IEnumerable<(string, Dictionary<string, string>)> upstreamQueries, Regex regex,
        IReadOnlyDictionary<Table, ImmutableArray<Column>> columns, Table tableUnderTest, ISubsetConfig config,
        IReadOnlyCollection<PrimaryKeySet> primaryKeySets)
    {
        var principalFk = config.ForeignKeys.First(fk => tableUnderTest.IsForeignKeyOf(fk) && fk.IsPrincipal);
        var principalKeySet = primaryKeySets.First(set => set.Table.TableName == principalFk.PrimaryKeyTable);
        foreach (var (query, paramsDict) in upstreamQueries)
        {
            // Assert query has shape expected (we can extract 3 capture groups)
            Assert.Matches(regex, query);
            var matches = regex.Match(query);
            var captures = matches.Groups.Values.Select(g => g.Value).Skip(2).ToArray();

            // Assert we query for all columns on table
            var capturedColumns = captures[0].Split(", ").Select(s => s.Split('.')[1]).ToImmutableHashSet();
            var expectedColumns = columns[tableUnderTest]
                .Select(c => c.ColumnName)
                .ToImmutableHashSet();
            Assert.Equal(expectedColumns, capturedColumns);

            // Assert we query correct table
            var capturedTable = captures[1].Split(".")[1];
            Assert.Equal(tableUnderTest.TableName, capturedTable);

            // Assert we query on primary key set of table
            var clauses = captures[2].Split(" AND ");
            if (clauses.Length == 1)
            {
                AssertSingleTablePkColumns(captures, principalFk, principalKeySet, paramsDict);
                return;
            }

            var pkColumns = captures[0]
                .Split(", ")
                .Select(c => c.Split(".")[1])
                .Where(c => !c.StartsWith("column_"))
                .OrderBy(s => s)
                .ToArray();

            // Assert pk set of table maps to pk set of principal table
            var expectedPkColumns = primaryKeySets.Single(s => s.Table.Equals(tableUnderTest))
                .PrincipalForeignKeys
                .SelectMany(fk => fk.ForeignKeyColumns)
                .OrderBy(x => x).ToArray();
            Assert.Equal(expectedPkColumns, pkColumns);
            var namedParams = new List<string>();
            var passedParams = paramsDict.Keys.OrderBy(x => x);
            foreach (var clause in clauses)
            {
                var clauseParts = clause.Split(" IN ");

                var param = clauseParts[1]
                    .Substring(1, clauseParts[1].Length - 2)
                    .Split("),(")
                    .SelectMany(s => s.Split(", "));
                namedParams.AddRange(param);
            }

            // Assert query params map to passed params
            var actualParams = namedParams.Distinct().OrderBy(x => x);
            Assert.Equal(passedParams, actualParams);
        }
    }

    private static void AssertSingleTablePkColumns(string[] captures, ForeignKey principalFk,
        PrimaryKeySet? principalKeySet,
        Dictionary<string, string>? paramsDict)
    {
        var pkColumnMapping = principalFk.ForeignKeyColumns.Zip(principalFk.PrimaryKeyColumns,
            (p, f) => new { Primary = p, Foreign = f }).ToDictionary(c => c.Primary, c => c.Foreign);
        var clauseParts = captures[2].Split(" IN ");
        var requestedColumns =
            (principalFk.PrimaryKeyColumns.Length == 1
                ? clauseParts[0]
                : clauseParts[0].Substring(1, clauseParts[0].Length - 2)).Split(", ");
        var pkColumns = requestedColumns.Where(c => !c.StartsWith("column_")).OrderBy(s => s).ToArray();

        // Assert pk set of table maps to pk set of principal table
        var actualPrincipalMultiStringKey =
            new MultiStringKey(principalKeySet!.Table, pkColumns.Select(c => pkColumnMapping[c]));
        Assert.Contains(actualPrincipalMultiStringKey, principalKeySet.SubsidiaryMultiStringKeys);

        // Assert query params map to passed params
        var namedParams = (principalFk.PrimaryKeyColumns.Length == 1
                ? clauseParts[1].Substring(1, clauseParts[1].Length - 3)
                : clauseParts[1].Substring(2, clauseParts[1].Length - 5))
            .Split("),(").SelectMany(s => s.Split(",")).ToImmutableHashSet();
        var passedParams = paramsDict!.Keys.ToImmutableHashSet();
        Assert.Equal(passedParams, namedParams);

        // Assert passed query params map to values of principal key columns
        var subsetTableExpectedValues = pkColumns.Select(c => $"{pkColumnMapping[c]}").ToImmutableHashSet();
        var subsetTableActualValues = paramsDict.Values.Select(s => s.TrimEnd(SubsetTestHelper.Digits))
            .ToImmutableHashSet();
        Assert.Equal(subsetTableExpectedValues, subsetTableActualValues);
    }
}