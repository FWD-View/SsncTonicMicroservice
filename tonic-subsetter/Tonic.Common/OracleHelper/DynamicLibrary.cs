using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Tonic.Common.OracleHelper
{
    /// <summary>
    /// P/Invoke base class for dynamically loadable libraries which eliminates the need for
    /// static 'extern' declarations annotated with <see cref="DllImportAttribute"/>
    /// </summary>
    public abstract class DynamicLibrary : IDisposable
    {
        private readonly IntPtr _handle;
        private bool _disposed;

        protected DynamicLibrary(string libraryName, DllImportSearchPath? searchPath = null)
        {
            if (!NativeLibrary.TryLoad(libraryName,
                Assembly.GetCallingAssembly(),
                searchPath,
                out _handle))
            {
                LastErrorCode = Win32ErrorCodes.LibraryLoadFailed;
                _handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Invoke with variable arguments
        /// </summary>
        /// <remarks>Does not support 'out' parameters</remarks>
        protected TReturn? Invoke<TDelegate, TReturn>(string exportName, params object[] arguments)
            where TDelegate : Delegate
            => Invoke<TDelegate, TReturn>(exportName, ref arguments);

        /// <summary>
        /// Invoke with ref arguments
        /// </summary>
        /// <remarks>
        /// makes it possible to access 'out' parameter values (like C struct pointers)
        /// </remarks>
        protected TReturn? Invoke<TDelegate, TReturn>(string exportName, ref object[] arguments)
            where TDelegate : Delegate
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(DynamicLibrary).FullName);
            }

            if (_handle == IntPtr.Zero)
            {
                return default;
            }

            LastErrorCode = Win32ErrorCodes.Success;

            if (!NativeLibrary.TryGetExport(_handle, exportName, out IntPtr functionExport))
            {
                LastErrorCode = Win32ErrorCodes.ExportNotFound;

                return default;
            }

            Delegate function = Marshal.GetDelegateForFunctionPointer<TDelegate>(functionExport);

            TReturn? result = (TReturn?) function.DynamicInvoke(arguments);

            LastErrorCode = Marshal.GetLastWin32Error();

            return result;
        }

        /// <summary>
        /// result of <see cref="Marshal.GetLastWin32Error"/> (effectively `errno` on Linux)
        /// </summary>
        public long LastErrorCode
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            get;
            private set;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static class Win32ErrorCodes
        {
            public const long Success = 0;

            /// <summary>
            /// ERROR_MOD_NOT_FOUND
            /// </summary>+
            public const long LibraryLoadFailed = 126L;

            /// <summary>
            /// TYPE_E_DLLFUNCTIONNOTFOUND
            /// </summary>
            public const long ExportNotFound = 0x802F;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                NativeLibrary.Free(_handle);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DynamicLibrary()
        {
            Dispose(false);
        }
    }
}