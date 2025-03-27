using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Tonic.Common.OracleHelper.ErrorCodes;

namespace Tonic.Common.Exceptions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Checks if the exception or any of its internal exceptions are of type TException
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindExceptionOfType<TException>(this Exception exception, [MaybeNullWhen(false)] out TException foundException)
            where TException : Exception
        {
            foundException = EnumerateExceptions<TException>(
                exception
                ).FirstOrDefault();

            return foundException != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Exception> EnumerateExceptions(this Exception exception, Func<Exception, bool>? includeFilter)
            => EnumerateExceptions<Exception>(exception, includeFilter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Exception> EnumerateExceptions(this Exception exception)
            => EnumerateExceptions<Exception>(exception, _ => true); //supply filter to support AggregateException

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TException> EnumerateExceptions<TException>(this Exception exception)
            where TException : Exception
            => EnumerateExceptions<TException>(exception, _ => true); //supply filter to support TException == Exception

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TException> EnumerateExceptions<TException>(this Exception exception, Func<TException, bool>? includeFilter)
            where TException : Exception
        {
            ISet<Exception> visited = new HashSet<Exception>();

            return RecursivelyEnumerateExceptions(visited, exception, includeFilter);
        }

        private static IEnumerable<TException> RecursivelyEnumerateExceptions<TException>(ISet<Exception> visited, Exception exception, Func<TException, bool>? includeFilter)
            where TException : Exception
        {
            if (!visited.Contains(exception))
            {
                visited.Add(exception);

                if (exception is AggregateException aggregateException)
                {
                    if (aggregateException is TException tAggregateException &&
                       includeFilter != null &&
                       includeFilter(tAggregateException))
                    {
                        //special case when filter is requesting types derived from AggregateException
                        yield return tAggregateException;
                    }

                    foreach (var aggInnerException in aggregateException.InnerExceptions)
                    {
                        foreach (var enumException in RecursivelyEnumerateExceptions(visited, aggInnerException, includeFilter))
                        {
                            if (includeFilter == null || includeFilter(enumException))
                            {
                                yield return enumException;
                            }
                        }
                    }
                }
                else
                {
                    if (exception is TException tException &&
                        (includeFilter == null || includeFilter(tException)))
                    {
                        yield return tException;
                    }

                    if (exception.InnerException is var innerException && innerException != null)
                    {
                        foreach (var tEnumException in RecursivelyEnumerateExceptions(visited, innerException, includeFilter))
                        {
                            if (includeFilter == null || includeFilter(tEnumException))
                            {
                                yield return tEnumException;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Returns whether any exception in the exception hierarchy has been augmented with one of the <see cref="ExceptionDataKeys"/> with value `true`
        /// </summary>
        private static bool IsDataKeyFlagged(this Exception exception, string exceptionDataKey) =>
            //walk the exception tree and check each `Data` dictionary for the flag, remove the entry if key is on obfuscate list
            EnumerateExceptions(exception, ex =>
                exception.TryGetDataValueTopLevel(exceptionDataKey, out var exceptionDataItem) &&
                true.Equals(exceptionDataItem.DataItem)
                ).Any();

        /// <summary>
        /// Returns whether a single exception has been augmented with one of the <see cref="ExceptionDataKeys"/> and provides the value for that key
        /// </summary>
        private static bool TryGetDataValueTopLevel(this Exception exception, string exceptionDataKey, [MaybeNullWhen(false)] out ExceptionDataItem exceptionDataItem)
        {
            if (exception.Data.Contains(exceptionDataKey) &&
                exception.Data[exceptionDataKey] is var dataPoint)
            {
                exceptionDataItem = new ExceptionDataItem(exceptionDataKey, dataPoint);
                return true;
            }

            exceptionDataItem = null;
            return false;
        }

        public static bool IsGenericTransientErrorCode(this Exception ex, [MaybeNullWhen(false)] out ITransientErrorCode errorCode)
        {
            errorCode = null;

            //in order of preferred transient type, look for:
            if (errorCode == null && ex.TryFindExceptionOfType<SocketException>(out var socketException))
            {
                errorCode = TransientErrorCodes.CreateSocketErrorCode(null, socketException.SocketErrorCode);
            }

            if (errorCode == null && ex.TryFindExceptionOfType<TimeoutException>(out var timeoutException))
            {
                errorCode = TransientExceptionTypes.FromErrorCode(null, timeoutException.HResult);
            }

            if (errorCode == null && ex.TryFindExceptionOfType<IOException>(out var ioException))
            {
                errorCode = TransientExceptionTypes.FromErrorCode(null, ioException.HResult);
            }

            return errorCode != null;
        }

        public static bool IsGenericTimeout(this Exception ex) =>
            ex is TimeoutException or SocketException { SocketErrorCode: SocketError.TimedOut };
    }

    public sealed class ExceptionDataItem
    {
        public object Key { get; }
        public object? DataItem { get; }

        public ExceptionDataItem(object key, object? dataItem)
        {
            Key = key;
            DataItem = dataItem;
        }

        public override string ToString() => Constants.RedactedValue;
    }
}