#nullable  enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Tonic.Subsetter;
using Tonic.Subsetter.Actions;
using Tonic.Subsetter.Utils;
using Tonic.Test.Utils;

namespace Tonic.Test;

public record DownstreamParams
{
    public readonly string RunId = Utilities.RunId();
    public Table TableUnderTest = null!;
    public HashSet<PrimaryKeySet> PrimaryKeySets = null!;
    public PrimaryKeyCaches PrimaryKeyCaches = null!;
    public BlockingCollection<(string, Dictionary<string, string>)> QueriesQueue = new();
    public PrimaryKeySet PkSet = null!;

    public static DownstreamParams InitialCreate(ISubsetConfig config, SeedCacheOptions? cacheOptions = null)
    {
        var tables = SubsetTestHelper.CreateTables(config);
        var columns = SubsetTestHelper.CreateTableColumns(tables, config);
        var primaryKeySets = tables.Select(t => new PrimaryKeySet(t, columns[t], config.ForeignKeys)).ToHashSet();
        var primaryKeyCaches = new PrimaryKeyCaches(Utilities.RunId());
        var downstreamParams = new DownstreamParams
        {
            PrimaryKeySets = primaryKeySets,
            PrimaryKeyCaches = primaryKeyCaches,
        };
        return downstreamParams;
    }
    public static DownstreamParams InitialCreate(ISubsetConfig config, string tableName, SeedCacheOptions? cacheOptions = null)
    {
        var seedCacheOptions = cacheOptions ?? new SeedCacheOptions();
        var tables = SubsetTestHelper.CreateTables(config);
        var columns = SubsetTestHelper.CreateTableColumns(tables, config);
        var primaryKeySets = tables.Select(t => new PrimaryKeySet(t, columns[t], config.ForeignKeys)).ToHashSet();
        var primaryKeyCaches = new PrimaryKeyCaches(Utilities.RunId());
        SubsetTestHelper.SeedTableCaches(primaryKeySets, primaryKeyCaches, seedCacheOptions);
        var downstreamParams = new DownstreamParams
        {
            PrimaryKeySets = primaryKeySets,
            PrimaryKeyCaches = primaryKeyCaches,
            QueriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>(),
            TableUnderTest = tables.First(t => t.TableName == tableName),
            PkSet = primaryKeySets.Single(ks => ks.Table.TableName == tableName),
        };
        return downstreamParams;
    }
    
    public DownstreamAction Init(ISubsetConfig config, Dictionary<string, string> categoryToSchemaMappings,
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

        var hostsService = MockBuilder.HostManager(mockHosts)
            .WithRunOnCategory()
            .WithHostsEnumerator().Object;
        var downstreamAction = new DownstreamAction(
            config,
            PkSet,
            PrimaryKeyCaches,
            config.QueryBatchSize,
            hostsService,
            categoryToSchemaMappings
        );
        return downstreamAction;
    }


    public static ISubsetConfig CreateDownstreamConfig(string fileName)
    {
        var configFileStream = ConfigFileBuilder
            .WithSubsetterOptions()
            .WithTonicHost()
            .WithFiles(fileName)
            .ToConcatenatedStream();
        return configFileStream.ParseSubsetConfig();
    }

    public static string ParseDownstreamUnionQuery(string query)
    {
        /*language=text*/
        const string joinedSelect = @"SELECT (.*) FROM \(
(.*)
\) l 
LEFT JOIN \(
(.*)
\) r ON (.*)
WHERE (.*) AND (.*)";
        var regex = new Regex(joinedSelect, RegexOptions.Singleline);
        if (!regex.IsMatch(query)) return null;
        var matches = regex.Match(query);
        var subSelects = matches.Groups.Values.Select(g => g.Value).Skip(2).First().Split(" UNION ").First().Trim();
        /*language=text*/
        var subSelectRegex = new Regex("SELECT ((:?|.* AS )(.*)) FROM .*", RegexOptions.Singleline);
        var subSelectMatches = subSelectRegex.Match(subSelects).Groups.Values.Select(g => g.Value).ToArray();
        var subSelect = subSelectMatches.Skip(1).First().Trim();
        var nextColumns = subSelect.Split(", ").ToDictionary(s => s, s => s.Split(" AS ")[1]);

        var newSubQuery = nextColumns.Aggregate(subSelectMatches[0],
            (current, value) => current.Replace(value.Key, value.Value));
        return newSubQuery;
    }
}