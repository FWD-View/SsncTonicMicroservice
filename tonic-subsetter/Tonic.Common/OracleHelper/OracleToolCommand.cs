using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Serilog;
using Tonic.Common.Enums;
using Tonic.Common.Exceptions;
using Tonic.Common.Extensions;
using Tonic.Common.Helpers;
using Tonic.Common.JsonConverters;
using Tonic.Common.OracleHelper.ErrorCodes;
using Tonic.Common.OracleHelper.Exceptions;
using Tonic.Common.OracleHelper.Models;

namespace Tonic.Common.OracleHelper
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class OracleToolCommand
    {
        /// <summary>
        /// The export or import job completed successfully but there were errors encountered during the job.
        /// The errors are displayed to the output device and recorded in the log file, if there is one.
        /// </summary>
        /// <remarks>
        /// aka EX_SUCC_ERR
        /// https://docs.oracle.com/database/121/SUTIL/GUID-34D0DEE7-3530-42DC-BE01-C2588CC73CE5.htm#SUTIL3834
        /// </remarks>
        public const int SUCCESS_WITH_ERRORS_EXIT_CODE = 5;

        internal FileInfo? TempFile { get; set; }


        public OracleToolCommand(OracleTool tool, string[] parametersFile)
        {
            Id = Guid.NewGuid();
            ExitCode = null;
            Tool = tool;

            var parFilePath = CreateParTempFile(parametersFile);

            Arguments = $"PARFILE='{parFilePath}'";
        }

        public OracleTool Tool { get; set; }

        private Guid Id { get; set; }

        [JsonConverter(typeof(EncryptionConverter))]
        public string Arguments { get; set; }

        public int? ExitCode { get; set; }
        public bool HasExited { get; set; }

        [JsonIgnore]
        private string DebuggerDisplay
        {
            get
            {
                if (HasExited)
                {
                    string exitMessage = nameof(HasExited);

                    if (ExitCode.HasValue)
                    {
                        exitMessage = ExitCode.Value.ToString(CultureInfo.InvariantCulture);
                    }

                    return $"{Tool} [{exitMessage}]";
                }

                return $"{Tool} {Arguments}";
            }
        }

        private string CreateParTempFile(string[] parametersFile)
        {
            var tempDirectoryPath = Utility.GetSharedTempPath();

            string parFilePath = Path.Combine(tempDirectoryPath, $"{Id}.par").Replace('\\', '/');

            File.WriteAllLines(parFilePath, parametersFile);

            TempFile = new FileInfo(parFilePath);

            return parFilePath;
        }
    }

    public static partial class OracleToolCommandExtensions
    {
        private const int _FileSystemErrorCode = -1073741515;
        private static readonly Regex _startingLineRegex = new(pattern: "Starting (\".*?\"\\.\".*?\"):.*? PARFILE=(.*)",
            options: RegexOptions.Compiled);
        private static readonly Regex  _sqlLoaderErrorRegex = new(pattern: ".*(SQL\\*Loader-\\d*:).*",
            options: RegexOptions.Compiled);

        private static readonly ImmutableHashSet<string> _allowedCommands = Enum.GetValues<OracleTool>()
            .Select(tool => tool.GetAttribute<EnumMemberAttribute>()?.Value)
            .Where(command => command != null)
            .ToImmutableHashSet()!;

        private static void ThrowIfInvalidTool([NotNull] string? tool)
        {
            ArgumentNullException.ThrowIfNull(tool);
            if (!_allowedCommands.Contains(tool))
            {
                throw new ArgumentOutOfRangeException(nameof(tool),
                    tool,
                    $"Invalid Oracle tool. Accepted values are: [{string.Join(", ", _allowedCommands)}]");
            }
        }

        public static async Task RunWithRetriesAsync(this OracleToolCommand command, int totalTryCount, CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < totalTryCount; i++)
                {
                    try
                    {
                        await command.RunAsync(cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception ex) when (ex is OracleToolTransientException && i < totalTryCount - 1)
                    {
                        Log.Warning(ex,
                            "Failed to run Oracle command {Command}, retrying {Retry} more time(s) before failing",
                            command.Tool.ToString(),
                            totalTryCount - i - 1);
                    }
                    catch (Exception ex) when (OracleExceptionAugmentor.Instance.AugmentAndFilterError(ex, AugmentAction.EnterCatch))
                    {
                        Log.Error(ex, "Failed to run Oracle command {Command}", command.Tool.ToString());
                        throw;
                    }
                }
            }
            finally
            {
                // Since this clears out the parameter file used to run the command, clear it out after all of the retries are done
                command.ClearRuntimeState();
            }
        }

        private static async Task RunAsync(this OracleToolCommand command, CancellationToken cancellationToken)
        {
            await Task.Yield();

            var processCancelSignal = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var execName = command.Tool.GetAttribute<EnumMemberAttribute>()?.Value;
            ThrowIfInvalidTool(execName);

            var environmentVars = new Dictionary<string, string> { { "NLS_LANG", TonicEnvironmentVariable.TONIC_ORACLE_CHARSET.Get<string>() } };
            using var processRunner =
                new ProcessRunner(execName, processCancelSignal.Token, environmentVars: environmentVars, arguments: command.Arguments);
            var errors = new List<string>();
            processRunner.IsExitCodeSuccessOverride = exitCode => ProcessRunner.IsExitCodeSuccessOrWarning(exitCode) ||
                                                                  exitCode == OracleToolCommand.SUCCESS_WITH_ERRORS_EXIT_CODE;
            processRunner.OutputDataReceived = (_, e) =>
            {
                var line = e.Data;
                if (line == null)
                {
                    return;
                }

                line = MaskHostnameInLine(line);

                Log.Logger.Information("OracleToolCommand - OutputDataReceived from {Tool}: {Data}", command.Tool, line);
            };
            processRunner.ErrorDataReceived = (_, e) =>
            {
                var line = e.Data;
                if (line == null)
                {
                    return;
                }

                line = MaskHostnameInLine(line);

                Log.Logger.Warning("OracleToolCommand - ErrorDataReceived from {Command}: {Data}", command.Tool, line);
                if (command.Tool == OracleTool.SqlLoader && _sqlLoaderErrorRegex.IsMatch(line) ||
                    line.Contains($"{OracleErrorCodes.InvalidUsernameOrPassword}:", StringComparison.InvariantCulture) ||
                    line.Contains($"{OracleErrorCodes.JobResumableWait}:", StringComparison.InvariantCulture) ||
                    line.Contains("UDI-", StringComparison.InvariantCulture))
                    processCancelSignal.Cancel();

                errors.Add(line);
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                try
                {
                    await processRunner.RunAsync().ConfigureAwait(false);
                }
                catch (Win32Exception wex)
                {
                    //typically this means 'tool not found',
                    //but Windows isn't directly supported for customers anyway
                    Log.Logger.Warning(wex, "{Command}: {Data}", command.Tool, wex.Message);
                }
            else
            {                
                await processRunner.RunAsync().ConfigureAwait(false);
            }

            command.HasExited = true;
            var exitCode = processRunner.ExitCode;
            command.ExitCode = exitCode;

            Log.Logger.Information("{Tool} exited with {ExitCode}", command.Tool, exitCode);

            if (ProcessRunner.IsExitCodeSuccessOrWarning(exitCode) 
                || exitCode == OracleToolCommand.SUCCESS_WITH_ERRORS_EXIT_CODE) return;

            if (exitCode == _FileSystemErrorCode)
            {
                var sb = new StringBuilder();

                sb.AppendLine("The SQL*Loader utility failed to execute and returned OS exit code -1073741515");
                sb.AppendLine("'sqlldr' may not be installed or is having trouble creating a log file due to permissions");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    sb.AppendLine("Ensure you have Microsoft Visual C++ Redistributable packages 2017+ installed.");
                }

                throw new Exception($"Failure to run command {command.Tool}. {sb}");
            }

            // If the tool exits without any errors & exit code 1, it's potentially a transient error, we should differentiate it from a real failure
            if (exitCode == 1 && errors.Count == 0)
            {
                throw new Exception($"Failure to run command, raising transient exception from {command.Tool} (Exit code {exitCode}).");
            }

            throw new Exception($"Failure to run command {command.Tool} (Exit code {exitCode}). {string.Join(". ", errors)}");
        }

        private static string MaskHostnameInLine(string line)
        {
            // This line contains the Oracle host, so we need to mask it
            if (_startingLineRegex.IsMatch(line))
            {
                line = _startingLineRegex.Replace(line, "Starting $1 PARFILE=$2");
            }

            return line;
        }

        /// <summary>
        /// Clears state that is used only while the command is actually running and not afterward
        /// </summary>
        private static void ClearRuntimeState(this OracleToolCommand toolCommand)
        {
            try
            {
                toolCommand.TempFile?.Delete();
            }
            catch (IOException)
            {
                //ignore failed attempts to delete the file
            }
            finally
            {
                toolCommand.TempFile = null;
            }
        }
    }
}