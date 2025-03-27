/*
 * Copyright Tonic AI Inc. 2020
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using Tonic.Common.Helpers;
using Tonic.Common.Models;

namespace Tonic.Common.OracleHelper;

public static class OracleUtilities
{
    public const string NullChar = "\\N";
    public const int RecordLengthNumDigits = 10;
    public const char TsvColSeparatingChar = '\u001e';
    public const string SharedDirectory = "/tmp";


    public static readonly HashSet<string> BlobTypes =
        new(new[] { "CLOB", "NCLOB", "BLOB", "LONG" });

    public static string ColumnsSelectStr(IList<Column> columns)
    {
        if (columns.Select(c => (HostCategory: c.HostCategoryName, Table: c.TableName)).Distinct()
                .Count() > 1)
            throw new ArgumentException("Every column must be from the same table.");
        return string.Join(", ", columns.Select(c => $"{c.TableName}.{c.ColumnName}"));
    }

    public static string ColumnsSelectStr(IList<Column> columns,string schema)
    {
        if (columns.Select(c => (HostCategory: c.HostCategoryName, Table: c.TableName)).Distinct()
                .Count() > 1)
            throw new ArgumentException("Every column must be from the same table.");
        return string.Join(", ", columns.Select(c => $"{schema}.{c.TableName}.{c.ColumnName}"));
    }

    public static List<Column> GetColumns(IHost host)
    {
        var query =
            $@"SELECT
                    cols.TABLE_NAME AS ""table_name"",
                    cols.COLUMN_NAME AS ""column_name"",
                    cols.DATA_TYPE AS ""data_type_of_column"",
                    cols.DATA_PRECISION AS ""data_precision_of_column"",
                    CASE cols.NULLABLE WHEN 'Y' THEN 'YES' ELSE 'NO' END AS ""is_column_nullable"",
                    coalesce(max(CASE WHEN ct.CONSTRAINT_TYPE='P' THEN 1 WHEN ct.CONSTRAINT_TYPE='R' THEN 1 ELSE 0 END), 0) as ""is_key_column""
                FROM all_tab_cols cols
                LEFT JOIN
                    (
                        SELECT
                            uc.CONSTRAINT_TYPE,
                            uc.TABLE_NAME,
                            cc.COLUMN_NAME
                        FROM
                            user_cons_columns cc
                        JOIN
                            user_constraints uc
                        ON
                            uc.CONSTRAINT_NAME=cc.CONSTRAINT_NAME
                    ) ct
                ON
                    ct.TABLE_NAME = cols.TABLE_NAME AND ct.COLUMN_NAME = cols.COLUMN_NAME
                WHERE
                    HIDDEN_COLUMN = 'NO' AND OWNER = '{host.Configuration.Schema}'
                GROUP BY
                    cols.TABLE_NAME,
                    cols.COLUMN_NAME,
                    cols.DATA_TYPE,
                    cols.DATA_PRECISION,
                    CASE cols.NULLABLE WHEN 'Y' THEN 'YES' ELSE 'NO' END,
                    cols.COLUMN_ID
                ORDER BY cols.COLUMN_ID";

        using var reader = host.ExecuteQuery(query);
        var columns = new List<Column>();
        var hostCategory = host.Configuration.HostCategory;
        while (reader.Read())
        {
            var tableName = reader.GetString(0);
            var columnName = reader.GetString(1);
            var dataType = reader.GetString(2);
            var isNullable = reader.GetString(4);
            columns.Add(new Column(hostCategory, tableName, columnName, dataType, isNullable == "YES"));
        }

        return columns;
    }

    public static string ConvertValueToStringForCsv(object? value, Column column,
        ImmutableHashSet<string> compressedColumns, bool ignoreDecompression = false)
    {
        var dbType = column.DataType;
        if (value == null || value == DBNull.Value)
        {
            return BlobTypes.Contains(dbType)
                ? $"<blob-438f58ee-f378-41d4-901d-6d7846784e38>{NullChar}<blob-438f58ee-f378-41d4-901d-6d7846784e38>"
                : NullChar;
        }

        dbType = dbType.ToUpper();
        if (dbType.StartsWith("TIMESTAMP") || dbType.StartsWith("TIMESTMP"))
        {
            var date = value is DateTime time ? time : DateTime.Parse(value.ToString()!);
            var abc= date.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            return abc;
        }

        switch (dbType)
        {
            case "TIME":
                return value.ToString()!;
            case "CHAR":
            case "VARCHAR":
            case "VARCHAR2":
            case "NVARCHAR":
            case "NVARCHAR2":
            case "NCHAR":
            case "NCHAR2":
            case "CHARACTER":
            case "CHARACTER_VARYING":
            case "CHAR_VARYING":
            case "NATIONAL_CHARACTER":
            case "NATIONAL_CHAR":
            case "NATIONAL_CHARACTER_VARYING":
            case "NATIONAL_CHAR_VARYING":
            case "NCHAR_VARYING":
            case "LONG_VARCHAR":
                return value.ToString()!;
            case "CLOB":
            case "NCLOB":
            case "LONG":
                return
                    $"<blob-438f58ee-f378-41d4-901d-6d7846784e38>{value}<blob-438f58ee-f378-41d4-901d-6d7846784e38>";
            case "LONG_RAW":
            case "BLOB":
                if (value is not byte[] buffer) return NullChar;
                var compressedColumn = compressedColumns.Contains(column.ColumnSelector);
                if (compressedColumn && !ignoreDecompression)
                {
                    using var compressedStream = new MemoryStream(buffer);
                    using var decompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                    using var decompressedBufferStream = new MemoryStream();

                    decompressedStream.CopyTo(decompressedBufferStream);
                    buffer = decompressedBufferStream.ToArray();
                }

                var utfString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                return
                    $"<blob-438f58ee-f378-41d4-901d-6d7846784e38>{utfString}<blob-438f58ee-f378-41d4-901d-6d7846784e38>";
            case "NUMBER":
            case "BINARY_FLOAT":
            case "BINARY_DOUBLE":
                return value.ToString()!;
            case "DATE":
                var date = value is DateTime time ? time : DateTime.Parse(value.ToString()!);
                return date.ToString("yyyy-MM-dd HH:mm:ss");
            case "XMLTYPE":
                return value.ToString()!;
            case "NUMERIC":
            case "DECIMAL":
            case "INTEGER":
            case "INT":
            case "SMALLINT":
            case "FLOAT":
            case "DOUBLE_PRECISION":
            case "REAL":
                return value.ToString()!;
            case "BFILE":
            case "URIType":
            case "DBURIType":
            case "XDBURIType":
            case "HTTPURIType":
                throw new Exception($"Unsupported database type: {dbType}");
            default:
                throw new Exception($"Unsupported database type: {dbType}");
        }
    }

    public static OracleDbType DbTypeFromString(string dbType)
    {
        return dbType switch
        {
            "CHAR" => OracleDbType.Char,
            "VARCHAR" => OracleDbType.Varchar2,
            "VARCHAR2" => OracleDbType.Varchar2,
            "NVARCHAR" => OracleDbType.NVarchar2,
            "NVARCHAR2" => OracleDbType.NVarchar2,
            "NCHAR" => OracleDbType.NChar,
            "NCHAR2" => OracleDbType.NChar,
            "CHARACTER" => OracleDbType.Char,
            "CHARACTER_VARYING" => OracleDbType.Varchar2,
            "CHAR_VARYING" => OracleDbType.Varchar2,
            "NATIONAL_CHARACTER" => OracleDbType.NChar,
            "NATIONAL_CHAR" => OracleDbType.NChar,
            "NATIONAL_CHARACTER_VARYING" => OracleDbType.NVarchar2,
            "NATIONAL_CHAR_VARYING" => OracleDbType.NVarchar2,
            "NCHAR_VARYING" => OracleDbType.NVarchar2,
            "LONG_VARCHAR" => OracleDbType.Long,
            "CLOB" => OracleDbType.Clob,
            "NCLOB" => OracleDbType.NClob,
            "LONG" => OracleDbType.Long,
            "LONG_RAW" => OracleDbType.LongRaw,
            "BLOB" => OracleDbType.Blob,
            "NUMBER" => OracleDbType.Decimal,
            "BINARY_FLOAT" => OracleDbType.BinaryFloat,
            "BINARY_DOUBLE" => OracleDbType.BinaryDouble,
            "DATE" => OracleDbType.Date,
            "XMLTYPE" => OracleDbType.XmlType,
            "NUMERIC" => OracleDbType.Decimal,
            "DECIMAL" => OracleDbType.Decimal,
            "INTEGER" => OracleDbType.Int32,
            "INT" => OracleDbType.Int32,
            "SMALLINT" => OracleDbType.Int16,
            "FLOAT" => OracleDbType.Single,
            "DOUBLE_PRECISION" => OracleDbType.Double,
            "REAL" => OracleDbType.Decimal,
            "BFILE" => OracleDbType.BFile,
            _ => throw new ArgumentException($"Unknown Oracle Type: {dbType}")
        };
    }

    public static int GetTimezoneVersion(IHost host)
    {
        using var reader = host.ExecuteQuery("SELECT VERSION FROM v$timezone_file");
        reader.Read();
        return reader.GetInt32(0);
    }


    public static ImmutableHashSet<Table> GetIoTTables(IHost host)
    {
        var iotTables = new List<Table>();
        using var reader = host.ExecuteQuery(
            $"SELECT table_name FROM all_tables WHERE owner = '{host.Configuration.Schema}' AND iot_type IS NOT NULL AND table_name NOT LIKE 'SYS_IOT_OVER_%'");

        while (reader.Read())
        {
            iotTables.Add(new Table(host.Configuration.HostCategory, reader.GetString(0)));
        }

        return iotTables.ToImmutableHashSet();
    }

    public static string GetByteCountString(IReadOnlyList<object?> row) =>
        (row.Select(c => c?.ToString()).Select(Encoding.UTF8.GetByteCount).Sum() +
         Encoding.UTF8.GetByteCount(TsvColSeparatingChar.ToString()) * (row.Count - 1))
        .ToString($"D{RecordLengthNumDigits}");

    public static void SetCommandText(this OracleCommand command, string text, IHost host)
    {
        if (command.Parameters.Count > 0)
        {
            var values = new List<string>();
            for (var i = 0; i < command.Parameters.Count; ++i)
            {
                values.Add(command.Parameters[i].Value.ToString() ?? "NULL");
            }

            Log.Debug("Querying: {Url}:{Host}\n\t{Text}\nParams:\n\t{Params}", host.Configuration.Host,
                host.Name, text, $"[{string.Join(",", values)}]");
        }
        else
        {
            Log.Debug("Querying: {Host}\n\t{Text}", host.Name, text);
        }

        command.CommandText = text;
    }
}