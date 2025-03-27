using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Serilog;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Common.Utils;

namespace Tonic.Common.Helpers;

public interface ITsvWriter
{
    public void WriteRow(IReadOnlyList<string> row);
    public IEnumerable<string> TakeFiles();
    public List<string> TakeAllFiles();
}

public class TsvWriter : ITsvWriter
{
    public Func<string, string, TextWriter> OpenTsvFile { get; init; } = Utilities.OpenTsvFile;
    public string HostAndTable { get; init; } = string.Empty;
    private IList<ColumnKey> Columns { get; init; } = ImmutableArray<ColumnKey>.Empty;
    public string TempFilePath { get; init; } = OracleUtilities.SharedDirectory;
    private Dictionary<int, int> FileCount { get; } = new();
    private Dictionary<int, int> RowCount { get; } = new();
    public int RowLimit { get; init; }
    private List<string> Files { get; set; } = new();
    private bool Intermediate { get; init; }
    private static string Extension => ".tsv";
    public int DestinationCount { get; init; } = 1;
    public Func<IList<ColumnKey>, IReadOnlyList<string>, int, int> ShardDiscriminant { get; init; } = (_, _, _) => 0;
    private AutoDisposeDict<int, TextWriter> Writers { get; } = new();
    public char Separator { get; init; } = OracleUtilities.TsvColSeparatingChar;
    public static string FieldSeparator => "__";


    public static TsvWriter CreateInstance(string tempFilePath, Table table, IList<ColumnKey> columns, int rowLimit,
        DestinationShard<Func<IList<ColumnKey>, IReadOnlyList<string>, int, int>> destinationShard, int shardCount) =>
        new()
        {
            TempFilePath = tempFilePath,
            HostAndTable = table.HostAndTable(),
            Columns = columns,
            RowLimit = rowLimit,
            DestinationCount = shardCount,
            ShardDiscriminant = destinationShard.Discriminant,
            Intermediate = false,
        };

    public static TsvWriter CreateInstance(string tempFilePath, Table table, int rowLimit, bool intermediate) =>
        new()
        {
            TempFilePath = tempFilePath,
            HostAndTable = table.HostAndTable(),
            RowLimit = rowLimit,
            Intermediate = intermediate,
            DestinationCount = 1,
        };

    public void WriteRow(IReadOnlyList<string> row)
    {
        var destinationIndex = DestinationCount == 1 ? 0 : ShardDiscriminant(Columns, row, DestinationCount);
        if (!Writers.ContainsKey(destinationIndex))
        {
            FileCount.TryAdd(destinationIndex, 0);
            RowCount.TryAdd(destinationIndex, 0);
            var fileName = GetNextFileName(destinationIndex, FileCount[destinationIndex]) + Extension;
            Writers[destinationIndex] = OpenTsvFile(fileName, HostAndTable);
            Log.Debug("Opened intermediate {Sharded} {File} for {Table}", DestinationCount == 1, fileName,
                HostAndTable);
        }

        var writer = Writers[destinationIndex];
        AddRowToWriter(row, writer, Separator);

        RowCount[destinationIndex] += 1;
        if (RowCount[destinationIndex] < RowLimit)
        {
            return;
        }

        writer.Dispose();
        var currentFile = GetNextFileName(destinationIndex, FileCount[destinationIndex]++);
        Files.Add(currentFile);
        RowCount[destinationIndex] = 0;
        var nextFile = GetNextFileName(destinationIndex, FileCount[destinationIndex]);
        Writers[destinationIndex] = OpenTsvFile(nextFile + Extension, HostAndTable);
    }

    private static void AddRowToWriter(IReadOnlyList<string> row, TextWriter writer, char separator)
    {
        // Insert the length of the field in UTF-8 at the beginning of the row
        writer.Write(OracleUtilities.GetByteCountString(row));

        for (var i = 0; i < row.Count; ++i)
        {
            writer.Write(row[i]);
            if (i != row.Count - 1)
            {
                writer.Write(separator);
            }
        }
    }

    public IEnumerable<string> TakeFiles()
    {
        var next = Files;
        Files = new List<string>();
        return next;
    }

    public List<string> TakeAllFiles()
    {
        var files = new List<string>();
        var indexes = Writers.Keys;
        foreach (var index in indexes)
        {
            var writer = Writers[index];
            writer.Dispose();
            if (RowCount[index] != 0)
            {
                var fileName = GetNextFileName(index, FileCount[index]);
                files.Add(Path.Combine(TempFilePath, fileName));
            }

            Writers.Remove(index);
        }

        return files;
    }

    private string GetNextFileName(int destinationIndex, int index) => Path.Combine(TempFilePath,
        $"{HostAndTable}{FieldSeparator}{destinationIndex}{FieldSeparator}{(Intermediate ? $"{FieldSeparator}intermediate{FieldSeparator}" : "")}{index}");

    public static int GetIndexForBaseName(string baseFileName)
    {
        var baseName = new DirectoryInfo(baseFileName).Name;
        var parts = baseName.Split(FieldSeparator);
        if (!int.TryParse(parts[1], out var index))
            throw new InvalidDataException($"Could not retrieve destination index from filename {baseName}");
        return index;
    }
}