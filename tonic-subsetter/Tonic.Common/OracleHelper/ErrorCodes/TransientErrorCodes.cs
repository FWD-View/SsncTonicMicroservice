using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Serilog;
using Tonic.Common.Enums;

namespace Tonic.Common.OracleHelper.ErrorCodes;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class TransientErrorCodes
{
    [Description("Error Code Not Mapped")]
    public const int Unmapped = int.MinValue; //must be less than zero, but also less than valid negative error codes

    /// <summary>
    /// S_OK - Operation successful
    /// </summary>
    [Description("Not Specified")]
    public const int None = 0;

    /// <inheritdoc cref="System.TimeoutException"/>
    public const int COR_E_TIMEOUT = unchecked((int) 0x80131505); //-2146233083

    // <inheritdoc cref="System.IO.IOException"/>
    public const int COR_E_IO = unchecked((int) 0x80131620); //-2146232800

    /// <summary>
    /// E_FAIL - Unspecified failure
    /// </summary>
    /// <remarks>
    /// This HRESULT, with value -2147467259, is returned by the <see cref="System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(int)"/> method
    /// for an exception with no error code set, when there is a transient failure in an ODBC driver,
    /// or when files or configuration are inaccessible.
    /// </remarks>
    public const int E_FAIL = unchecked((int) 0x80004005); //-2147467259

    [UsedImplicitly]
    public static readonly Type SocketErrorType = typeof(SocketError);
    public static ITransientErrorCode CreateSocketErrorCode(DatabaseType? databaseType, SocketError socketError) =>
        new TransientErrorCode(databaseType, TransientExceptionType.SocketException, socketError);

    public static ITransientErrorCode CreateUnmappedErrorCode(DatabaseType databaseType, object unmappedErrorCode)
    {
        if (!E_FAIL.Equals(unmappedErrorCode)) //this error code is not necessarily transient and thus should not be mapped or warned about
        {
            Log.Warning("{Problem} {Type} {Value}",
                nameof(TransientExceptionType.Unmapped),
                nameof(TransientExceptionType),
                unmappedErrorCode
            );
        }
        return new TransientErrorCode(databaseType, TransientExceptionType.Unmapped, unmappedErrorCode);
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    private sealed record TransientErrorCode(DatabaseType? DatabaseType, TransientExceptionType TransienceType, object ErrorCode) : ITransientErrorCode
    {
        public bool IsDatabaseAgnostic => DatabaseType == null;

        public SocketError? SocketError => ErrorCode is SocketError socketError ? socketError : null;

        [JsonIgnore]
        public string DebuggerDisplay => $"{DatabaseType} {TransienceType} {ErrorCode} {SocketError}";
    }

    private static readonly Lazy<IReadOnlyList<TransientExceptionTypeErrorCodes>> _allTransientErrorCodes
        = new(() =>
        {
            var errorCodeList = new List<TransientExceptionTypeErrorCodes>();
            var typeOfTransientExceptionType = typeof(TransientExceptionType);
            foreach (var enumValue in Enum.GetValues<TransientExceptionType>())
            {
                var field = typeOfTransientExceptionType.GetField(enumValue.ToString());

                if (field != null)
                {
                    var transientErrorCodes = field.GetCustomAttributes(typeof(TransientErrorCodeAttribute), false)
                        .Cast<ITransientErrorCode>()
                        .ToArray();

                    errorCodeList.Add(new TransientExceptionTypeErrorCodes(field, enumValue, transientErrorCodes));
                }
            }

            return errorCodeList;
        });

    internal static IReadOnlyList<TransientExceptionTypeErrorCodes> AllCodes => _allTransientErrorCodes.Value;
}


internal sealed record TransientExceptionTypeErrorCodes(FieldInfo Field, TransientExceptionType TransienceType, ITransientErrorCode[] ErrorCodes);