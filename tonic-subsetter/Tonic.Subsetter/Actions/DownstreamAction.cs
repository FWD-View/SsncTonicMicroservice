using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Tonic.Common;
using Tonic.Common.CommonAbstraction;
using Tonic.Common.Enums;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Tonic.Subsetter.Utils;

namespace Tonic.Subsetter.Actions;

// Fields & Constructors
public partial record DownstreamAction(ISubsetConfig Config, PrimaryKeySet PrimaryKeySet,
    IPrimaryKeyCaches KeyCaches, int _queryBatchSize, IHostsService DestConnections,
    Dictionary<string, string> CategoryToDestSchemaMappings)
{
    private readonly int _queryBatchSize = _queryBatchSize;
    private ISubsetConfig Config { get; } = Config;
    private IPrimaryKeyCaches KeyCaches { get; } = KeyCaches;
    private PrimaryKeySet PrimaryKeySet { get; } = PrimaryKeySet;
    private Dictionary<string, string> CategoryToDestSchemaMappings { get; } = CategoryToDestSchemaMappings;
    private IHostsService DestConnections { get; } = DestConnections;

    public static Dictionary<string, string> CreateCategoryToSchemaMappings(IHostsService destConnections)
    {
        var mappings = new Dictionary<string, string>();
        foreach (var (category, host) in destConnections)
        {
            mappings.TryAdd(category, host.Configuration.Schema);
        }

        return mappings;
    }
}

// Subsetting Methods
public partial record DownstreamAction
{
    public Task DownstreamSubsetTable(BlockingCollection<(string, Dictionary<string, string>)> queriesQueue)
    {
        if (!PrimaryKeySet.SubsidiaryMultiStringKeys.Any())
        {
            Log.Warning(
                "Could not find any subsidiary key sets for table {Table}, skipping downstream subsetting",
                PrimaryKeySet.Table);
            queriesQueue.CompleteAdding();
            return Task.CompletedTask;
        }

        var selectsByHost = CreateHostSelects(PrimaryKeySet, CategoryToDestSchemaMappings);
        var tasks = CreateFilteredDownstreamBatchQuery(selectsByHost, queriesQueue);

        return Task.WhenAll(tasks.ToArray()).ContinueWith(t =>
        {
            queriesQueue.CompleteAdding();
            if (t.IsFaulted) throw t.Exception!;
        });
    }

    private List<Task> CreateFilteredDownstreamBatchQuery(
        Dictionary<MultiStringKey, Dictionary<string, (IForeignKeyProcessor, string[])>> selectsByHost,
        BlockingCollection<(string, Dictionary<string, string>)> queriesQueue)
    {
        var tasks = new List<Task>();
        var table = PrimaryKeySet.Table;
        foreach (var (keySet, hostLookup) in selectsByHost)
        {
            foreach (var (hostCategory, (processor, selectStatements)) in hostLookup)
            {
                var hasForeignKeyProcessor = processor != DefaultForeignKeyProcessor.Instance;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (hasForeignKeyProcessor)
                {
                    Log.Information(
                        "ForeignKeyProcessor detected, building downstream queries for {Table} using destination queries",
                        table);
                }
                else
                {
                    Log.Information("Building downstream queries for {Table} using primary key caches", table);
                }

                var hostTasks = DestConnections.RunOnCategory(table.HostCategory,
                    host =>
                    {
                        var pkCols = keySet.From(2).Parts;
                        string selectStatement;
                        if (!Config.Debug || !Config.NoDownstreamUnions)
                        {
                            var subSelectQuery = string.Join(" UNION ", selectStatements);
                            // if no fk processor, query for missing rows directly
                            if (!hasForeignKeyProcessor)
                            {
                                var selectFromTemp =
                                    CreatePrimaryKeyCacheSelects(table, pkCols, subSelectQuery,
                                        Config.SchemaOverrides);
                                selectStatement = selectFromTemp;
                            }
                            // if fk processor, query for missing rows, use processor, filter on found keys from batch
                            else
                            {
                                selectStatement = subSelectQuery;
                            }

                            var fkValues = DownstreamPopulateKeyQueries(host, selectStatement, processor);
                            if (fkValues.Any())
                            {
                                Log.Logger.Debug("Found {Count} missing downstream values for {Key}",
                                    fkValues.Count, keySet);
                            }

                            for (var i = 0; i < fkValues.Count; i += _queryBatchSize)
                            {
                                var batch = fkValues.Skip(i).Take(_queryBatchSize).ToList();
                                queriesQueue.Add(DownstreamBatchQuery(keySet, batch));
                            }

                            return;
                        }

                        // Don't do UNIONS for debug to help understand where data is coming from
                        var previouslySeenFkValues = new HashSet<MultiStringKey>();
                        foreach (var subSelectQuery in selectStatements)
                        {
                            if (!hasForeignKeyProcessor)
                            {
                                var selectFromTemp =
                                    CreatePrimaryKeyCacheSelects(table, pkCols, subSelectQuery,
                                        Config.SchemaOverrides);
                                selectStatement = selectFromTemp;
                            }
                            else
                            {
                                selectStatement = subSelectQuery;
                            }

                            var fkValues =
                                DownstreamPopulateKeyQueries(host, selectStatement, processor);
                            var initialCount = fkValues.Count;
                            var queryTextWithHost = selectStatement.Replace(
                                Constants.TonicSchemaToken, host.Configuration.Schema);

                            fkValues = fkValues
                                .Where(fkVal =>
                                    !previouslySeenFkValues.Contains(new MultiStringKey(fkVal)))
                                .ToList();
                            previouslySeenFkValues.UnionWith(
                                fkValues.Select(values => new MultiStringKey(values)));

                            if (fkValues.Count != initialCount)
                            {
                                Log.Debug(
                                    "Found {NumUniqueKeys} unique keys out of {NumTotalKeys} keys for downstream subsetting of table {TableName} via query: {Query}",
                                    fkValues.Count, initialCount, table, queryTextWithHost);
                            }

                            for (var i = 0; i < fkValues.Count; i += _queryBatchSize)
                            {
                                var batch = fkValues.Skip(i).Take(_queryBatchSize).ToList();
                                queriesQueue.Add(DownstreamBatchQuery(keySet, batch));
                            }
                        }
                    });
                if (hostTasks.Length != 1)
                    throw new InvalidOperationException(
                        $"There can't be more than one destination per category, see category {hostCategory}");
                tasks.Add(hostTasks[0]);
            }
        }

        return tasks;
    }

    private (string, Dictionary<string, string> paramsDict) DownstreamBatchQuery(MultiStringKey keySet,
        IList<string[]> batch)
    {
        var columnsSelectStr = DBAbstractionLayer.ColumnsSelectStr(PrimaryKeySet.Columns, (int)DatabaseType.Oracle);

        if (KeyCaches.HasCache(keySet))
        {
            var originalBatchCount = batch.Count;
            var alreadyExistingRows = KeyCaches.GetCache(keySet).ContainsKeys(batch, keySet);
            batch = batch.Where((_, i) => !alreadyExistingRows[i]).ToImmutableArray();
            Log.Logger.Debug("Filtered out {C} rows of batch for {KeySet}", originalBatchCount - batch.Count,
                keySet);
        }

        var (keyClause, paramsDict) = Utilities.KeyClause(keySet, batch);
        var table = PrimaryKeySet.Table;
        var schemaToken = SchemaOverride.ShouldOverrideTable(table, Config.SchemaOverrides)
            ? SchemaOverride.CreateOverrideToken(table.TableName)
            : Constants.TonicSchemaToken;
        return (
            $"SELECT {columnsSelectStr} FROM {schemaToken}.{PrimaryKeySet.Table.TableName} WHERE {keyClause}",
            paramsDict);
    }

    private static List<string[]> DownstreamPopulateKeyQueries(IHost host, string query,
        IForeignKeyProcessor processor)
    {
        using var reader = host.ExecuteQuery(query);
        var fkValues = new List<string[]>();

        while (reader.Read())
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; ++i)
                values[i] = reader.GetValue(i).ToString()!;

            var foreignKeyValues = processor.ExtractKeys(values)
                .Where(foreignKeyValues => !foreignKeyValues.Any(string.IsNullOrEmpty)).ToList();
            fkValues.AddRange(foreignKeyValues);
        }

        return fkValues;
    }
}

// Auxiliary functions
public partial record DownstreamAction
{
    private static string CreatePrimaryKeyCacheSelects(Table table, ImmutableArray<string> pkCols,
        string subSelect, ImmutableArray<SchemaOverride> schemaOverrides)
    {
        var prefixedColumns =
            pkCols.Select<string, (string Left, string Right)>(c => ($"l.{c}", $"r.{c}")).ToList();

        var (table0, _) = Utilities.SendTo0TableHack(table);
        var hasSchemaOverride = SchemaOverride.ShouldOverrideTable(table, schemaOverrides);
        var schemaToken = hasSchemaOverride
            ? SchemaOverride.CreateOverrideToken(table.TableName)
            : Constants.TonicSchemaToken;
        var parentSelect =
            $"SELECT {string.Join(", ", pkCols)}, 1 AS null_check FROM {schemaToken}.{table0}";
        var targetColumnClause =
            string.Join(" AND ", prefixedColumns.Select((tuple) => $"{tuple.Left} = {tuple.Right}"));
        var primaryKeyNullCheck = string.Join(" AND ",
            prefixedColumns.Select(tpl => $"{tpl.Left} IS NOT NULL"));
        var lPrefixedColumnsString = string.Join(", ", prefixedColumns.Select(tuple => tuple.Left));

        var createTableSelect = @$"SELECT {lPrefixedColumnsString} FROM (
    {subSelect}
) l 
LEFT JOIN (
    {parentSelect}
) r ON {targetColumnClause} 
WHERE r.null_check IS NULL AND {primaryKeyNullCheck}
";
        return createTableSelect;
    }

    public static Dictionary<MultiStringKey, Dictionary<string, (IForeignKeyProcessor, string[])>>
        CreateHostSelects(PrimaryKeySet primaryKeySet, Dictionary<string, string> categoryToSchemaMappings)
    {
        var selectLookup = new Dictionary<MultiStringKey, Dictionary<string, (IForeignKeyProcessor, string[])>>();
        foreach (var multiStringKey in primaryKeySet.SubsidiaryMultiStringKeys)
        {
            var primaryColumns = multiStringKey.From(2).Parts;
            var tableGroups = primaryKeySet.SubsidiaryForeignKeys
                .Where(fk => primaryColumns.SequenceEqual(fk.PrimaryKeyColumns))
                .GroupBy(fk => fk.ForeignKeyHostCategory, (lh, fks) => (lh, fks.ToList())).ToList();

            if (!tableGroups.Any())
            {
                Log.Logger.Warning("Could not find any relevant table groups for key {Key}, will skip downstream",
                    multiStringKey);
            }

            foreach (var (lhc, fks) in tableGroups)
            {
                var hostSelects =
                    CreateSelectsForHost(fks, categoryToSchemaMappings);
                if (!selectLookup.ContainsKey(multiStringKey))
                    selectLookup[multiStringKey] = new Dictionary<string, (IForeignKeyProcessor, string[])>();
                selectLookup[multiStringKey][lhc] = (fks.First().ForeignKeyProcessor, hostSelects);
            }
        }

        return selectLookup;
    }

    private static string[] CreateSelectsForHost(IEnumerable<ForeignKey> foreignKeys,
        IReadOnlyDictionary<string, string> categoryToSchemaMappings) =>
        foreignKeys.Select(fk =>
        {
            var columnsRenamed = fk.ForeignKeyColumns.Zip(fk.PrimaryKeyColumns)
                .OrderBy(cr => cr.Second)
                .Select(cr =>
                {
                    var (first, second) = cr;
                    var fkExpression = fk.ForeignKeyColumnCastTypes.ContainsKey(first)
                        ? $"CAST({first} AS {fk.ForeignKeyColumnCastTypes[first]})"
                        : first;
                    return $"{fkExpression} AS {second}";
                });
            var columnSelect = string.Join(", ", columnsRenamed);
            var table = new Table(fk.ForeignKeyHostCategory, fk.ForeignKeyTable, fk.Grouped);
            var (table0, _) = Utilities.SendTo0TableHack(table);
            return
                $"SELECT {columnSelect} FROM {categoryToSchemaMappings[table.HostCategory]}.{table0}";
        }).ToArray();
}