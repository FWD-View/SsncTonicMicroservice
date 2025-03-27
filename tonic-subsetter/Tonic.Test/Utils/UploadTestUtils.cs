#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Common.Utils;

namespace Tonic.Test.Utils;

public static class UploadTestUtils
{
    public static BlockingCollection<string> CreateUploadFiles(Table table, int batchSize, List<string[]> rows)
    {
        var tempFilePath = Utilities.CreateBaseDirectory($"tonic_pkremapper_{Guid.NewGuid()}");
        var tsvHelper = TsvWriter.CreateInstance(tempFilePath, table, batchSize, false);
        foreach (var row in rows)
        {
            tsvHelper.WriteRow(row.ToImmutableArray());
        }

        var csvQueue = new BlockingCollection<string>();
        foreach (var file in tsvHelper.TakeFiles())
        {
            csvQueue.Add(file);
        }

        csvQueue.CompleteAdding();
        return csvQueue;
    }
    
    public static string CreateTsvFile(Table table, IList<Column> columns, List<string[]> rows)
    {
        var tempFilePath = Path.Combine(OracleUtilities.SharedDirectory, $"tonic_pkremapper_{Utilities.RunId()}");
        Directory.CreateDirectory(tempFilePath);
        Utilities.MakeFilePublicOnUnix(tempFilePath);
        var tsvWriter = TsvWriter.CreateInstance(tempFilePath, table, OracleHelperUtils.CsvBatchSize, true);
        foreach (var row in rows)
        {
            tsvWriter.WriteRow(row);
        }

        return tsvWriter.TakeAllFiles().Single();
    }

    public static void CleanupTsvFile(string fileName)
    {
        var path = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrEmpty(path)) Directory.Delete(path, true);
    }
    

    public static void TruncateTable(IHost host, Table table)
    {
        host.ExecuteNonQuery($"TRUNCATE TABLE {host.Configuration.Schema}.{table.TableName}");
    }

    public static void InsertRow(IHost host, Table table, IList<Column> columns, string[] duplicate)
    {
        var columnsStr = string.Join(", ", columns.Select(c => c.ColumnName));
        var values = columns.Zip(duplicate, (column, s) => column.DataType == "NUMBER" ? s : $"'{s}'");
        var valuesStr = string.Join(", ", values);
        host.ExecuteNonQuery(
            $"INSERT INTO {host.Configuration.Schema}.{table.TableName} ({columnsStr}) VALUES ({valuesStr})");
    }

    public static IEnumerable<object[]> GetTableContents(IHost host, Table table,
        IEnumerable<Column>? columns)
    {
        var columnsSelect = columns == null ? "*" : string.Join(", ", columns.Select(c => c.ColumnName));
        using var reader =
            host.ExecuteQuery(
                $"SELECT {columnsSelect} FROM {host.Configuration.Schema}.{table.TableName}");
        var rows = new List<object[]>();
        while (reader.Read())
        {
            var row = new object[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    public static bool TableExists(IHost host, Table table, bool isTemp = true)
    {
        var tableName = isTemp ? $"TEMP_{table.TableName}" : table.TableName;
        using var reader = host.ExecuteQuery(
            $@"SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = '{tableName}'");
        if (!reader.Read()) throw new InvalidOperationException();
        var numTables = reader.GetInt32(0);
        if (numTables != 0 && numTables != 1) throw new InvalidOperationException();
        return numTables > 0;
    }
}