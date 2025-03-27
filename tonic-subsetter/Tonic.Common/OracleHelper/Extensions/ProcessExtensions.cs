using System;
using System.ComponentModel;
using System.Diagnostics;
using Mono.Unix.Native;

namespace Tonic.Common.OracleHelper.Extensions
{
    public static class ProcessExtensions
    {
        public static int Kill(this Process process, int killWaitMilliseconds = 1000)
        {
            // graceful shutdown
            using (LibC libC = new LibC())
            {
                libC.Kill(process.Id, Signum.SIGINT);
            }

            bool exited = process.WaitForExit(killWaitMilliseconds);

            if (!exited)
            {
                // forceful shutdown
                process.Kill();
            }

            process.WaitForExit(killWaitMilliseconds);

            var kill = process.SafeGet(p => p.HasExited, defaultValue: true) ? 0 : -1;
            return kill;
        }

        /// <summary>
        /// Try to read a property from a <see cref="Process"/> or return defaultValue
        /// if the process has already exited or been collected or both
        /// </summary>
        public static T SafeGet<T>(this Process process, Func<Process, T> getter, T defaultValue)
        {
            try
            {
                return getter.Invoke(process) ?? defaultValue;
            }
            catch (InvalidOperationException)
            {
                //thrown when 'no process associated with this object'
                //either because it was never started properly or
                //it already terminated and/or was collected
            }
            catch (Win32Exception e) when (e.Message.Contains("Unable to retrieve the specified information about the process or thread",
                                               StringComparison.InvariantCultureIgnoreCase) &&
                                           e.Message.Contains("It may have exited or may be privileged",
                                               StringComparison.InvariantCultureIgnoreCase))
            {
                //thrown when the process has ended before this method was called
            }

            return defaultValue;
        }
    }
}