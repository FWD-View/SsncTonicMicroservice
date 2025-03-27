using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tonic.Common.Configs;
using Tonic.Common.OracleHelper.Models;

namespace Tonic.Common.DB2Helper
{

    public sealed class DB2CommandRunner
        {
            // number of times an ADDRESS list is traversed before the connection attempt is terminated
            private const int RetryCount = 3;

            // Delay in seconds between subsequent retries for a connection
            private const int RetryDelay = 1;

            public async Task RunCommand<T>(T toolParameters,string filename, HostConfig lh, CancellationToken cancellationToken) where T : ParametersBase
            {
                ArgumentNullException.ThrowIfNull(toolParameters);
                ArgumentNullException.ThrowIfNull(lh);

                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();

                var tool = toolParameters.OracleTool;
                await StartCommand(tool, filename, toolParameters, cancellationToken).ConfigureAwait(false);
            }

            private static async Task StartCommand<T>(OracleTool tool,string filename, T toolParameters, CancellationToken cancellationToken)
                where T : ParametersBase
            {
            Log.Logger.Information("Running {CommandType}", tool.ToString());
            var command = new DB2ToolCommand(tool);
            command.Arguments = $"db2 -f {filename}";

                await command.RunWithRetriesAsync(RetryCount, cancellationToken).ConfigureAwait(false);
            }            
        }    
}
