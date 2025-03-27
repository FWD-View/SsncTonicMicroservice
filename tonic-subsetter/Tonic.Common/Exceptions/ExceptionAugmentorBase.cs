using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper.ErrorCodes;

namespace Tonic.Common.Exceptions;

/// <inheritdoc cref="IAugmentException"/>
public abstract class ExceptionAugmentorBase : IAugmentException
{
    public virtual void AugmentException(IDbCommand? dbCommand, Exception ex)
    {
        AugmentExceptionUsingDbCommand(dbCommand, ex);
        AugmentException(ex);
    }

    public virtual void AugmentException(Exception ex)
    {
        AugmentExceptionCommon(ex);
    }

    /// <summary>
    /// Returns a <see cref="TextPosition"/> for a specific exception thrown during execution of a database command.
    /// </summary>
    /// <remarks>Implementations are DB-specific.</remarks>
    protected virtual TextPosition? GetErrorTextPosition(IDbCommand? dbCommand, Exception ex) => null;

    /// <summary>
    /// Handler called in exception context with <see cref="IDbCommand"/>, for every database type.
    /// Implementations may be shared across DB types or overridden per DB type.
    /// </summary>
    private void AugmentExceptionUsingDbCommand(IDbCommand? dbCommand, Exception ex)
    {
        AugmentExceptionWithTextPosition(dbCommand, ex);
    }

    /// <summary>
    /// Handler called in any exception context, for every database type.
    /// Implementations may be shared across DB types or overridden per DB type.
    /// </summary>
    private void AugmentExceptionCommon(Exception ex)
    {
        AugmentExceptionWithTimeout(ex);
        AugmentExceptionAsTransient(ex);
    }

    /// <summary>
    /// Adds a <see cref="TextPosition"/> data key for an exception, if applicable.
    /// </summary>
    /// <remarks>Defers to derived implementation of <see cref="GetErrorTextPosition"/> if available.</remarks>
    private void AugmentExceptionWithTextPosition(IDbCommand? dbCommand, Exception ex)
    {
        if (!ex.Data.Contains(ExceptionDataKeys.ErrorTextPosition))
        {
            var textPosition = GetErrorTextPosition(dbCommand, ex);
            if (textPosition != null)
            {
                ex.Data[ExceptionDataKeys.ErrorTextPosition] = textPosition;
            }
        }
    }

    /// <summary>
    /// Adds a timeout data key flag for an exception, if applicable.
    /// </summary>
    /// <remarks>Defers to derived implementation of <see cref="ExceptionAugmentorBase.IsTimeoutException"/> if available.</remarks>
    private void AugmentExceptionWithTimeout(Exception ex)
    {
        if (!ex.Data.Contains(ExceptionDataKeys.CommandTimeout) &&
            IsTimeoutException(ex))
        {
            ex.Data[ExceptionDataKeys.CommandTimeout] = true;
        }
    }

    /// <summary>
    /// Adds a transient data key flag for an exception, if applicable.
    /// </summary>
    /// <remarks>Defers to derived implementation of <see cref="ExceptionAugmentorBase.IsTimeoutException"/> if available.</remarks>
    private void AugmentExceptionAsTransient(Exception ex)
    {
        if (!ex.Data.Contains(ExceptionDataKeys.TransientErrorInfo) && //skip re-processing an exception already marked as transient
            IsTransientException(ex, out var transientErrorCode))
        {
            var sourceOfCapture = (SourceInfo?) ex.Data[ExceptionDataKeys.SourceOfCapture] ?? SourceInfo.FromHere();
            var transientErrorInfo = transientErrorCode.ToErrorInfo(ex, sourceOfCapture);
            ex.Data[ExceptionDataKeys.TransientErrorInfo] = transientErrorInfo;
        }
    }

    /// <summary>
    /// Returns whether an exception is due to a timeout, regardless of source (.NET / DB).
    /// </summary>
    /// <remarks>
    /// DB-specific implementations should be defined in their respective derived class,
    /// but should almost always call the base method as well.
    /// </remarks>
    public abstract bool IsTimeoutException(Exception ex);

    /// <summary>
    /// Returns whether an exception is transient, regardless of source (.NET / DB).
    /// </summary>
    /// <remarks>
    /// DB-specific implementations should be defined in their respective derived class,
    /// but should almost always call the base method as well.
    /// </remarks>
    public abstract bool IsTransientException(Exception ex, [MaybeNullWhen(false)] out ITransientErrorCode errorCode);
}