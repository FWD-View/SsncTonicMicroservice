#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Subsetter;
using Tonic.Subsetter.Actions;
using Tonic.Subsetter.Utils;
using Tonic.Test.Utils;
using Tonic.Test.Utils.MockBuilders;
using Xunit;

namespace Tonic.Test;

public class DownstreamSubsetTest
{
    private DownstreamAction _downstreamAction = null!;
    private IHostsService _hostsService = null!;
    private DownstreamParams _params = null!;


    private void Init(ISubsetConfig config, Dictionary<string, string> categoryToSchemaMappings,
        Func<string, DbDataReader> mockDbDataReader)
    {
        var mockHosts = config.SourceHostConfigs.Select(cfg =>
        {
            var mockHost = MockBuilder.Host()
                .WithConfig(cfg)
                .WithExecuteQuery(mockDbDataReader)
                .WithExecuteNonQuery()
                .Object;
            return mockHost;
        }).AsEnumerable();

        _hostsService = MockBuilder.HostManager(mockHosts)
            .WithRunOnCategory()
            .WithHostsEnumerator().Object;
        _downstreamAction = new DownstreamAction(
            config,
            _params.PkSet,
            _params.PrimaryKeyCaches,
            config.QueryBatchSize,
            _hostsService,
            categoryToSchemaMappings
        );
    }


    [Fact]
    public void TestSelectsForHosts()
    {
        const string tableName = "TEST_TABLE";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_empty_pk_key_set.toml");
        var downstreamParams = DownstreamParams.InitialCreate(config);

        var tableUnderTest = new Table("OtherHost", tableName);
        var expectedKeys = new HashSet<MultiStringKey>
        {
            new(tableUnderTest, new[] { "B", "C" }),
            new(tableUnderTest, new[] { "B" }),
            new(tableUnderTest, new[] { "A", "B" }),
        }.ToImmutableHashSet();

        var hostService = new HostsService(config.DestinationHostConfigs);

        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);
        var primaryKeySet = downstreamParams.PrimaryKeySets.First(pks => pks.Table.TableName == tableName);
        var selectsByHostKeySet =
            DownstreamAction.CreateHostSelects(primaryKeySet, categoryToSchemaMappings);
        var regex = new Regex("^SELECT ((?:|, ).*) FROM (.*)$");
        foreach (var (multiStringKey, hostSelectLookup) in selectsByHostKeySet)
        {
            Assert.Contains(multiStringKey, expectedKeys);
            foreach (var (hostCategory, (processor, selectStatements)) in hostSelectLookup)
            {
                Assert.Equal(DefaultForeignKeyProcessor.Instance, processor);
                foreach (var statement in selectStatements)
                {
                    Assert.Matches(regex, statement);
                    var captures = regex.Match(statement).Groups.Values.Skip(1).Take(2).Select(x => x.Value)
                        .ToArray();
                    var foundColumns = captures[0].Split(",").Select(s => s.Split(" AS ")[0].Trim()).ToArray();
                    var foundTable = captures[1].Split(".")[1];
                    var relevantFk = config.ForeignKeys.SingleOrDefault(fk =>
                        fk.ForeignKeyHostCategory == hostCategory &&
                        fk.ForeignKeyTable == foundTable && fk.PrimaryKeyColumns.SequenceEqual(foundColumns));
                    Assert.NotNull(relevantFk);
                }
            }
        }
    }

    [Fact]
    public void TestSelectsForHostsGroupingOrder()
    {
        var categoryToSchemaMappings = new Dictionary<string, string>
        {
            ["b"] = "b",
            ["c"] = "c",
            ["d"] = "d",
        };
        var foreignKeys = new List<ForeignKey>
        {
            new()
            {
                ForeignKeyHostCategory = "b",
                ForeignKeyTable = "table_b",
                ForeignKeyColumns = new[] { "b_a_0" }.ToImmutableArray(),
                PrimaryKeyHostCategory = "a",
                PrimaryKeyTable = "table_a",
                PrimaryKeyColumns = new[] { "a_0" }.ToImmutableArray(),
            },
            new()
            {
                ForeignKeyHostCategory = "c",
                ForeignKeyTable = "table_c",
                ForeignKeyColumns = new[] { "c_a_0", "c_a_1" }.ToImmutableArray(),
                PrimaryKeyHostCategory = "a",
                PrimaryKeyTable = "table_a",
                PrimaryKeyColumns = new[] { "a_0", "a_1" }.ToImmutableArray(),
            },
            new()
            {
                ForeignKeyHostCategory = "d",
                ForeignKeyTable = "table_d",
                ForeignKeyColumns = new[] { "d_a_0", "d_a_2" }.ToImmutableArray(),
                PrimaryKeyHostCategory = "a",
                PrimaryKeyTable = "table_a",
                PrimaryKeyColumns = new[] { "a_0", "a_2" }.ToImmutableArray(),
            },
            new()
            {
                ForeignKeyHostCategory = "b",
                ForeignKeyTable = "table_e",
                ForeignKeyColumns = new[] { "e_a_0", "e_a_1", "e_a_2" }.ToImmutableArray(),
                PrimaryKeyHostCategory = "a",
                PrimaryKeyTable = "table_a",
                PrimaryKeyColumns = new[] { "a_0", "a_1", "a_2" }.ToImmutableArray()
            },
            new()
            {
                ForeignKeyHostCategory = "c",
                ForeignKeyTable = "table_f",
                ForeignKeyColumns = new[] { "f_a_0" }.ToImmutableArray(),
                PrimaryKeyHostCategory = "a",
                PrimaryKeyTable = "table_a",
                PrimaryKeyColumns = new[] { "a_0" }.ToImmutableArray(),
            },
            new()
            {
                ForeignKeyHostCategory = "d",
                ForeignKeyTable = "table_g",
                ForeignKeyColumns = new[] { "g_a_0", "g_a_2" }.ToImmutableArray(),
                PrimaryKeyHostCategory = "a",
                PrimaryKeyTable = "table_a",
                PrimaryKeyColumns = new[] { "a_0", "a_2" }.ToImmutableArray()
            },
        };

        var table = new Table("a", "table_a");
        var columns = ImmutableArray<Column>.Empty;
        var primaryKeySet = new PrimaryKeySet(table, columns, foreignKeys);
        var selectsByHostKeySet =
            DownstreamAction.CreateHostSelects(primaryKeySet, categoryToSchemaMappings);
        Assert.NotEmpty(selectsByHostKeySet);
    }

    [Fact]
    public void TestDownstreamQuerySchemaOverride()
    {
        const string tableName = "TEST_UPSTREAM_TABLE";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "lh_test_schema_override.toml",
                "fk_test_schema_overrides.toml"
            )
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var mockDbDataReader =
            MockBuilder.DbReader.ForQuery(DbDataReaderBuilder.ParseGenericDownstreamDestinationQueryFirstSubselect);
        _params = DownstreamParams.InitialCreate(config, tableName);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);
        var queryTask = downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        queryTask.Wait();

        var downstreamQueries = _params.QueriesQueue.ToList();

        foreach (var hostConfig in config.SourceHostConfigs)
        {
            var host = new Host(hostConfig.HostCategory, hostConfig,
                ImmutableArray<SchemaOverride>.Empty);
            var schemaOverride =
                config.SchemaOverrides.FirstOrDefault(o => host.Configuration.Host == o.Host);
            foreach (var (query, _) in downstreamQueries)
            {
                Assert.DoesNotContain(Constants.TonicSchemaToken, query);
                var (overriddenQuery, newSid) = host.ReplaceSchemaTokens(query, config.SchemaOverrides);
                if (schemaOverride != null) Assert.Equal("NEW_SID", newSid);
                Assert.DoesNotContain(SchemaOverride.OverrideSchemaToken, overriddenQuery);
                Assert.DoesNotContain(Constants.TonicSchemaToken, overriddenQuery);
                var expectedSchema = schemaOverride?.Schema ?? host.Configuration.Schema;
                Assert.Contains(expectedSchema, overriddenQuery);
            }
        }
    }

    [Fact]
    public void TestBidsWithSchemaOverrides()
    {
        const string tableName = "TABLE_3";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_multi_pk_upstreams.toml");
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);
        var mockDbDataReader =
            MockBuilder.DbReader.ForQuery(DbDataReaderBuilder.ParseGenericDownstreamDestinationQueryFirstSubselect);
        _params = DownstreamParams.InitialCreate(config, tableName);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        var queryTask = downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        queryTask.Wait();

        foreach (var hostConfig in config.SourceHostConfigs)
        {
            var host = new Host(hostConfig.HostCategory, hostConfig,
                ImmutableArray<SchemaOverride>.Empty);
            var schemaOverride =
                config.SchemaOverrides.FirstOrDefault(o =>
                    host.Configuration.Host == o.Host && o.TableName == tableName);
            Assert.Null(schemaOverride);
            foreach (var (query, _) in _params.QueriesQueue)
            {
                Assert.Contains(Constants.TonicSchemaToken, query);
                var (overriddenQuery, _) = host.ReplaceSchemaTokens(query, config.SchemaOverrides);
                Assert.DoesNotContain(SchemaOverride.OverrideSchemaToken, overriddenQuery);
                Assert.Contains(hostConfig.Schema, overriddenQuery);
            }
        }
    }


    [Fact]
    public void TestDownstreamSubsetQueries()
    {
        const string tableName = "TABLE_1";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_multi_pk_upstreams.toml");
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };
        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);
        var mockDbDataReader =
            MockBuilder.DbReader.ForQuery(DownstreamParams.ParseDownstreamUnionQuery);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var principalFk =
            config.ForeignKeys.First(fk => _params.TableUnderTest.IsPrimaryKeyOf(fk) && fk.IsPrincipal);
        AssertDownstreamQueriesCorrect(principalFk, regex, downstreamQueries);
    }


    [Fact]
    public void TestComsOrderDownstreamSubset()
    {
        const string tableName = "SELF_REF_TEST_TABLE";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_self_refs.toml");
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };
        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);
        var mockDbDataReader = MockBuilder.DbReader.ForQuery(DownstreamParams.ParseDownstreamUnionQuery);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var principalFk =
            config.ForeignKeys.First(fk => _params.TableUnderTest.IsPrimaryKeyOf(fk) && fk.IsPrincipal);
        AssertDownstreamQueriesCorrect(principalFk, regex, downstreamQueries);
    }


    [Fact]
    public void TestInvyTablesDownstreamSubset()
    {
        const string tableName = "TEST_TABLE";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_multi_pk_upstream_parents.toml");
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };
        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);
        var mockDbDataReader = MockBuilder.DbReader.ForQuery(DownstreamParams.ParseDownstreamUnionQuery);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var principalFk =
            config.ForeignKeys.First(fk => _params.TableUnderTest.IsPrimaryKeyOf(fk) && fk.IsPrincipal);
        AssertDownstreamQueriesCorrect(principalFk, regex, downstreamQueries);
    }

    [Fact]
    public void TestForeignKeyProcessorTablesDownstreamSubset()
    {
        const string tableName = "TEST";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_fkprocessor.toml");
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };

        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);
        Init(config, categoryToSchemaMappings, (_) => DbDataReaderBuilder
            .CreateInstance()
            .WithReader(DbDataReaderBuilder.CreateVinReader)
            .Object);

        _downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var principalFk = config.ForeignKeys.First(fk => _params.TableUnderTest.IsPrimaryKeyOf(fk));
        AssertDownstreamQueriesCorrect(principalFk, regex, downstreamQueries);
    }

    [Fact]
    public void TestSuffixedTablesDownstreamSubset()
    {
        const string tableName = "SUFFIXED_TEST_TABLE_0";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_suffixed_tables.toml");
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };

        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);

        var mockDbDataReader =
            MockBuilder.DbReader.ForQuery(DbDataReaderBuilder.ParseGenericDownstreamDestinationQueryFirstSubselect);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var principalFk =
            config.ForeignKeys.First(fk => _params.TableUnderTest.IsPrimaryKeyOf(fk) && fk.IsPrincipal);
        AssertDownstreamQueriesCorrect(principalFk, regex, downstreamQueries);
    }

    [Fact]
    public void TestTransactionCacheDownstreamSubset()
    {
        const string tableName = "MULTI_PK_TEST_TABLE";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "lh_test_schema_override.toml",
                "fk_test_schema_overrides.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };

        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);

        var mockDbDataReader =
            MockBuilder.DbReader.ForQuery(DbDataReaderBuilder.ParseGenericDownstreamDestinationQueryFirstSubselect);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();

        // MULTI_PK_TEST_TABLE should have no references to it, queries should be empty
        Assert.Empty(downstreamQueries);
    }

    [Fact]
    public void TestRenamedForeignKeyDownstreamSubset()
    {
        const string tableName = "TEST_TABLE";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_multi_pk_upstream_parents.toml");
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };

        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);

        var mockDbDataReader =
            MockBuilder.DbReader.ForQuery(DownstreamParams.ParseDownstreamUnionQuery);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var principalFk =
            config.ForeignKeys.First(fk => _params.TableUnderTest.IsPrimaryKeyOf(fk) && fk.IsPrincipal);
        AssertDownstreamQueriesCorrect(principalFk, regex, downstreamQueries);
    }

    [Fact]
    public void TestUsersDownstream()
    {
        const string tableName = "TEST_TABLE";
        var config = DownstreamParams.CreateDownstreamConfig("fks_test_multi_pk_upstream_parents.toml");
        var seedCacheOptions = new SeedCacheOptions
        {
            Stride = 2,
        };

        _params = DownstreamParams.InitialCreate(config, tableName, seedCacheOptions);
        var hostService =
            new HostsService(config.SourceHostConfigs.Concat(config.DestinationHostConfigs));
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(hostService);

        var mockDbDataReader =
            MockBuilder.DbReader.ForQuery(DownstreamParams.ParseDownstreamUnionQuery);
        var downstreamAction = _params.Init(config, categoryToSchemaMappings, mockDbDataReader);

        downstreamAction.DownstreamSubsetTable(_params.QueriesQueue);
        var downstreamQueries = _params.QueriesQueue.ToArray();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var principalFk =
            config.ForeignKeys.First(fk => _params.TableUnderTest.IsPrimaryKeyOf(fk) && fk.IsPrincipal);
        AssertDownstreamQueriesCorrect(principalFk, regex, downstreamQueries);
    }

    private void AssertDownstreamQueriesCorrect(ForeignKey principalFk, Regex regex,
        IEnumerable<(string, Dictionary<string, string>)> queries)
    {
        var principalKeySet = _params.PkSet;
        foreach (var (query, _) in queries)
        {
            // Assert query has shape expected (we can extract 3 capture groups)
            Assert.Matches(regex, query);
            var matches = regex.Match(query);
            var captures = matches.Groups.Values.Select(g => g.Value).Skip(1).ToArray();

            // Assert we query on primary key set of table
            var clauseParts = captures[2].Split(" IN ");
            var requestedColumns =
                ((principalFk.PrimaryKeyColumns.Length == 1 || clauseParts[0].Length == 1)
                    ? clauseParts[0]
                    : clauseParts[0].Substring(1, clauseParts[0].Length - 2)).Split(", ");
            var pkColumns = requestedColumns.Where(c => !c.StartsWith("column_")).OrderBy(s => s).ToArray();

            // Assert pk set of table maps to pk set of principal table
            var actualPrincipalMultiStringKey = new MultiStringKey(principalKeySet.Table, pkColumns);
            Assert.Contains(actualPrincipalMultiStringKey, principalKeySet.SubsidiaryMultiStringKeys);
        }
    }
}