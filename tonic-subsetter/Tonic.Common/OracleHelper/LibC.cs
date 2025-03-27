using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mono.Unix;
using Mono.Unix.Native;
using Tonic.Common.OracleHelper.Enums;

namespace Tonic.Common.OracleHelper
{
    /// <summary>
    /// P/Invoke wrapper for C Standard Library exports
    /// </summary>
    public sealed class LibC : DynamicLibrary
    {
        public LibC(DllImportSearchPath? searchPath = null)
            : base("libc", searchPath)
        {
        }

        /// <summary>
        /// system call can be used to send any signal to any process group or process
        /// </summary>
        /// <param name="processId">
        /// If pid is positive, then signal sig is sent to the process with
        /// the ID specified by pid.
        ///
        /// If pid equals 0, then sig is sent to every process in the process
        /// group of the calling process.
        ///
        /// If pid equals -1, then sig is sent to every process for which the
        /// calling process has permission to send signals, except for
        /// process 1 (init), but see below.
        ///
        /// If pid is less than -1, then sig is sent to every process in the
        /// process group whose ID is -pid.
        /// </param>
        /// <param name="signalNumber">
        /// If sig is 0, then no signal is sent, but existence and permission
        /// checks are still performed; this can be used to check for the
        /// existence of a process ID or process group ID that the caller is
        /// permitted to signal.
        /// </param>
        /// <returns>On success (at least one signal was sent), zero is returned.
        /// On error, -1 is returned, and errno is set to indicate the error. (<see cref="DynamicLibrary.LastErrorCode"/>)
        /// </returns>
        /// <remarks>
        /// For a process to have permission to send a signal, it must either
        /// be privileged (under Linux: have the CAP_KILL capability in the
        /// user namespace of the target process), or the real or effective
        /// user ID of the sending process must equal the real or saved set-
        /// user-ID of the target process.  In the case of SIGCONT, it
        /// suffices when the sending and receiving processes belong to the
        /// same session
        /// </remarks>
        public int Kill(int processId, Signum signalNumber) =>
            Invoke<kill, int>(nameof(kill), processId, signalNumber);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
#pragma warning disable CS8981
        private delegate int kill(int pid, int sig);
#pragma warning restore CS8981

        /// <summary>
        /// system call which changes the mode bits of the file specified
        /// </summary>
        /// <param name="pathName">file path (which is dereferenced if it is a symbolic)</param>
        /// <param name="octalModeT">The new file mode represented as an octal string (like the command-line chmod accepts)</param>
        /// <returns>On success, zero is returned.
        /// On error, -1 is returned, and errno is set to indicate the error.
        /// </returns>
        /// <remarks>
        /// The file mode consists of the file permission bits plus the set-user-ID, set-group-ID, and sticky bits.
        /// http://permissions-calculator.org/decode/
        /// </remarks>
        public int Chmod(string pathName, string octalModeT) =>
            Invoke<chmod, int>(nameof(chmod), pathName, Convert.ToUInt32(octalModeT, 8));

        /// <summary>
        /// system call which changes the mode bits of the file specified
        /// </summary>
        /// <param name="pathName">file path (which is dereferenced if it is a symbolic)</param>
        /// <param name="modeT">The new file mode</param>
        /// <returns>On success, zero is returned.
        /// On error, -1 is returned, and errno is set to indicate the error.
        /// </returns>
        /// <remarks>
        /// The file mode consists of the file permission bits plus the set-user-ID, set-group-ID, and sticky bits.
        /// </remarks>
        public int Chmod(string pathName, FileAccessPermissions modeT) =>
            Invoke<chmod, int>(nameof(chmod), pathName, (ModeT) modeT);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
#pragma warning disable CS8981
        private delegate int chmod(string pathname, uint mode);
#pragma warning restore CS8981
    }

    public static class LibCExtensions
    {
        /// <summary>
        /// returns the file mode as an octal string (like the command-line chmod accepts) for the specified file
        /// </summary>
        /// <param name="_"></param>
        /// <param name="filePath">the path of the file whose file mode is to be returned</param>
        /// <returns>the file mode as an octal string (like the command-line chmod accepts) for the specified file</returns>
        /// <remarks>
        /// though technically possible to implement
        ///     `int stat (string file_name, out Stat buf);`
        /// as a <see cref="LibC"/> delegate, the Stat object is complex and marshalling of it to and from C requires a fair amount
        /// of code.  In order to not have to write this code, this calls to the 'stat' executable on the system instead
        /// </remarks>
        public static async Task<string?> StatOctalAsync(this LibC _, string filePath)
        {
            string? result;

            var processRunner = new ProcessRunner("stat",
                CancellationToken.None,
                arguments: RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"-f '%OLp' {filePath}" : $"--format=%a {filePath}");

            using (processRunner)
            {
                var (output, _) = await processRunner.RunAsync().ConfigureAwait(false);
                result = output.FirstOrDefault(x => !string.IsNullOrEmpty(x));
            }

            return result;
        }

        /// <summary>
        /// returns the file mode as a <see cref="FileAccessPermissions"/> for the specified file
        /// </summary>
        /// <param name="_"></param>
        /// <param name="filePath">the path of the file whose file mode is to be returned</param>
        /// <returns>the file mode as a <see cref="FileAccessPermissions"/> for the specified file</returns>
        /// <remarks>
        /// though technically possible to implement
        ///     `int stat (string file_name, out Stat buf);`
        /// as a <see cref="LibC"/> delegate, the Stat object is complex and marshalling of it to and from C requires a fair amount
        /// of code.  In order to not have to write this code, this calls to the 'stat' executable on the system instead
        /// </remarks>
        public static async Task<FileAccessPermissions> StatAsync(this LibC _, string filePath)
            => (FileAccessPermissions) Convert.ToUInt32((await _.StatOctalAsync(filePath).ConfigureAwait(false))?.Trim(new[] { '\'' }), 8);
    }
}