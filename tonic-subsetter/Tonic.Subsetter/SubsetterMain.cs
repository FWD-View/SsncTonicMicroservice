using System;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog;
using Tonic.Common.Utils;

namespace Tonic.Subsetter;

public static class SubsetterMain
{
    private static void ConfigureLogging(bool debug)
    {
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration =
            debug ? loggerConfiguration.MinimumLevel.Debug() : loggerConfiguration.MinimumLevel.Information();
        Log.Logger = loggerConfiguration
            .WriteTo.Console()
            .WriteTo.File("/tmp/subsetter_cli.log", rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }

    public static void Main(string[] args)
    {
        ThreadPool.SetMinThreads(50, 50);
        var files = Utilities.ExtractFilesFromArgs(args).Select(f => new FileStream(f, FileMode.Open));

        var config = (args.Length == 0 ? Console.OpenStandardInput()  : new ConcatenatedStream(files)).ParseSubsetConfig();
        ConfigureLogging(config.Debug);
        SubsetterService.Run(config);
    }
}