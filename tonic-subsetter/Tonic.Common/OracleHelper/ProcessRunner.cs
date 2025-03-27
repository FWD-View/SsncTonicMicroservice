using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Unix.Native;
using Serilog;
using Tonic.Common.Enums;
using Tonic.Common.Extensions;
using Tonic.Common.OracleHelper.Extensions;

namespace Tonic.Common.OracleHelper
{
    public sealed class ProcessRunner : IDisposable
    {
        private readonly string _execName;
        private readonly CancellationToken _cancellationToken;
        private readonly Process _process;
        private readonly string? _stdin;
        private readonly StringBuilder _stdError;
        private readonly StringBuilder _stdOut;
        private int? _killResultCode;

        public ProcessRunner(string execName, CancellationToken cancellationToken, IDictionary<string, string>?
            environmentVars = null, IList<string>? argumentList = null, string? arguments = null, string? stdin = null)
        {
            ArgumentNullException.ThrowIfNull(execName);

            _execName = execName;
            _cancellationToken = cancellationToken;

            _stdin = stdin;
            _stdOut = new StringBuilder();
            _stdError = new StringBuilder();

            var pathToExec = ResolveFullPath(execName);

            var startInfo = new ProcessStartInfo
            {
                FileName = pathToExec,
                UseShellExecute = false,
                RedirectStandardInput = !string.IsNullOrEmpty(_stdin),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (null != argumentList)
            {
                argumentList.ForEach(startInfo.ArgumentList.Add);
            }
            else
            {
                startInfo.Arguments = arguments;
            }

            if (environmentVars is not null)
            {
                foreach (var (key, value) in environmentVars)
                {
                    startInfo.EnvironmentVariables[key] = value;
                }
            }

            _process = new Process
            {
                StartInfo = startInfo
            };

            _process.EnableRaisingEvents = true;

            RegisterEvents(true);
        }

        public async Task<(string[] output, string[] errors)> RunAsync()
        {
            if (_process.Start())
            {
                ProcessId = _process.Id;

                StartTime = _process.SafeGet(p => p.StartTime, defaultValue: DateTime.Now);

                // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();

                // If STDIN was provided, write it to the process
                if (!string.IsNullOrEmpty(_stdin))
                    await _process.StandardInput.WriteAsync(_stdin).ConfigureAwait(false);

                try
                {
                    await _process.WaitForExitAsync(_cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (!_process.SafeGet(p => p.HasExited, defaultValue: true))
                    {
                        _killResultCode = _process.Kill(killWaitMilliseconds: 1000);
                        Log.Logger.Information("Sent {Signum} to {ExecName}, returned code {ResultCode}", Signum.SIGTERM, _execName, _killResultCode);
                    }
                    else
                    {
                        Log.Logger.Information("Process {ExecName} exited, returned code {ResultCode}", _execName, ExitCode, default);
                    }
                }
                finally
                {
                    UpdateExitState();
                }
            }
            else
            {
                Log.Logger.Error("Failed to start process {ProcessName}", _execName);
            }

            var isExitCodeSuccess = IsExitCodeSuccessOverride?.Invoke(ExitCode) ?? IsExitCodeSuccess(ExitCode);

            if (!isExitCodeSuccess && _stdError.Length > 0)
            {
                throw new Exception(_stdError.ToString()); //NOSONAR
            }

            return (_stdOut.ToString().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                _stdError.ToString().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        public int? ExitCode
        {
            get;
            private set;
        }

        public bool HasExited
        {
            get
            {
                return _process.SafeGet(p => p.HasExited, defaultValue: true);
            }
        }

        public bool WasKilled
        {
            get
            {
                return _killResultCode.HasValue && _killResultCode.Value == 0;
            }
        }

        public int ProcessId
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            get;
            private set;
        }

        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }

        public TimeSpan? ExecutionTime
        {
            get
            {
                if (StartTime.HasValue)
                {
                    if (EndTime.HasValue)
                    {
                        return EndTime.Value.Subtract(StartTime.Value);
                    }
                    else
                    {
                        return DateTimeOffset.Now.Subtract(StartTime.Value);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Returns true if process exited successfully
        /// </summary>
        /// <remarks>aka EX_SUCC</remarks>
        public static bool IsExitCodeSuccess(int? exitCode) => exitCode == 0;

        /// <summary>
        /// Returns true if process exited NOT successfully
        /// </summary>
        /// <remarks>aka EX_FAIL or EX_FTL</remarks>
        public static bool IsExitCodeFailure(int? exitCode)
        {
            switch (exitCode)
            {
                case 1:
                case 3:
                    return true;
                case 4:
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Returns true if process exited with warnings
        /// </summary>
        /// <remarks>aka EX_WARN</remarks>
        public static bool IsExitCodeWarning(int? exitCode) => exitCode == 2;

        public static bool IsExitCodeSuccessOrWarning(int? exitCode) => IsExitCodeSuccess(exitCode) || IsExitCodeWarning(exitCode);

        private static string ResolveFullPath(string execName)
        {
            var pathToExec = execName;

            TonicEnvironmentVariable pathPrefixVariable = TonicEnvironmentVariable.Unspecified;

            switch (execName.ToLowerInvariant())
            {
                case "impdp":
                case "expdp":
                case "sqlldr":
                    pathPrefixVariable = TonicEnvironmentVariable.ORACLE_HOME;
                    break;
            }

            if (pathPrefixVariable != TonicEnvironmentVariable.Unspecified)
            {
                var pathPrefix = pathPrefixVariable.Get();
                if (string.IsNullOrEmpty(pathPrefix)) throw new Exception($"Must define environmental variable {pathPrefixVariable}"); //NOSONAR

                pathToExec = Path.Combine(pathPrefix, execName);
            }

            return pathToExec;
        }

        internal DataReceivedEventHandler? OutputDataReceived { get; set; }
        internal DataReceivedEventHandler? ErrorDataReceived { get; set; }

        internal ExitCodeCheck? IsExitCodeSuccessOverride { get; set; }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            _stdOut.AppendLine(e.Data);

            OutputDataReceived?.Invoke(sender, e);
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null &&
                !e.Data.Contains("mysql: [Warning]") &&
                !e.Data.Contains("mysqldump: [Warning]") &&
                (!string.Equals(_execName, "psql", StringComparison.InvariantCultureIgnoreCase) ||
                 !e.Data.Contains("DETAIL:")))
            {
                _stdError.AppendLine(e.Data);
            }

            ErrorDataReceived?.Invoke(sender, e);
        }

        private void OnExited(object? sender, EventArgs e)
        {
            UpdateExitState();
        }

        private void UpdateExitState()
        {
            if (!ExitCode.HasValue)
            {
                EndTime = _process.SafeGet(p => p.ExitTime, defaultValue: DateTime.Now);
                ExitCode = _process.SafeGet(p => p.ExitCode, default);

                if (ExecutionTime.HasValue)
                {
                    Log.Logger.Information("Process {ExecName} exited, executed in {ExecutionTime}s, returned code {ResultCode}",
                        _execName,
                        ExecutionTime.Value.TotalSeconds,
                        ExitCode);
                }
                else
                {
                    Log.Logger.Information("Process {ExecName} exited, returned code {ResultCode}",
                        _execName,
                        ExitCode);
                }
            }
        }

        private void RegisterEvents(bool register)
        {
            _process.Exited -= OnExited;
            _process.ErrorDataReceived -= OnErrorDataReceived;
            _process.OutputDataReceived -= OnOutputDataReceived;

            if (register)
            {
                _process.Exited += OnExited;
                _process.ErrorDataReceived += OnErrorDataReceived;
                _process.OutputDataReceived += OnOutputDataReceived;
            }
        }

        private void ReleaseUnmanagedResources()
        {
            if (_process != null)
            {
                RegisterEvents(false);

                UpdateExitState();

                _process.Dispose();
            }

            _stdError?.Clear();
            _stdOut?.Clear();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ProcessRunner()
        {
            ReleaseUnmanagedResources();
        }
    }

    public delegate bool ExitCodeCheck(int? exitCode);
}