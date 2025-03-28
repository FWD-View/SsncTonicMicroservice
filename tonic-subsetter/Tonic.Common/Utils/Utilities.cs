using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Unix;
using Serilog;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using FileAccessPermissions = Mono.Unix.FileAccessPermissions;

namespace Tonic.Common.Utils;

public static class Utilities
{
    public static Task TaskForQueue<T>(BlockingCollection<T> queue, Action action) =>
        TaskForQueue(queue, () =>
        {
            action();
            return Task.CompletedTask;
        });

    public static Task TaskForQueue<T>(BlockingCollection<T> queue, Func<Task> action) =>
        Task.Run(action).ContinueWith(t =>
        {
            queue.CompleteAdding();
            if (t.IsFaulted && t.Exception != null) throw t.Exception;
        });

    public static (string, IList<Column>) SendTo0TableHack(Table table, IList<Column>? columns = null)
    {
        //
        // HACK: destinationTable and destinationColumn re-route all data for the numbered tables
        //       to one table, the 0th table. This simplifies downstream processing.
        //
        var table0 = table.Grouped
            ? Regex.Replace(table.TableName, "[0-9]", "0")
            : table.TableName;
        if (columns == null)
            return (table0, ArraySegment<Column>.Empty);
        var columns0 = table.Grouped
            ? columns.Select(c => c.WithTable(table0)).ToImmutableArray()
            : columns;
        return (table0, columns0);
    }

    public static int FindIndex<T>(this IList<T> source, Predicate<T> match)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (match(source[i])) return i;
        }

        return -1;
    }

    public static void MakeFilePublicOnUnix(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        var fileInfo = new UnixFileInfo(path);
        if (!fileInfo.Exists) throw new ArgumentException($"path '{path}' must be an existing file of directory");

        fileInfo.FileAccessPermissions |= FileAccessPermissions.OtherRead;
        fileInfo.Refresh();
        if (fileInfo.IsDirectory)
        {
            fileInfo.FileAccessPermissions |= FileAccessPermissions.OtherExecute | FileAccessPermissions.OtherWrite;
            fileInfo.Refresh();
        }
    }

    public static string CreateBaseDirectory(string baseDir,
        string sharedDirectory = OracleHelperUtils.SharedDirectory)
    {
        var tempFilePath = Path.Combine(sharedDirectory, baseDir);
        Directory.CreateDirectory(tempFilePath);
        MakeFilePublicOnUnix(tempFilePath);
        Log.Information("File path {TempFilePath}", tempFilePath);
        return tempFilePath;
    }

    public static IEnumerable<string> ExtractFilesFromArgs(string[] args)
    {
        var idx = args.FindIndex(arg => !arg.StartsWith("-"));
        return idx > 0 ? args[idx..] : args;
    }

    public static string RunId() => DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

    public static string InterpolateTableName(string tablePattern, int idx)
    {
        var count = tablePattern.Count(c => c == '0');
        if (count < idx.ToString().Length)
            throw new ArgumentException(
                $"table pattern: '{tablePattern}' doesn't have enough digits for index: '{idx}'");
        var format = new string('0', count);
        return tablePattern.Replace(format, idx.ToString(format));
    }

    public static (string, Dictionary<string, string>) KeyClauseByMembershipGroup(IList<string> keyColumns,
        List<List<string>> filterValues, int keyClauseBatchSize = 999)
    {
        if (keyColumns == null || filterValues == null)
        {
            throw new ArgumentException("Key columns or filter values were null");
        }

        if (filterValues.Count > keyClauseBatchSize)
        {
            throw new ArgumentException(
                $"Filter values has too many values: {filterValues.Count} max is {keyClauseBatchSize}");
        }

        // when there are no filter values we want the WHERE clause to return nothing, this is the closest Oracle has to WHERE FALSE
        if (!filterValues.Any()) return ("1 = 0", new Dictionary<string, string>());

        var (_, memberClauses, paramLookups) = keyColumns.Zip(filterValues)
            .Aggregate((0, new List<string>(), new List<KeyValuePair<string, string>>()), (acc, current) =>
            {
                var (column, values) = current;
                var (index, clauses, paramMaps) = acc;
                var valueMappings = values.ToDictionary(_ => $":kp{index++}", v => v);
                clauses.Add($"{column} IN ({string.Join(", ", valueMappings.Keys)})");
                paramMaps.AddRange(valueMappings);
                return (index, clauses, paramMaps);
            });
        var keyClause = string.Join(" AND ", memberClauses);
        var paramsDict = paramLookups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return (keyClause, paramsDict);
    }

    public static (string, Dictionary<string, string>) KeyClause(MultiStringKey multiStringKey,
        IList<string[]> filterValues, int keyClauseBatchSize = 999)
    {
        if (multiStringKey == null || filterValues == null || multiStringKey.Parts.Length <= 2)
        {
            throw new ArgumentException("Key columns or filter values were null");
        }

        if (filterValues.Count > keyClauseBatchSize)
        {
            throw new ArgumentException(
                $"Filter values has too many values: {filterValues.Count} max is {keyClauseBatchSize}");
        }

        var keyColumns = multiStringKey.From(2).Parts;
        // when there are no filter values we want the WHERE clause to return nothing, this is the closest Oracle has to WHERE FALSE
        if (!filterValues.Any()) return ("1 = 0", new Dictionary<string, string>());

        var pkcTuple = keyColumns.Length == 1 ? $"{keyColumns[0]}" : $"({string.Join(", ", keyColumns)})";
        var paramCount = filterValues[0].Length * filterValues.Count;

        var paramNames = Enumerable.Range(0, paramCount).Select(i => $":kp{i}");

        var flattenedValues = filterValues.SelectMany(arr => arr);
        var paramsToValues = paramNames.Zip(flattenedValues)
            .ToDictionary(tuple => tuple.First, tuple => tuple.Second);

        var paramClauses = new List<string>();
        for (var i = 0; i < paramCount; i += keyColumns.Length)
        {
            var curCount = i;
            paramClauses.Add(keyColumns.Length == 1
                ? $":kp{curCount}"
                : $"({string.Join(",", Enumerable.Range(0, keyColumns.Length).Select(k => $":kp{curCount + k}"))})");
        }

        var paramInClause = string.Join(",", paramClauses);
        return ($"{pkcTuple} IN ({paramInClause})", paramsToValues);
    }

    public static (string, Dictionary<string, string>) KeyClause(IList<string> keyColumns,
        IList<string[]> filterValues, int upstream = 0, int keyClauseBatchSize = 999)
    {
        if (keyColumns == null || filterValues == null)
        {
            throw new ArgumentException("Key columns or filter values were null");
        }

        if (filterValues.Count > keyClauseBatchSize)
        {
            throw new ArgumentException(
                $"Filter values has too many values: {filterValues.Count} max is {keyClauseBatchSize}");
        }

        // when there are no filter values we want the WHERE clause to return nothing, this is the closest Oracle has to WHERE FALSE
        if (!filterValues.Any()) return ("1 = 0", new Dictionary<string, string>());

        var pkcTuple = keyColumns.Count == 1 ? $"{keyColumns[0]}" : $"({string.Join(", ", keyColumns)})";
        var paramCount = filterValues[0].Length * filterValues.Count;

        var paramNames = Enumerable.Range(0, paramCount).Select(i => $":kp{i}");

        var flattenedValues = filterValues.SelectMany(arr => arr);
        var paramsToValues = paramNames.Zip(flattenedValues)
            .ToDictionary(tuple => tuple.First, tuple => tuple.Second);

        var paramClauses = new List<string>();
        for (var i = upstream; i < paramCount; i += keyColumns.Count)
        {
            var curCount = i;
            paramClauses.Add(keyColumns.Count == 1
                ? $":kp{curCount}"
                : $"({string.Join(",", Enumerable.Range(0, keyColumns.Count).Select(k => $":kp{curCount + k}"))})");
        }

        var paramInClause = string.Join(",", paramClauses);
        return ($"{pkcTuple} IN ({paramInClause})", paramsToValues);
    }

    public static void PrintTableCounts(Dictionary<string, string> tableCounts)
    {
        var tableKeys = tableCounts.Keys.ToList();
        tableKeys.Sort();

        var total = 0;
        foreach (var key in tableKeys)
        {
            var count = tableCounts[key];
            Log.Information("{TableKey} - {Count}", key.PadRight(50), count);
            if (int.TryParse(count, out var c)) total += c;
        }

        Log.Information("TOTAL - {Total}", total);
    }

    public static StreamWriter OpenTsvFile(string filePath, string purpose)
    {
        Log.Information("Opening {FilePath} for data in {Purpose}", filePath, purpose);
        var writer = new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write),
            new UTF8Encoding(false));
        MakeFilePublicOnUnix(filePath);
        return writer;
    }

    public static string GetShardedTableName(Table table, int destinationIndex,
        ImmutableDictionary<(string, int), ImmutableDictionary<Table, int>> shardMappings)
    {
        var hostCategory = table.HostCategory;
        if (!shardMappings.ContainsKey((hostCategory, destinationIndex))) return table.TableName;
        var tableToShardIndexMap = shardMappings[(hostCategory, destinationIndex)];
        if (!tableToShardIndexMap.ContainsKey(table)) return table.TableName;
        return Regex.Replace(table.TableName, "[0-9]{1,2}", tableToShardIndexMap[table].ToString("00"));
    }

    public static (Task, IList<BlockingCollection<T>>) CopyQueue<T>(BlockingCollection<T> source,
        int multiples)
    {
        var queues = new BlockingCollection<T>[multiples];
        for (var i = 0; i < multiples; ++i) queues[i] = new BlockingCollection<T>();


        var task = Task.Run(() =>
        {
            foreach (var o in source.GetConsumingEnumerable())
            {
                for (var i = 0; i < multiples; ++i)
                {
                    queues[i].Add(o);
                }
            }

            for (var i = 0; i < multiples; ++i) queues[i].CompleteAdding();
        });
        return (task, queues);
    }
}