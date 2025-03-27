using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Tonic.Common.Configs;
using Tonic.Common.Enums;
using Tonic.Common.OracleHelper.Extensions;
using Tonic.Common.OracleHelper.Models;

namespace Tonic.Common.OracleHelper
{
    public sealed class OracleCommandRunner : IOracleCommandRunner
    {
        // number of times an ADDRESS list is traversed before the connection attempt is terminated
        private const int RetryCount = 3;

        // Delay in seconds between subsequent retries for a connection
        private const int RetryDelay = 1;

        public async Task RunCommand<T>(T toolParameters, HostConfig lh, CancellationToken cancellationToken) where T : ParametersBase
        {
            ArgumentNullException.ThrowIfNull(toolParameters);
            ArgumentNullException.ThrowIfNull(lh);

            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            var tool = toolParameters.OracleTool;

            switch (toolParameters)
            {
                case DataPumpParameters dataPumpParameters when !dataPumpParameters.Validate(out var validationErrors):
                    throw new ArgumentException(string.Join(Environment.NewLine, validationErrors));
                case DataPumpParameters dataPumpParameters:
                    dataPumpParameters.UserId = GetOracleConnectionString(lh);
                    break;
                case SqlLoaderParameters sqlLoaderParameters:
                    sqlLoaderParameters.UserId = GetOracleConnectionString(lh);
                    break;
            }

            await StartCommand(tool, toolParameters, cancellationToken).ConfigureAwait(false);
        }

        private static async Task StartCommand<T>(OracleTool tool, T toolParameters, CancellationToken cancellationToken)
            where T : ParametersBase
        {
            Log.Logger.Information("Running {CommandType}", tool.ToString());

            //careful with the credentials here:
            var parametersFileWithPlainTextCredentials = ParFileSerializer.SerializeLines(toolParameters);
            //\

            //scrubbed version
            var anonymizedToolParameters = toolParameters.WithCredentialsRemoved();
            var parametersFileCleanedForLogs = ParFileSerializer.SerializeLines(anonymizedToolParameters);
            Log.Information("{@ParametersFile}", parametersFileCleanedForLogs); //[JsonIgnore] properties are also omitted via '@'
            //\

            var command = new OracleToolCommand(tool, parametersFileWithPlainTextCredentials);

            await command.RunWithRetriesAsync(RetryCount, cancellationToken).ConfigureAwait(false);
        }

        public string GetOracleConnectionString(HostConfig lh)
        {
            var username = lh.User.Replace("'", "''", StringComparison.Ordinal);
            var password = lh.Password.Replace("'", "''", StringComparison.Ordinal);
            var database = lh.Sid.Replace("'", "''", StringComparison.Ordinal);

            var connectionDetails = new Dictionary<string, string>
            {
                { "retry_count", RetryCount.ToString(CultureInfo.InvariantCulture) },
                { "retry_delay", RetryDelay.ToString(CultureInfo.InvariantCulture) }
            };

            var sduSize = TonicEnvironmentVariable.TONIC_ORACLE_SDU_SIZE.Get<int>();
            if (sduSize >= 512) connectionDetails.Add("sdu", sduSize.ToString(CultureInfo.InvariantCulture));

            var connectionDetailsString = string.Join('&', connectionDetails.Select(pair => $"{pair.Key}={pair.Value}"));

            return $"'{username}/\"{password}\"@//{lh.Host}:{lh.Port}/\"{database}\"?{connectionDetailsString}'";
        }
    }
}