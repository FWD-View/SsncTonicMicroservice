#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Tonic.Common.Exceptions;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Subsetter;
using Tonic.Subsetter.Actions;
using Tonic.Subsetter.Utils;
using Tonic.Test.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Tonic.Test;

public class SubsetterTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private Dictionary<Table, ImmutableArray<Column>> _columns = null!;
    private List<Table> _tables = null!;

    public SubsetterTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private void Init(ISubsetConfig config)
    {
        _tables = SubsetTestHelper.CreateTables(config);
        _columns = SubsetTestHelper.CreateTableColumns(_tables, config);
    }

    [Fact]
    public void TestWriteSubsetInfo()
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles("fks_test_multi_pk_upstreams.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var orderedTableActions = config.ForeignKeys
            .SelectMany(fk => new[]
            {
                new Table(fk.ForeignKeyHostCategory, fk.ForeignKeyTable),
                new Table(fk.PrimaryKeyHostCategory, fk.PrimaryKeyTable)
            })
            .Distinct()
            .Select(t => new SubsetAction(t, SubsetActionType.Direct));
        Subsetter.Subsetter.WriteSubsetInformationToFile(orderedTableActions, config.ForeignKeys,
            OracleUtilities.SharedDirectory);
        var fileName = Path.Combine(OracleUtilities.SharedDirectory, "table_subset_information.txt");
        Assert.True(File.Exists(fileName));
        var fileContents = File.ReadAllText(fileName);
        File.Delete(fileName);
        Assert.False(string.IsNullOrEmpty(fileContents));
    }

    [Fact]
    public void TestCategorySchemaMappings()
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var hostManager = new HostsService(config.DestinationHostConfigs);
        var mappings = DownstreamAction.CreateCategoryToSchemaMappings(hostManager);
        Assert.Equal(4, mappings.Count);
    }

    [Fact]
    public void TestMultiTableSubsetTraversal()
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "direct_targets_upstreams.toml",
                "fks_test_empty_pk_key_set.toml"
            )
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        var directTargets = config.DirectTargets.Select(dst => dst.Table).ToHashSet();
        var upstreamStarts = config.AdditionalUpstreamStarts.ToHashSet();
        List<SubsetAction> traversal;
        try
        {
            traversal = new SubsetTraversal(config.ForeignKeys, directTargets, upstreamStarts)
                .GetTraversalOrder().ToList();
        }
        catch (CycleFoundException ex)
        {
            if (ex.Cycle == null)
                throw;

            var edges = string.Join("\n", ex.Cycle.Select(tableRel => tableRel.ToString()));
            _testOutputHelper.WriteLine(edges);
            throw;
        }

        Assert.NotEmpty(traversal);
    }

    [Fact]
    public void TestComsTransactionLookupKeySetCreation()
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles("fks_test_self_refs.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        Init(config);

        var tableUnderTest = _tables.Single(t => t.TableName == "SELF_REF_TEST_TABLE");
        var actualPrimaryKeySet = new PrimaryKeySet(tableUnderTest, _columns[tableUnderTest], config.ForeignKeys);

        var expectedReferences = config.ForeignKeys
            .Where(fk =>
                fk.PrimaryKeyHostCategory == tableUnderTest.HostCategory &&
                fk.PrimaryKeyTable == tableUnderTest.TableName)
            .ToImmutableHashSet();

        Assert.Equal(actualPrimaryKeySet.SubsidiaryForeignKeys, expectedReferences);
    }

    [Fact]
    public void TestTransactionCachePrimaryKeySetCreation()
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
        Init(config);
        var tableUnderTest = _tables.Single(t => t.TableName == tableName);
        var actualPrimaryKeySet = new PrimaryKeySet(tableUnderTest, _columns[tableUnderTest], config.ForeignKeys);

        var actualColumns = actualPrimaryKeySet.PrincipalForeignKeys.SelectMany(fk => fk.ForeignKeyColumns)
            .OrderBy(c => c);
        var actualPrincipalMultiStringKey = new MultiStringKey(tableUnderTest, actualColumns);
        var expectedSubsidiaryMultiStringKey =
            new MultiStringKey(tableUnderTest, new[] { "ID_A", "ID_B", "ID_C" });
        Assert.Equal(expectedSubsidiaryMultiStringKey, actualPrincipalMultiStringKey);
    }

    [Fact]
    public void TestPrimaryKeyCacheContainsKeys()
    {
        const string tableName = "TEST_TABLE";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "direct_targets_standard.toml",
                "fks_test_empty_pk_key_set.toml"
            )
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        Init(config);
        var tableUnderTest = new Table("OtherHost", tableName);
        var primaryKeySet = new PrimaryKeySet(tableUnderTest, _columns[tableUnderTest], config.ForeignKeys);

        var expectedKeys = new HashSet<MultiStringKey>
        {
            new(tableUnderTest, new[] { "A", "B" }),
            new(tableUnderTest, new[] { "B" }),
            new(tableUnderTest, new[] { "B", "C" }),
        }.ToImmutableHashSet();
        Assert.Equal(expectedKeys, primaryKeySet.SubsidiaryMultiStringKeys);
    }

    [Fact]
    public void TestAllPrimaryKeyColumns()
    {
        const string tableName = "MULTI_PK_TEST_TABLE";
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(
                "lh_test_schema_override.toml",
                "fks_test_multi_pk_upstreams.toml")
            .ToConcatenatedStream();
        var config = configFileStream.ParseSubsetConfig();
        Init(config);

        foreach (var table in _tables.Where(t => t.TableName == tableName))
        {
            var originalPrimaryKeys = SubsetTestHelper.ComputePrimaryKeyColumns(config.ForeignKeys, table);
            var pkSet = new PrimaryKeySet(table, _columns[table], config.ForeignKeys);
            if (pkSet.IsMultiTablePrincipal)
            {
                Assert.True(new Table("OtherHost", "MULTI_PK_TEST_TABLE").Equals(table));
                continue;
            }

            if (!originalPrimaryKeys.Any() && !pkSet.SubsidiaryMultiStringKeys.Any())
            {
                continue;
            }

            if (!originalPrimaryKeys.Any() && pkSet.SubsidiaryMultiStringKeys.Any())
            {
                throw new InvalidDataException($"Differing key sets for {table}");
            }

            var q = pkSet.SubsidiaryMultiStringKeys.ToImmutableArray();
            if (!q.Any())
            {
                throw new InvalidDataException($"Expected key set for {table}");
            }

            var newPrimaryKeys = q.First().From(2).Parts;
            if (!pkSet.SubsidiaryMultiStringKeys.Any())
            {
                if (!originalPrimaryKeys.Any()) continue;
                _testOutputHelper.WriteLine($"{originalPrimaryKeys}");
                throw new InvalidDataException($"Expected key set for {table}");
            }

            if (pkSet.SubsidiaryMultiStringKeys.Count == 1)
            {
                Assert.Equal(originalPrimaryKeys.ToList(), newPrimaryKeys.ToList());
            }
            else
            {
                // assert differing key sets are permutations of original
            }
        }
    }
}