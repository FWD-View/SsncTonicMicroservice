using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Tonic.Common.CommonAbstraction;
using Tonic.Common.Exceptions;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Tonic.Subsetter.Actions;
using Tonic.Subsetter.Utils;

namespace Tonic.Subsetter;

public class Subsetter
{
    private int QueryBatchSize { get; }
    private int QueryQueueSize { get; } = 100;
    private ISubsetConfig Config { get; }
    private IHostsService SourceConnections { get; }
    private IHostsService DestConnections { get; }
    public string RunId { get; }
    public IPrimaryKeyCaches KeyCaches { get; }
    private readonly Dictionary<Table, PrimaryKeySet> _primaryKeySets = new();

    public Subsetter(ISubsetConfig config, IHostsService sourceConnections,
        IHostsService destinationConnections)
    {
        Config = config;
        SourceConnections = sourceConnections;
        DestConnections = destinationConnections;
        RunId = Utilities.RunId();
        KeyCaches = new PrimaryKeyCaches(RunId);
        QueryBatchSize = config.QueryBatchSize;
        KeyCaches = new PrimaryKeyCaches(RunId);
    }

    public void Subset()
    {
        var orderedTableActions = GetSubsetActions();

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var columns = DbSchemaUtilities.CollectSourceSchemaInformation(SourceConnections);
        var iotTables =  DbSchemaUtilities.CollectIoTTables(SourceConnections);
        AssertColumnsHasAllTables(columns, orderedTableActions.Select(action => action.Table));


        var isDebug = Log.IsEnabled(LogEventLevel.Debug);
        foreach (var subsetAction in orderedTableActions)
        {
            if (_primaryKeySets.ContainsKey(subsetAction.Table)) continue;
            var tableColumns = columns[subsetAction.Table];
            _primaryKeySets[subsetAction.Table] =
                new PrimaryKeySet(subsetAction.Table, tableColumns, Config.ForeignKeys);
            if (isDebug)
            {
                Log.Logger.Debug("Created primaryKeySet for {Table}: {Set}", subsetAction.Table,
                    _primaryKeySets[subsetAction.Table]);
            }
        }

        if (isDebug)
        {
            foreach (var (hostCategory, _) in SourceConnections)
            {
                var sourceConnections = SourceConnections[hostCategory].Select(lh => lh.Configuration.Host);
                var sourceConnectionsString = string.Join("\n\t", sourceConnections);
                Log.Debug("Using hosts for {Category} category:\n {Hosts}", hostCategory,
                    sourceConnectionsString);
            }
        }

        if (Config.TableSchemaRestrictions.Any())
        {
            var restrictionsString = Config.TableSchemaRestrictions
                .Select(kvp => $"\t{kvp.Key}:{kvp.Value}")
                .Aggregate((acc, current) => string.Join("\n", acc, current));
            Log.Information("Detected table schema restrictions:\n{Restrictions}", restrictionsString);
        }

        stopWatch.Reset();
        stopWatch.Start();

        var numTableActions = orderedTableActions.Count;
        var currentTableAction = 0;
        var copyRowsHelper = new CopyRowsService(RunId, Config, SourceConnections, DestConnections, KeyCaches);
        foreach (var subsetAction in orderedTableActions)
        {
            var subsetActionTypeStr = subsetAction.ActionType switch
            {
                SubsetActionType.Direct => "Direct",
                SubsetActionType.Upstream => "Upstream",
                SubsetActionType.Downstream => "Downstream",
                _ => throw new ArgumentOutOfRangeException($"SubsetAction not recognized: {subsetAction}")
            };
            Log.Information("Subsetting Table: {Table} Action: {Action} ({Count}/{TotalCount})", subsetAction.Table,
                subsetActionTypeStr, currentTableAction, numTableActions);
            var isIotTable = iotTables.Contains(subsetAction.Table);

            switch (subsetAction.ActionType)
            {
                case SubsetActionType.Direct:
                    PerformDirectSubsetting(_primaryKeySets[subsetAction.Table], copyRowsHelper, isIotTable);
                    break;
                case SubsetActionType.Upstream:
                    PerformUpstreamSubsetting(_primaryKeySets[subsetAction.Table], copyRowsHelper, isIotTable);
                    break;
                case SubsetActionType.Downstream:
                    PerformDownstreamSubsetting(_primaryKeySets[subsetAction.Table], copyRowsHelper, isIotTable);
                    break;
                default:
                    throw new ArgumentException(
                        $"Table {subsetAction.Table} has invalid action for subsetting (must be direct, upstream, or downstream): {subsetAction.ActionType}");
            }

            currentTableAction += 1;
        }

        Log.Information("Spent total of {TotalSeconds}s in subsetting", stopWatch.Elapsed.TotalSeconds);
    }

    private List<SubsetAction> GetSubsetActions()
    {
        // the first table is the furthest upstream one
        List<SubsetAction> orderedTableActions;
        try
        {
            orderedTableActions =
                new SubsetTraversal(Config.ForeignKeys, Config.DirectTargets.Select(dst => dst.Table).ToHashSet(),
                        Config.AdditionalUpstreamStarts.ToHashSet())
                    .GetTraversalOrder().ToList();
        }
        catch (CycleFoundException ex)
        {
            Log.Fatal("Found a cycle between tables! Cannot proceed");
            if (ex.Cycle == null)
                throw;

            var edges = string.Join("\n", ex.Cycle.Select(tableRel => tableRel.ToString()));
            Log.Fatal("Cycle:\n{Edges}", edges);
            throw;
        }

        Log.Information("Subsetting Tables in Order:\n{Tables}",
            string.Join("\n", orderedTableActions.Select((action, i) => $"\t{i}: {action}")));
        if (!Config.DetailedSubsetInformation) return orderedTableActions;
        var tempFilePath = Path.Combine(DBAbstractionLayer.SharedDirectory, $"tonic_subsetter_{RunId}");
        WriteSubsetInformationToFile(orderedTableActions, Config.ForeignKeys, tempFilePath);
        return orderedTableActions;
    }

    public static void WriteSubsetInformationToFile(IEnumerable<SubsetAction> orderedTableActions,
        IList<ForeignKey> foreignKeys, string filePath)
    {
        var baseFileName = Path.Combine(filePath, "table_subset_information.txt");
        using var writer = Utilities.OpenTsvFile(baseFileName, "table subsetting information");
        var tables = orderedTableActions.Select(ta => ta.Table).Distinct();
        foreach (var table in tables)
        {
            var upstreamTables = string.Join("\n\t\t", foreignKeys.Where(table.IsForeignKeyOf).Select(fk => fk.ToString()));
            var downstreamTables = string.Join("\n\t\t", foreignKeys.Where(table.IsPrimaryKeyOf).Select(fk => fk.ToString()));
            var hasUpstreams = !string.IsNullOrEmpty(upstreamTables);
            var hasDownstreams = !string.IsNullOrEmpty(downstreamTables);
            if (hasUpstreams || hasDownstreams)
                writer.WriteLine($"{table}: ");
            if (hasUpstreams)
                writer.WriteLine($"\tUpstream:\n\t\t{upstreamTables}");
            if (hasDownstreams)
                writer.WriteLine($"\tDownstream:\n\t\t{downstreamTables}");
        }
    }

    private void PerformDirectSubsetting(PrimaryKeySet primaryKeySet, CopyRowsService copyRowsService,
        bool isIotTable)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var directTarget = Config.DirectTargets.Single(dt => dt.Table.Equals(primaryKeySet.Table));
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>(QueryQueueSize);
        var directAction = new DirectAction(Config, directTarget, primaryKeySet.Columns, QueryBatchSize);
        directAction.DirectSubsetTable(queriesQueue);
        queriesQueue.CompleteAdding();
        var tasks = copyRowsService.CopyTableWithQueries(primaryKeySet, queriesQueue, isIotTable);
        Task.WaitAll(tasks);
        Log.Information("Finished direct table {Table} with Queries in {TotalSeconds}", primaryKeySet.Table,
            stopWatch.Elapsed.TotalSeconds);
    }

    private void PerformUpstreamSubsetting(PrimaryKeySet primaryKeySet, CopyRowsService copyRowsService,
        bool isIotTable)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>(QueryQueueSize);
        var upstreamAction = new UpstreamAction(Config, KeyCaches, primaryKeySet);
        var queryTask = primaryKeySet.IsMultiTablePrincipal
            ? upstreamAction.GenerateMultiPrincipalUpstreamQueries(queriesQueue)
            : upstreamAction.GeneratePrincipalUpstreamQueries(queriesQueue);
        var copyTask = copyRowsService.CopyTableWithQueries(primaryKeySet, queriesQueue, isIotTable, true);
        Task.WaitAll(copyTask.Append(queryTask).ToArray());
        Log.Information("Finished upstream table {Table} with Queries in {TotalSeconds}", primaryKeySet.Table,
            stopWatch.Elapsed.TotalSeconds);
    }

    private void PerformDownstreamSubsetting(PrimaryKeySet primaryKeySet, CopyRowsService copyRowsService,
        bool isIotTable)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var queriesQueue = new BlockingCollection<(string, Dictionary<string, string>)>(QueryQueueSize);
        var categoryToSchemaMappings = DownstreamAction.CreateCategoryToSchemaMappings(DestConnections);
        var downstreamAction =
            new DownstreamAction(Config, primaryKeySet, KeyCaches, QueryBatchSize, DestConnections,
                categoryToSchemaMappings);
        var queryTask = downstreamAction.DownstreamSubsetTable(queriesQueue);
        var copyTask = copyRowsService.CopyTableWithQueries(primaryKeySet, queriesQueue, isIotTable);
        Task.WaitAll(copyTask.Append(queryTask).ToArray());
        Log.Information("Finished downstream table {Table} with Queries in {TotalSeconds}", primaryKeySet.Table,
            stopWatch.Elapsed.TotalSeconds);
    }

    private static void AssertColumnsHasAllTables(ImmutableDictionary<Table, ImmutableArray<Column>> columns,
        IEnumerable<Table> tables)
    {
        var missingTables = tables.Where(t => !columns.ContainsKey(t)).ToImmutableArray();
        if (!missingTables.Any()) return;
        var tablesStr = string.Join("\n\t", missingTables);
        throw new InvalidOperationException($"Missing schema info for tables:\n\t{tablesStr}");
    }
}