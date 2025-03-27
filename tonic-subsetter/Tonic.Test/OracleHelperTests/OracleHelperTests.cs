using System;
using System.Net.Http;
using System.Threading;
using Serilog;
using Tonic.Common.Configs;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;
using Tonic.Common.OracleHelper.Models;
using Xunit;
using Xunit.Abstractions;

namespace Tonic.Test.OracleHelperTests;

public class OracleHelperTests
{
    private readonly OracleCommandRunner _oracleCommandRunner;

    private static readonly HostConfig _databaseConnectionDetails = new()
    {
        User = "TONIC",
        Password = "password",
        Sid = "ORCLPDB1",
        Host = "host.docker.internal",
        Port = 11522,
        HostCategory = "TEST",
        Schema = "TEST"
    };

    public OracleHelperTests(ITestOutputHelper output)
    {
        _oracleCommandRunner = new OracleCommandRunner();

        var loggerConfiguration = new LoggerConfiguration();
        Log.Logger = loggerConfiguration
            .MinimumLevel
            .Debug()
            .WriteTo.Console()
            .WriteTo.TestOutput(output)
            .CreateLogger();
    }

#pragma warning disable xUnit1004
    //[Fact(Skip = "Requires Tonic Oracle Helper to be running")]
#pragma warning restore xUnit1004
    [Fact]
    public void TestOracleHelperClientConnect()
    {
        Exception exception = null;
        try
        {
            var parameters = new DataPumpImportParameters();
            _oracleCommandRunner.RunCommand(parameters, _databaseConnectionDetails, CancellationToken.None).Wait();
        }
        catch (Exception e)
        {
            if (e.Message.Contains("ORA-31640"))
            {
                // `sqlldr impdp ...`
                exception = e;
            }
            else throw;
        }

        Assert.NotNull(exception);
    }
    
    [Fact]
    public void TestParametersDetectsXmlColumns()
    {
        var columns = new[] { new Column("TEST", "MUST_USE_CONVENTIONAL_PATH", "XML", "XMLTYPE", false) };
        var mustUseConventionalPath = SqlLoaderUtils.MustUseConventionalPath(columns);
        var parameters = SqlLoaderUtils.CreateSubsetterImportParameters("path", "file_name", 4_000, false, mustUseConventionalPath);
        Assert.Null(parameters.Rows);
        Assert.False(parameters.Direct);
    }
}