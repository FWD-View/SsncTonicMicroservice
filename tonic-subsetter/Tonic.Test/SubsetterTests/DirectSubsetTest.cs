using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Tonic.Subsetter;
using Tonic.Subsetter.Actions;
using Tonic.Subsetter.Utils;
using Tonic.Test.Utils;
using Xunit;

namespace Tonic.Test;

public class DirectSubsetTest
{
    private Dictionary<Table, ImmutableArray<Column>> _columns;
    private PrimaryKeyCaches _primaryKeyCaches;
    private HashSet<PrimaryKeySet> _primaryKeySets;
    private List<Table> _tables;

    private void Init(ISubsetConfig config)
    {
        _tables = SubsetTestHelper.CreateTables(config);
        _columns = SubsetTestHelper.CreateTableColumns(_tables, config);
        _primaryKeySets = _tables.Select(t => new PrimaryKeySet(t, _columns[t], config.ForeignKeys)).ToHashSet();
        _primaryKeyCaches = new PrimaryKeyCaches(Utilities.RunId());
        SubsetTestHelper.SeedTableCaches(_primaryKeySets, _primaryKeyCaches);
    }

    [Fact]
    public void TestDirectQuery()
    {
        const string tableName = "TEST_UPSTREAM_TABLE";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "direct_targets_standard.toml",
                "fk_test_schema_overrides.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        Init(config);
        var tableUnderTest = _tables.Single(t => t.TableName == tableName);
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>();
        var directSubsetTarget = config.DirectTargets.Single();
        var directAction = new DirectAction(config, directSubsetTarget, _columns[tableUnderTest], 3);

        directAction.DirectSubsetTable(queriesQueue);
        var queries = queriesQueue.ToList();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var dt = config.DirectTargets.Single(t => t.Table.Equals(tableUnderTest));
        var ids = dt.IdKeys!.Select(i => i.ToString());
        AssertDirectQueriesCorrect(queries, regex, ids);
    }

    [Fact]
    public void TestDirectQuerySchemaOverride()
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

        Init(config);
        var tableUnderTest = _tables.Single(t => t.TableName == tableName);
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>();
        var directSubsetTarget = config.DirectTargets.Single();
        var directAction = new DirectAction(config, directSubsetTarget, _columns[tableUnderTest], 3);

        directAction.DirectSubsetTable(queriesQueue);

        foreach (var hostConfig in config.SourceHostConfigs)
        {
            var host = new Host(hostConfig.HostCategory, hostConfig,
                ImmutableArray<SchemaOverride>.Empty);
            var schemaOverride =
                config.SchemaOverrides.FirstOrDefault(o => host.Configuration.Host == o.Host);
            foreach (var (query, _) in queriesQueue)
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

        var queries = queriesQueue.ToList();
        var regex = new Regex($"^SELECT (.*) FROM (.*) WHERE (.*)$");
        var dt = config.DirectTargets.Single(t => t.Table.Equals(tableUnderTest));
        var ids = dt.IdKeys!.Select(i => i.ToString());
        AssertDirectQueriesCorrect(queries, regex, ids);
    }

    private static void AssertDirectQueriesCorrect(List<(string, Dictionary<string, string>)> queries, Regex regex, IEnumerable<string> ids)
    {
        var foundIds = new HashSet<string>();
        foreach (var (query, paramsDict) in queries)
        {
            Assert.Matches(regex, query);
            foundIds.UnionWith(paramsDict.Values);
        }
        Assert.Equal(ids, foundIds);
    }
}