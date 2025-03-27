using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Oracle.ManagedDataAccess.Client;
using Tonic.Common.Enums;
using Tonic.Common.Exceptions;
using Tonic.Common.Extensions;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper.ErrorCodes;

namespace Tonic.Common.OracleHelper.Exceptions;

internal sealed class OracleExceptionAugmentor : ExceptionAugmentorBase
{
    /// <inheritdoc cref="IAugmentException"/>
    public static readonly IAugmentException Instance = new OracleExceptionAugmentor();

    [SuppressMessage("ReSharper", "EmptyConstructor")]
    public OracleExceptionAugmentor()
    {
    }

    protected override TextPosition? GetErrorTextPosition(IDbCommand? dbCommand, Exception ex)
    {
        if (dbCommand != null && ex is OracleException orex && orex.Errors.Count > 0 && orex.Errors[0] is var firstError && firstError != null)
        {
            var desiredOffset = firstError.ParseErrorOffset;

            return TextPosition.ParseOffset(dbCommand.CommandText, desiredOffset);
        }

        return null;
    }

    public override bool IsTimeoutException(Exception ex)
    {
        if (ex is OracleException oracleException)
        {
            return oracleException.IsTimeout();
        }

        return ex.IsGenericTimeout();
    }

    public override bool IsTransientException(Exception ex, [MaybeNullWhen(false)] out ITransientErrorCode errorCode)
    {
        errorCode = null;

        if (ex is OracleException oracleException &&
            oracleException.IsTransient(out errorCode))
        {
            return errorCode != null;
        }

        return ex.IsGenericTransientErrorCode(out errorCode);
    }
}

public static class OracleExceptionExtensions
{
    /// <summary>
    /// <see cref="OracleException"/> DOES NOT implement <see cref="DbException.IsTransient"/>.
    /// This method will return `true` if there is a transient value mapped in <see cref="OracleErrorCodes"/> OR
    /// if <see cref="ExceptionExtensions.IsGenericTransientErrorCode"/> returns `true`, otherwise this returns `false`.
    /// </summary>
    /// <remarks>
    /// The default implementation of <see cref="DbException.IsTransient"/> returns `false` and so it is a 'banned API'.
    /// The property <see cref="DbException.IsTransient"/> is improperly implemented by multiple types derived from <see cref="DbException"/>.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs")] //this method is allowed to use `DbException.IsTransient`
    private const byte IsTransientDocumentation = 0;

    /// <inheritdoc cref="IsTransientDocumentation"/>
    [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs")] //this method is allowed to use `DbException.IsTransient`
    public static bool IsTransient(this OracleException oracleException, [MaybeNullWhen(false)] out ITransientErrorCode errorCode)
    {
        errorCode = null;

        //`OracleException` DOES NOT implement `IsTransient` (which always returns `false`)
        errorCode = TransientExceptionTypes.FromErrorCode(DatabaseType.Oracle, oracleException.Number);

        if (errorCode == null)
        {
            //in case `IsTransient` gets implemented correctly at some point
            //AND a particular transient exception is not mapped in `TransientExceptionType`
            if (oracleException.IsTransient)
            {
                errorCode = TransientErrorCodes.CreateUnmappedErrorCode(DatabaseType.Oracle, oracleException.Number);
            }
            else
            {
                oracleException.IsGenericTransientErrorCode(out errorCode);
            }
        }

        return errorCode != null;
    }

    /// <inheritdoc cref="IsTransientDocumentation"/>
    [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs")] //this method is allowed to use `DbException.IsTransient`
    public static bool IsTransient(this OracleException oracleException) => IsTransient(oracleException, out _);

    /// <summary>
    /// This method will return `true` if there is a timeout value mapped in <see cref="OracleErrorCodes"/> OR
    /// if <see cref="ExceptionExtensions.IsGenericTimeout"/> returns `true`, otherwise this returns `false`.
    /// </summary>
    public static bool IsTimeout(this OracleException oracleException)
    {
        if (oracleException.Number.EqualsAny(
                OracleErrorCodes.Transient.Timeout.Connection,
                OracleErrorCodes.Transient.Timeout.Receive,
                OracleErrorCodes.Transient.Timeout.Send,
                OracleErrorCodes.Transient.Timeout.ClientRequest,
                OracleErrorCodes.Transient.Timeout.InboundConnection,
                OracleErrorCodes.UserRequestedCancel
            ))
        {
            return true;
        }

        return oracleException.IsGenericTimeout();
    }
}