using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Tonic.Common.Enums;

namespace Tonic.Common.OracleHelper.ErrorCodes;

/// <summary>
/// Specifies the database-specific transient error code mapped to a <see cref="TransientExceptionType"/>
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class TransientErrorCodeAttribute : Attribute, ITransientErrorCode
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private const int NoDatabaseType = -1;

    private readonly int _databaseTypeRaw;

    /// <inheritdoc cref="ITransientErrorCode.DatabaseType"/>
    public DatabaseType? DatabaseType => _databaseTypeRaw == NoDatabaseType ? null : (DatabaseType) _databaseTypeRaw;

    /// <inheritdoc cref="ITransientErrorCode.TransienceType"/>
    public TransientExceptionType TransienceType { get; }

    /// <inheritdoc cref="ITransientErrorCode.ErrorCode"/>
    public object ErrorCode { get; }

    /// <inheritdoc cref="ITransientErrorCode.IsDatabaseAgnostic"/>
    public bool IsDatabaseAgnostic => _databaseTypeRaw == NoDatabaseType;

    /// <inheritdoc cref="ITransientErrorCode.SocketError"/>
    SocketError? ITransientErrorCode.SocketError => ErrorCode is SocketError socketError ? socketError : null;

    /// <summary>
    /// Initializes a new instance of <see cref="TransientErrorCodeAttribute"/> for the specified error code (<see cref="NoDatabaseType"/>).
    /// </summary>
    /// <param name="errorCode">The transient error code.</param>
    /// <param name="transientExceptionType">The transience type of the exception.</param>
    public TransientErrorCodeAttribute(object errorCode, TransientExceptionType transientExceptionType)
        : this(NoDatabaseType, errorCode, transientExceptionType)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TransientErrorCodeAttribute"/> for the specified database type and error code.
    /// </summary>
    /// <param name="databaseType">
    /// The integer value of the <see cref="DatabaseType"/> to which <paramref name="errorCode"/> applies or <see cref="NoDatabaseType"/>
    /// </param>
    /// <param name="errorCode">The transient error code.</param>
    /// <param name="transientExceptionType">The transience type of the exception.</param>
    public TransientErrorCodeAttribute(int databaseType, object errorCode, TransientExceptionType transientExceptionType)
    {
        _databaseTypeRaw = databaseType;
        ErrorCode = errorCode;
        TransienceType = transientExceptionType;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TransientErrorCodeAttribute"/> for the specified database type and error code.
    /// </summary>
    /// <param name="databaseType">The <see cref="DatabaseType"/> to which <paramref name="errorCode"/> applies</param>
    /// <param name="errorCode">The transient error code.</param>
    /// <param name="transientExceptionType">The transience type of the exception.</param>
    public TransientErrorCodeAttribute(DatabaseType databaseType, object errorCode, TransientExceptionType transientExceptionType)
        : this((int) databaseType, errorCode, transientExceptionType)
    {
    }

    [IgnoreDataMember]
    [JsonIgnore]
    public string DebuggerDisplay => $"{DatabaseType} {TransienceType} {ErrorCode} {((ITransientErrorCode) this).SocketError}";
}