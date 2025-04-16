using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using Serilog.Events;
using Tonic.Common;
using Tonic.Common.AWSHelper;
using Tonic.Common.CommonAbstraction;
using Tonic.Common.Configs;
using Tonic.Common.DB2Helper;
using Tonic.Common.Enums;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Common.OracleHelper.Models;
using Tonic.Common.Utils;

namespace Tonic.Subsetter.Utils;

public record CopyRowsService(string RunId, ISubsetConfig Config, IHostsService SourceConnections,
    IHostsService DestConnections, IPrimaryKeyCaches KeyCaches)
{
    private string RunId { get; } = RunId;
    private ISubsetConfig Config { get; } = Config;

    private IHostsService DestConnections { get; } = DestConnections;
    private IPrimaryKeyCaches KeyCaches { get; } = KeyCaches;
    private CancellationTokenSource CancellationTokenSource { get; } = new();

    public Task[] CopyTableWithQueries(PrimaryKeySet primaryKeySet,
        BlockingCollection<(string, Dictionary<string, string>)> queriesQueue,
        bool isIotTable, bool filterOnSecondaries = false)
    {
        var table = primaryKeySet.Table;
        Log.Information("Copying table: {Table} using queries", table);
        var columns = primaryKeySet.Columns;
        var pkCacheQueue = new BlockingCollection<string[]>();
        var rowWriteQueue = new BlockingCollection<string[]>();
        var tempFilePath = Path.Combine(DBAbstractionLayer.SharedDirectory, $"tonic_subsetter_{RunId}");

        // var (destinationTableName, destinationColumns) = Utilities.SendTo0TableHack(table, columns);

        var readTasks = SourceConnections.RunOnCategoryWithMultiplexedQueue(table.HostCategory,
            queriesQueue,
            (host, queryQueue) =>
            {
                var currentSchema = host.Configuration.Schema;
                var schemaRestricted = IsSchemaRestrictedTable(table, currentSchema);
                if (schemaRestricted)
                {
                    return;
                }

                GetRows(filterOnSecondaries, queryQueue, host, rowWriteQueue,
                    pkCacheQueue,
                    table, columns);
            });

        var readCompleteTask = Task.WhenAll(readTasks).ContinueWith(t =>
        {
            rowWriteQueue.CompleteAdding();
            pkCacheQueue.CompleteAdding();
            if (t.IsFaulted) throw t.Exception!;
            if (!Log.IsEnabled(LogEventLevel.Debug)) return;
            Log.Logger.Debug("CopyRowService finished all reads for {Table}", table);
        });

        var cacheTask = BuildKeySetCaches(primaryKeySet, pkCacheQueue);

        Directory.CreateDirectory(tempFilePath);
        Utilities.MakeFilePublicOnUnix(tempFilePath);
        Log.Information("File path {TempFilePath}", tempFilePath);
        var csvQueue = new BlockingCollection<string>();
        // var writeTask = Utilities.TaskForQueue(csvQueue, () => WriteRowsToCsv(table, tempFilePath, rowWriteQueue, csvQueue));
        var isDb2 = SourceConnections.IsDB2(table.HostCategory);
        var writeTask = isDb2 ? Utilities.TaskForQueue(csvQueue, () => WriteRowsToCsv(table, tempFilePath, rowWriteQueue, csvQueue, "DB2")) : Utilities.TaskForQueue(csvQueue, () => WriteRowsToCsv(table, tempFilePath, rowWriteQueue, csvQueue));
        var destHost = DestConnections.FindHost(table.HostCategory);
        UploadFile(table, DestConnections.FindHost(table.HostCategory), SourceConnections.FindSid(table.HostCategory), tempFilePath, csvQueue);
        //var uploadTasks = UploadSubsetRows(table, isIotTable, csvQueue, destinationTableName, destinationColumns, isDb2);
        var uploadTasks = UploadSubsetRows(table, isIotTable, csvQueue, table.TableName, columns, isDb2);

        return uploadTasks.Append(readCompleteTask).Append(writeTask).Append(cacheTask).ToArray();
    }

    public void UploadFile(Table table, string schema, string sid, string tempFilePath, BlockingCollection<string> csvQueue)
    {
        foreach (var baseFileName in csvQueue.GetConsumingEnumerable())
        {
            var result = AWSClient.UploadFile(baseFileName, sid, schema, table, tempFilePath);
        }
    }
    public bool IsSchemaRestrictedTable(Table table, string hostSchemaName)
    {
        var tableFilters = Config.TableSchemaRestrictions;

        var tableHasRestrictions = tableFilters.TryGetValue(table.TableName, out var allowedSchema);
        if (!tableHasRestrictions) return false;
        if (hostSchemaName == allowedSchema)
        {
            Log.Information("Processing schema restricted table {Table} for {Schema}", table, allowedSchema);
            return false;
        }

        Log.Information("Schema table restriction found for {Table} on {Schema}, skipping", table,
            hostSchemaName);
        return true;
    }

    private void GetRows(bool filterOnSecondaries,
        BlockingCollection<(string, Dictionary<string, string>)> queryQueue,
        IHost host,
        BlockingCollection<string[]> rowWriteQueue,
        BlockingCollection<string[]> pkCacheQueue,
        Table table,
        ImmutableArray<Column> columns)
    {
        foreach (var (query, paramsDict) in queryQueue.GetConsumingEnumerable())
        {
            Log.Information(query);
            Log.Information(paramsDict.ToString());
            try
            {
                using var reader = host.ExecuteParameterizedQuery(query, paramsDict);
                var currentQueryRows = new List<string[]>();
                var rows = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetName(i);
                    // rows[i] = DBAbstractionLayer.ConvertValueToStringForCsv(value, ((IList<Column>)columns)[i],                    Config.CompressedColumns);
                    rows[i] = reader.GetName(i);

                }
                currentQueryRows.Add(rows);

                while (reader.Read())
                {
                    var row = new string[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; ++i)
                    {
                        var value = !reader.IsDBNull(i) ? reader.GetValue(i) : null;
                        row[i] = DBAbstractionLayer.ConvertValueToStringForCsv(value, ((IList<Column>)columns)[i],
                            Config.CompressedColumns);
                    }

                    currentQueryRows.Add(row);
                }

                if (filterOnSecondaries)
                {
                    currentQueryRows = FilterRowsOnSecondaries(table, columns, currentQueryRows);
                }

                foreach (var row in currentQueryRows)
                {
                    rowWriteQueue.Add(row);
                    pkCacheQueue.Add(row);
                }
            }
            catch (OracleException oraExc)
            {
                if (oraExc.Number != 942) throw;
                Log.Warning("Ignoring failed query for {Table} on {Host}", table, host.Configuration.Host);
            }
        }
    }

    public Task BuildKeySetCaches(PrimaryKeySet primaryKeySet, BlockingCollection<string[]> pkCacheQueue)
    {
        var table = primaryKeySet.Table;
        var columns = primaryKeySet.Columns;
        Task cacheTask;
        if (!primaryKeySet.SubsidiaryMultiStringKeys.Any())
        {
            cacheTask = Task.CompletedTask;
            Log.Error("Could not determine primary key columns for {Table}, skipping cache creation {PkSet}", table,
                primaryKeySet);
        }
        else
        {
            cacheTask = Task.Run(() =>
                CreateAndPopulateTableCache(table, columns, pkCacheQueue, primaryKeySet.SubsidiaryMultiStringKeys));
        }

        return cacheTask;
    }

    private IEnumerable<Task> UploadSubsetRows(Table table, bool isIotTable, BlockingCollection<string> csvQueue,
        string destinationTable, IList<Column> columns, bool isDb2)
    {
        var uploadTasks = new List<Task>();
        var useConventionalPath = DBAbstractionLayer.MustUseConventionalPath(columns);
        for (var i = 0; i < Config.UploadParallelism; ++i)
        {
            var uploadTask = DestConnections.RunOnCategory(table.HostCategory, host =>
            {
                foreach (var baseFileName in csvQueue.GetConsumingEnumerable())
                {
                    DBAbstractionLayer.BuildSqlLoaderConfig(baseFileName, destinationTable,
                        host.Configuration, columns, isDb2);
                    // UploadCsv(host.Configuration, DBAbstractionLayer.SharedDirectory, baseFileName, 4_000,                        isIotTable, useConventionalPath);
                }
            });
            uploadTasks.AddRange(uploadTask);
        }

        return uploadTasks;
    }

    public void UploadCsv(
        HostConfig hostConfig,
        string path,
        string fileName,
        long maximumRowSizeInBytes,
        bool isTableIndexOrganized,
        bool useConventionalPath)
    {
        const int maxUploadAttempts = 3;
        var attempts = 0;

        var db2LoaderParameters = DB2LoaderUtils.CreateSubsetterImportParameters(
           path,
           fileName,
           maximumRowSizeInBytes,
           isTableIndexOrganized,
           useConventionalPath
       );

        var sqlLoaderParameters = SqlLoaderUtils.CreateSubsetterImportParameters(
                       path,
                       fileName,
                       maximumRowSizeInBytes,
                       isTableIndexOrganized,
                       useConventionalPath);


        while (attempts < maxUploadAttempts)
        {
            try
            {

                if (hostConfig.DbType == DatabaseType.DB2.ToString())
                {
                    DBAbstractionLayer.RunCommand(db2LoaderParameters, db2LoaderParameters.ControlFile, hostConfig, CancellationTokenSource.Token);
                }
                else
                {
                    DBAbstractionLayer.RunCommand(sqlLoaderParameters, sqlLoaderParameters.ControlFile, hostConfig, CancellationTokenSource.Token);

                }
                break;
            }
            catch (Exception ex)
            {
                attempts += 1;
                Log.Warning(ex,
                    "Error {ErrorMessage} encountered on attempt {Attempt}",
                    ex.Message,
                    attempts);
                if (attempts == maxUploadAttempts) throw;
            }
        }
    }

    public static void WriteRowsToCsv(Table table, string tempFilePath, BlockingCollection<string[]> rowWriteQueue,
        BlockingCollection<string> csvQueue, int batchRowCountLimit = DBAbstractionLayer.CsvBatchSize)
    {
        var baseFileName = Path.Combine(tempFilePath, Path.GetRandomFileName());
        var writer = Utilities.OpenTsvFile(baseFileName + ".tsv", table.TableName);
        var count = 0L;
        foreach (var row in rowWriteQueue.GetConsumingEnumerable())
        {
            // Insert the length of the field in UTF-8 at the beginning of the row
            var byteCountString = DBAbstractionLayer.GetByteCountString(row);
            writer.Write(byteCountString);

            count += 1;
            for (var i = 0; i < row.Length; ++i)
            {
                writer.Write(row[i]);
                if (i != row.Length - 1) writer.Write(DBAbstractionLayer.TsvColSeparatingChar);
            }

            if (count < batchRowCountLimit) continue;
            count = 0;
            writer.Dispose();
            csvQueue.Add(baseFileName);
            baseFileName = Path.Combine(tempFilePath, Path.GetRandomFileName());
            writer = Utilities.OpenTsvFile(baseFileName + ".tsv", table.TableName);
        }

        writer.Dispose();
        if (count != 0) csvQueue.Add(baseFileName);
    }

    public static void WriteRowsToCsv(Table table, string tempFilePath, BlockingCollection<string[]> rowWriteQueue,
        BlockingCollection<string> csvQueue, string dbType, int batchRowCountLimit = DBAbstractionLayer.CsvBatchSize)
    {
        var baseFileName = Path.Combine(tempFilePath, "AR.I02.TONIC." + DateTime.UtcNow.ToString("yyMMdd.HHmmss") + "." + table.TableAliasName);
        var writer = Utilities.OpenTsvFile(baseFileName + ".csv", table.TableName);
        var count = 0L;
        var c = dbType;
        foreach (var row in rowWriteQueue.GetConsumingEnumerable())
        {
            var abc = string.Join(DBAbstractionLayer.CsvColSeparatingChar, Enumerable.Range(0, row.Length)
                                                             .Select(i => GetCsvFieldData(row[i].ToString())));
            writer.WriteLine(abc);
            count += 1;
        }

        writer.Dispose();
        if (count != 0) csvQueue.Add(baseFileName);
    }


    private static string GetCsvFieldData(string fieldData)
    {
        if (fieldData.Contains(",") || fieldData.Contains("\""))
        {
            fieldData = "\"" + fieldData.Replace("\"", "\"\"") + "\"";
        }

        return fieldData;
    }

    private void CreateAndPopulateTableCache(Table table, ImmutableArray<Column> columns,
        BlockingCollection<string[]> pkCacheQueue, ImmutableHashSet<MultiStringKey> primaryKeySets)
    {
        using var cacheConnections = new AutoDisposeDict<MultiStringKey, SQLiteConnection>();
        using var cacheKeyContexts = new AutoDisposeDict<MultiStringKey, AddKeyContext>();
        var keyIndexesForKeySet = new Dictionary<MultiStringKey, ImmutableArray<int>>();
        foreach (var keySet in primaryKeySets)
        {
            var pkColumns = keySet.From(2).Parts;

            var hasCache = KeyCaches.HasCache(keySet);
            if (!hasCache)
            {
                Log.Debug("Creating cache for {Table}:{KeySet}", table, keySet.ToString());
            }

            var cache = hasCache ? KeyCaches.GetCache(keySet) : KeyCaches.CreateCache(keySet);
            cacheConnections[keySet] = KeyCaches.OpenConnection();
            cacheKeyContexts[keySet] = cache.PrepareAddKeys(cacheConnections[keySet], false);

            var pkIdx = pkColumns.Select(pkc => columns.FindIndex(c => c.ColumnName == pkc)).ToImmutableArray();
            if (pkIdx.Any(pki => pki == -1))
            {
                var missingPrimaryKeysStr =
                    $"[{string.Join(", ", pkColumns.Where(pkc => columns.FindIndex(c => c.ColumnName == pkc) == -1).ToImmutableArray())}]";
                var colsStr = $"[{string.Join(", ", columns)}]";

                Log.Fatal(
                    "Specified key names are not present in table {Table}! Missing Primary Keys: {MissingKeys} Table Columns: {Columns}",
                    table,
                    missingPrimaryKeysStr,
                    colsStr);
                throw new ArgumentException(
                    $"Specified key names are not present in table {table}! Missing Primary Keys: {missingPrimaryKeysStr} Table Columns: {colsStr}");
            }

            keyIndexesForKeySet[keySet] = pkIdx;
        }

        var count = 0;
        foreach (var row in pkCacheQueue.GetConsumingEnumerable())
        {
            foreach (var keySet in primaryKeySets)
            {
                var key = new string[keySet.Length - 2];
                var j = 0;
                foreach (var i in keyIndexesForKeySet[keySet])
                {
                    key[j++] = row[i];
                }

                KeyCaches[keySet].AddKey(key, cacheKeyContexts[keySet]);
            }

            count += 1;
        }

        if (!Log.IsEnabled(LogEventLevel.Debug)) return;
        var keySetString = string.Join(", ", primaryKeySets.Select(s => s.ToString()));
        Log.Logger.Debug("Added {C} rows for {Table} Caches {Caches}", count, table, keySetString);
    }

    private List<string[]> FilterRowsOnSecondaries(Table table, IList<Column> columns,
        IReadOnlyCollection<string[]> unfilteredRows)
    {
        var secondaryKeys = Config.ForeignKeys.Where(fk => table.IsForeignKeyOf(fk) && fk.IsSecondary)
            .ToList();

        if (!secondaryKeys.Any())
            return unfilteredRows.ToList();

        Log.Information("Filtering Upstream Table {Table} on secondary foreign keys: [{SecondaryKeys}]", table,
            string.Join(", ", secondaryKeys.Select(fk => $"[{string.Join(", ", fk.ForeignKeyColumns)}]")));

        var rowFilter = new bool[unfilteredRows.Count];
        for (var i = 0; i < rowFilter.Length; i++)
        {
            rowFilter[i] = true;
        }

        foreach (var secondaryKey in secondaryKeys)
        {
            var secondaryTable =
                new Table(secondaryKey.PrimaryKeyHostCategory, secondaryKey.PrimaryKeyTable);
            var secondaryMultiStringKey = new MultiStringKey(table, secondaryKey.PrimaryKeyColumns);

            if (!KeyCaches.HasCache(secondaryMultiStringKey))
            {
                Log.Warning(
                    "Secondary foreign key has no values in PK cache. Target: {Table}, Secondary: {Secondary}, skipping to next secondary filter",
                    table, secondaryTable);
                continue;
            }

            var secondaryCache = KeyCaches.GetCache(secondaryMultiStringKey);
            var fkColumns = secondaryKey.ForeignKeyColumns;
            var fkIndexes = fkColumns.Select(fk => columns.FindIndex(col => col.ColumnName == fk)).ToArray();
            if (fkIndexes.Any(fki => fki == -1))
            {
                var missingSecondaryKeysStr =
                    $"[{string.Join(", ", fkColumns.Where(fk => columns.FindIndex(col => col.ColumnName == fk) == -1))}]";
                var colsStr = $"[{string.Join(", ", columns)}]";
                Log.Fatal(
                    "Specified secondary key names are not present in table {Table} Missing Secondary Keys: {MissingKeys}, Table Columns: {Columns}",
                    table,
                    missingSecondaryKeysStr,
                    colsStr);
                throw new ArgumentException(
                    $"Specified secondary key names are not present in table {table}! Missing Secondary Keys: {missingSecondaryKeysStr} Table Columns: {colsStr}");
            }

            var fkValues = unfilteredRows.Select(row => fkIndexes.Select(fki => row[fki].ToString()).ToArray());
            var fkValueInCache = secondaryCache.ContainsKeys(fkValues, secondaryMultiStringKey);

            for (var i = 0; i < rowFilter.Length; i++)
            {
                rowFilter[i] = rowFilter[i] && fkValueInCache[i];
            }
        }

        return unfilteredRows.Zip(rowFilter)
            .Where(tup => tup.Second)
            .Select(tup => tup.First)
            .ToList();
    }
}