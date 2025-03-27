using System;
using System.Collections.Concurrent;
using System.Linq;
using Serilog;
using Tonic.Common;
using Tonic.Common.Configs;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Common.Utils;
using Tonic.Subsetter;
using Tonic.Subsetter.Utils;
using Tonic.Test.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Tonic.Test;

public class CopyRowsServiceUploadTests
{
    public CopyRowsServiceUploadTests(ITestOutputHelper output)
    {
        Environment.SetEnvironmentVariable("TONIC_ORACLE_HELPER_URL", "http://localhost:5000");

        var loggerConfiguration = new LoggerConfiguration();
        Log.Logger = loggerConfiguration
            .MinimumLevel
            .Debug()
            .WriteTo.Console()
            .WriteTo.TestOutput(output)
            .CreateLogger();

        var runId = Utilities.RunId();
        _copyRowsService = new CopyRowsService(
            runId,
            new SubsetConfig(), 
            new HostsService(new[] { DestHost.Configuration }),
            new HostsService(new[] { DestHost.Configuration }), 
            new PrimaryKeyCaches(runId));
    }

    // [Fact]
#pragma warning disable xUnit1004
    //[Fact(Skip = "Requires Tonic Oracle Helper to be running.")]
    [Fact]
#pragma warning restore xUnit1004
    public void TestUploadCsv()
    {
        var table = new Table("TONIC", "UPLOAD_TEST");
        UploadTestUtils.TruncateTable(UploadHost, table);
        var csvQueue = new BlockingCollection<string>();
        var rowWriteQueue = new BlockingCollection<string[]>();

        rowWriteQueue.Add(new[] { "0", "0" });
        rowWriteQueue.CompleteAdding();
        CopyRowsService.WriteRowsToCsv(table, OracleHelperUtils.SharedDirectory, rowWriteQueue, csvQueue, 0);
        csvQueue.CompleteAdding();

        var baseFileName = csvQueue.First();
        var destinationColumns = new[]
        {
            new Column("OBI", "UPLOAD_TEST", "ID", "INT", false),
            new Column("OBI", "UPLOAD_TEST", "COL1", "VARCHAR", false)
        };
        SqlLoaderUtils.BuildSqlLoaderConfig(baseFileName, table.TableName,
            DestHost.Configuration.Schema, destinationColumns);

        _copyRowsService.UploadCsv(DestHost.Configuration, OracleHelperUtils.SharedDirectory, baseFileName,
            4_000, false,
            false);

        var tableContents = UploadTestUtils.GetTableContents(UploadHost, table, destinationColumns).ToList();
        Assert.Single(tableContents);
        Assert.True(new[] { "0", "0" }.SequenceEqual(tableContents.First()));
    }

    private readonly CopyRowsService _copyRowsService;

    private static readonly Host DestHost = new("TEST", new HostConfig
    {
        User = "OBI",
        Password = "Jd9ef6tg",
        Sid = "LUX",
        Host = "core-db-vdb.obexchange.com",
        Port = 1521,
        HostCategory = "TEST",
        Schema = "OBI",
        ShardedIndex = 0
    });

    private static readonly IHost UploadHost = new Host("TEST", new HostConfig
    {
        User = "OBI",
        Password = "Jd9ef6tg",
        Sid = "LUX",
        Host = "core-db-vdb.obexchange.com",
        Port = 1521,
        HostCategory = "TEST",
        Schema = "OBI"
    });

}