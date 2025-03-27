using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Tonic.Common.Configs;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Common.Utils;

namespace Tonic.Common.DB2Helper
{

    public static class DB2LoaderUtils
    {

        private static readonly ImmutableHashSet<string> _invalidDirectPathTypes = new HashSet<string> { "XMLTYPE", "GENERATED", "VARRAY", "CLOB", "NCLOB", "BLOB" }.ToImmutableHashSet();
               

        public static DB2LoaderParameter CreateSubsetterImportParameters(
            string path,
            string fileName,
            long maximumRowSizeInBytes,
            bool isTableIndexOrganized,
            bool useConventionalPath)
        {
            var parameters = new DB2LoaderParameter
            {
                Direct = !useConventionalPath,
                ControlFile = OracleHelperUtils.ControlFilePath(path, fileName),
                LogFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.LogFilePath(path, fileName)),
                BadFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.BadFilePath(path, fileName)),
                Errors = OracleHelperUtils.CsvBatchSize,
                ReadSize = 33554432,
                BindSize = 33554432
            };
            if (!isTableIndexOrganized && !useConventionalPath) parameters.Rows = 10_000;
            var maximumConcurrentRows = OracleHelperUtils.TwoGigabytes / maximumRowSizeInBytes;
            parameters.ColumnArrayRows = maximumConcurrentRows < OracleHelperUtils.DefaultColumnArrayRows
                ? maximumConcurrentRows
                : OracleHelperUtils.DefaultColumnArrayRows;

            return parameters;
        }       

        

        public static void BuildDB2LoaderConfig(string baseFileName, string table, HostConfig schema,
            IEnumerable<Column> columns)
        {
            using var writer =
                new StreamWriter(new FileStream($"{baseFileName}.ctl", FileMode.Create, FileAccess.Write));
            Utilities.MakeFilePublicOnUnix($"{baseFileName}.ctl");
            writer.WriteLine($"connect to {schema.Sid} user {schema.User} using {schema.Password}");

            writer.Write($"LOAD CLIENT FROM '{baseFileName}.csv' OF DEL INSERT INTO \"{schema.Schema.ToUpperInvariant()}\".\"{table.ToUpperInvariant()}\"");
            writer.Write("(");

            var columnLines = columns.Select(x => x.ColumnName);
            writer.Write(string.Join(",", columnLines));

            writer.WriteLine("  )");
            
            writer.WriteLine($"set integrity for  \"{schema.Schema.ToUpperInvariant()}\".\"{table.ToUpperInvariant()}\" immediate checked;");

            writer.Write("terminate");


        }
    }
}
