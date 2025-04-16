using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Tonic.Common.Configs;
using Tonic.Common.DB2Helper;
using Tonic.Common.Enums;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Common.OracleHelper.Models;

namespace Tonic.Common.CommonAbstraction
{
    public static class DBAbstractionLayer
    {
        public const int CsvBatchSize = 10_000;
        public const string SharedDirectory = "/tmp";
        public const int RecordLengthNumDigits = 10;
        public const char TsvColSeparatingChar = '\u001e';
        public const char CsvColSeparatingChar = ',';

        private static OracleCommandRunner OracleCommandRunner { get; } = new();
        private static DB2CommandRunner DB2CommandRunner { get; } = new();


        public static string ColumnsSelectStr(IList<Column> columns, int config)
        {
           return OracleUtilities.ColumnsSelectStr(columns);
        }

        public static string ColumnsSelectStr(IList<Column> columns, string schema)
        {
            return OracleUtilities.ColumnsSelectStr(columns,schema);
        }

        public static List<Column> GetColumns(IHost host)
        {
            return host.Configuration.DbType == DatabaseType.DB2.ToString() ? DB2Utilities.GetColumns(host) :OracleUtilities.GetColumns(host) ;
        }

        public static ImmutableHashSet<Table> GetIoTTables(IHost host)
        {
            return host.Configuration.DbType == DatabaseType.DB2.ToString() ? null: OracleUtilities.GetIoTTables(host);
        }

        public static string ConvertValueToStringForCsv(object? value, Column column,
        ImmutableHashSet<string> compressedColumns, bool ignoreDecompression = false)
        {
           return OracleUtilities.ConvertValueToStringForCsv(value, column, compressedColumns);
        }

        public static string GetByteCountString(IReadOnlyList<object?> row)
        {
            return (row.Select(c => c?.ToString()).Select(Encoding.UTF8.GetByteCount).Sum() +
     Encoding.UTF8.GetByteCount(TsvColSeparatingChar.ToString()) * (row.Count - 1))
    .ToString($"D{RecordLengthNumDigits}");
        }

        public static void RunCommand<T>(T toolParameters,string filename, HostConfig lh, CancellationToken cancellationToken) where T : ParametersBase
        {
            if (lh.DbType == DatabaseType.DB2.ToString())
            {
                DB2CommandRunner.RunCommand(toolParameters, filename, lh, cancellationToken).Wait();

            }
            else
            {
                OracleCommandRunner.RunCommand(toolParameters, lh, cancellationToken).Wait();
            }

        }
        public static bool MustUseConventionalPath(IEnumerable<Column> columns)
        {
            return SqlLoaderUtils.MustUseConventionalPath(columns);
        }
        public static void BuildSqlLoaderConfig(string baseFileName, string table, HostConfig schema,
        IEnumerable<Column> columns, bool isDb2)
        {
             if (isDb2)
            {
                DB2LoaderUtils.BuildDB2LoaderConfig(baseFileName, table,
                       schema, columns);
            }
            else
            {
                SqlLoaderUtils.BuildSqlLoaderConfig(baseFileName, table,
                       schema.Schema, columns);
            }
        }


        public static ImmutableDictionary<string,string> GetTableAlias(IHost host,IList<string> tableNames)
        {
            return DB2Utilities.GetTableAlias(host, tableNames);
        }

    }

}
