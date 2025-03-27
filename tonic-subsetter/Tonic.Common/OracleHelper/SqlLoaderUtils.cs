using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Tonic.Common.Configs;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper.Models;
using Tonic.Common.OracleHelper.Models.DataPump;
using Tonic.Common.Utils;

namespace Tonic.Common.OracleHelper;

public static class SqlLoaderUtils
{
    
    private static readonly ImmutableHashSet<string> _invalidDirectPathTypes = new HashSet<string>{"XMLTYPE", "GENERATED", "VARRAY", "CLOB", "NCLOB", "BLOB"}.ToImmutableHashSet();

    public static bool MustUseConventionalPath(IEnumerable<Column> columns)
    {
        var presentDataTypes = columns.Select(c => c.DataType).ToImmutableHashSet();
        var conflictingDataTypes = _invalidDirectPathTypes.Union(presentDataTypes);
        return conflictingDataTypes.Any();
    }
        
    public static SqlLoaderParameters CreateSubsetterImportParameters(
        string path,
        string fileName,
        long maximumRowSizeInBytes,
        bool isTableIndexOrganized,
        bool useConventionalPath)
    {
        var parameters = new SqlLoaderParameters
        {
            Direct = !useConventionalPath,
            ControlFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.ControlFilePath(path, fileName)),
            LogFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.LogFilePath(path, fileName)),
            BadFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.BadFilePath(path, fileName)),
            Errors = OracleHelperUtils.CsvBatchSize,
            ReadSize = 33554432,
            BindSize = 33554432
        };
        // SQL*Loader-292: ROWS parameter ignored when an XML, LOB or VARRAY column is loaded
        if (!isTableIndexOrganized && !useConventionalPath) parameters.Rows = 10_000;
        var maximumConcurrentRows = OracleHelperUtils.TwoGigabytes / maximumRowSizeInBytes;
        parameters.ColumnArrayRows = maximumConcurrentRows < OracleHelperUtils.DefaultColumnArrayRows
            ? maximumConcurrentRows
            : OracleHelperUtils.DefaultColumnArrayRows;

        return parameters;
    }

    public static ParametersBase CreateRemapperImportParameters(string fileName, bool isIotTable, int rows)
    {
        var parameters = new SqlLoaderParameters
        {
            ControlFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.ControlFilePath(fileName)),
            LogFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.LogFilePath(fileName)),
            BadFile = OracleHelperUtils.SingleQuote(OracleHelperUtils.BadFilePath(fileName)),
            Errors = OracleHelperUtils.CsvBatchSize,
            ReadSize = 33554432,
            BindSize = 33554432
        };
        if (!isIotTable)
            parameters.Rows = rows;
        return parameters;
    }

    public static DataPumpImportParameters CreateRemapSchemaParameters(HostConfig lhConfig,
        List<Table> lhTables, HostConfig sourceConfig, bool mergePartitions, bool debugImport,
        string linkName, bool keepPrimaryKeys)
    {
        var parameters = new DataPumpImportParameters
        {
            JobName = $"tonic_{Convert.ToBase64String(Guid.NewGuid().ToByteArray())}".TrimEnd('='),
            NetworkLink = linkName,
            Tables = lhTables.Select(t => $"{sourceConfig.Schema}.{t.TableName.ToUpperInvariant()}")
                .ToArray(),
            Content = ContentMode.MetadataOnly,
            RemapSchema = new[] { $"'{sourceConfig.Schema}:{lhConfig.Schema}'" },
            Exclude = new[]
            {
                OracleObjectType.Trigger, OracleObjectType.Grant, OracleObjectType.RefConstraint,
                OracleObjectType.Statistics
            },
            Transform = new[] { TransformFactory.SegmentAttributes(false) }
        };

        if (!debugImport)
            parameters.NoLogFile = true;
        if (mergePartitions)
            parameters.PartitionOptions = PartitionOptions.Merge;
        if (keepPrimaryKeys) return parameters;
        var newExcludes = parameters.Exclude.ToList();
        newExcludes.AddRange(new[] { OracleObjectType.Constraint, OracleObjectType.Index });
        parameters.Exclude = newExcludes.ToArray();

        return parameters;
    }

    public static void BuildSqlLoaderConfig(string baseFileName, string table, string schema,
        IEnumerable<Column> columns)
    {
        using var writer =
            new StreamWriter(new FileStream($"{baseFileName}.ctl", FileMode.Create, FileAccess.Write));
        Utilities.MakeFilePublicOnUnix($"{baseFileName}.ctl");

        writer.WriteLine(
            $"LOAD DATA CHARACTERSET UTF8 INFILE '{baseFileName}.tsv' \"VAR {OracleUtilities.RecordLengthNumDigits}\"");
        writer.WriteLine($"  APPEND INTO TABLE \"{schema.ToUpperInvariant()}\".\"{table.ToUpperInvariant()}\"");
        writer.WriteLine("  FIELDS TERMINATED BY x'1e' (");

        var columnLines = columns.Select(OracleHelperUtils.GetColumnConfig);
        writer.WriteLine(string.Join(",\n", columnLines));

        writer.WriteLine("  )");
    }

    public static void BuildDB2LoaderConfig(string baseFileName, string table, HostConfig schema,
        IEnumerable<Column> columns)
    {
        using var writer =
            new StreamWriter(new FileStream($"{baseFileName}.ctl", FileMode.Create, FileAccess.Write));
        Utilities.MakeFilePublicOnUnix($"{baseFileName}.ctl");
        writer.WriteLine($"connect to {schema.Sid} user {schema.User} using {schema.Password}");

        writer.WriteLine($"LOAD CLIENT FROM '{baseFileName}.csv'");
        writer.WriteLine($" INSERT INTO TABLE \"{schema.Schema.ToUpperInvariant()}\".\"{table.ToUpperInvariant()}\"");
        //writer.WriteLine("  FIELDS TERMINATED BY x'1e' (");

        var columnLines = columns.Select(x=>x.ColumnName);
        writer.WriteLine(string.Join(",\n", columnLines));

        writer.WriteLine("  )");
        writer.WriteLine("terminate");


    }
}