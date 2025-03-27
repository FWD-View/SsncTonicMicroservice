using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Serilog;
using Tonic.Common.OracleHelper;
using Tonic.Common.Utils;

namespace Tonic.ResetSchema;

public static class ResetSchemaMain
{
    private static void ConfigureLogging(bool debug)
    {
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration =
            debug ? loggerConfiguration.MinimumLevel.Debug() : loggerConfiguration.MinimumLevel.Information();
        Log.Logger = loggerConfiguration
            .WriteTo.Console()
            .WriteTo.File("/tmp/reset_schema_cli.log", rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }

    public static void Main(string[] args)
    {
        var files = Utilities.ExtractFilesFromArgs(args);

        var config = (args.Length == 0
                ? Console.OpenStandardInput()
                : new ConcatenatedStream(files.Select(f => new FileStream(f, FileMode.Open))))
            .ParseResetSchemaConfig();

        ConfigureLogging(config.DebugLogging);
        Log.Information("Resetting the following schemas: {Schemas}",
            config.DestinationHosts.Select(host =>
                $"{host.Configuration.Host}/{host.Configuration.Sid}/{host.Configuration.Schema}").ToArray());

        Log.Information("You have 10 seconds to cancel this before we proceed");
        Thread.Sleep(10000);
        Log.Information("Proceeding");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var httpClient = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = ((_, _, _, _) => true)
        });
        var resetSchemaService =
            new ResetSchemaService(new OracleCommandRunner(), new CancellationTokenSource());
        resetSchemaService.Run(config);
        Log.Information("Schema reset in {TotalSeconds}s", stopwatch.Elapsed.TotalSeconds);
    }
}