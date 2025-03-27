using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Tonic.Common.Enums;
using Tonic.Common.Exceptions;
using Tonic.Common.Models;

namespace Tonic.Common.OracleHelper.ErrorCodes;

public interface ITransientErrorCode : IDebuggerDisplay
{
    /// <summary>
    /// The <see cref="DatabaseType"/> to which the error code applies or `null` if it <see cref="IsDatabaseAgnostic"/>
    /// </summary>
    DatabaseType? DatabaseType { get; }

    /// <summary>
    /// The <see cref="TransientExceptionType"/> of the error.
    /// </summary>
    TransientExceptionType TransienceType { get; }

    /// <summary>
    /// Returns true if the error code is not specific to a <see cref="DatabaseType"/>, else false.
    /// </summary>
    bool IsDatabaseAgnostic { get; }

    /// <summary>
    /// The <see cref="SocketError"/> if the transient error is related to sockets.
    /// </summary>
    SocketError? SocketError { get; }

    /// <summary>
    /// The transient error code or a <see cref="Type"/> enumerating the possible values of the transient error code.
    /// </summary>
    object ErrorCode { get; }
}

public interface ITransientErrorInfo : ITransientErrorCode
{
    IExceptionDispatchInfo ExceptionDispatchInfo { get; }

    SourceInfo SourceOfCapture { get; }

    /// <summary>
    /// the count of exceptions with this error code observed during a retry loop
    /// </summary>
    int OccurrencesDuringRetry { get; }
}

public static class TransientErrorCodeExtensions
{
    public static ITransientErrorInfo ToErrorInfo(
        this ITransientErrorCode transientErrorCode,
        Exception exception,
        SourceInfo sourceOfCapture
        ) => new TransientErrorInfo(transientErrorCode, exception, sourceOfCapture);

    [DebuggerDisplay("{DebuggerDisplay}")]
    private sealed class TransientErrorInfo : ITransientErrorInfo, IDebuggerDisplay
    {
        internal TransientErrorInfo(
            ITransientErrorCode transientErrorCode,
            Exception exception,
            SourceInfo sourceOfCapture)
        {
            DatabaseType = transientErrorCode.DatabaseType;
            TransienceType = transientErrorCode.TransienceType;
            IsDatabaseAgnostic = transientErrorCode.IsDatabaseAgnostic;
            SocketError = transientErrorCode.SocketError;
            ErrorCode = transientErrorCode.ErrorCode;
            ExceptionDispatchInfo = exception.CaptureDispatchInfo();
            SourceOfCapture = sourceOfCapture;
        }

        public DatabaseType? DatabaseType { get; }

        public TransientExceptionType TransienceType { get; }

        public bool IsDatabaseAgnostic { get; }

        public SocketError? SocketError { get; }

        public object ErrorCode { get; }

        public IExceptionDispatchInfo ExceptionDispatchInfo { get; }

        public SourceInfo SourceOfCapture { get; }

        public int OccurrencesDuringRetry =>
            Convert.ToInt32(ExceptionDispatchInfo.SourceException.Data[ExceptionDataKeys.OccurrencesDuringRetry] ?? "1");

        public override string ToString() => Constants.RedactedValue;

        [IgnoreDataMember]
        [JsonIgnore]
        public string DebuggerDisplay => $"{DatabaseType} {TransienceType} {ErrorCode} {SocketError}";
    }
}