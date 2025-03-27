using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper.ErrorCodes;

namespace Tonic.Common.Exceptions;

/// <summary>
/// Abstraction which handles database-specific error codes and exceptions by
/// augmenting <see cref="Exception.Data"/> with contextual information keyed by <see cref="ExceptionDataKeys"/>
/// </summary>
public interface IAugmentException
{
    /// <summary>
    /// Augment <see cref="Exception.Data"/> with contextual information keyed by <see cref="ExceptionDataKeys"/>
    /// </summary>
    /// <remarks>
    /// Providing the <see cref="IDbCommand"/> will allow augmentation to include <see cref="TextPosition"/> if applicable
    /// </remarks>
    void AugmentException(IDbCommand? dbCommand, Exception ex);

    /// <summary>
    /// Augment <see cref="Exception.Data"/> with contextual information keyed by <see cref="ExceptionDataKeys"/>
    /// </summary>
    void AugmentException(Exception ex);

    /// <summary>
    /// Returns whether an exception is due to a timeout, regardless of source (.NET / DB).
    /// </summary>
    /// <remarks>
    /// DB-specific implementations should be defined in their respective derived class,
    /// but should almost always call the base method as well.
    /// </remarks>
    bool IsTimeoutException(Exception ex);

    /// <summary>
    /// Returns whether an exception is transient, regardless of source (.NET / DB).
    /// </summary>
    /// <remarks>
    /// DB-specific implementations should be defined in their respective derived class,
    /// but should almost always call the base method as well.
    /// </remarks>
    bool IsTransientException(Exception ex, [MaybeNullWhen(false)] out ITransientErrorCode errorCode);
}

/// <summary>
/// Extensions for handling DB driver-specific errors and augmenting them with additional data keys.
/// Handles the various contexts in which we may catch DB errors.
/// </summary>
/// <example>
/// e.g. instantiating connections -- ITonicDbConnection.Open()
/// <br/>
/// e.g. reading from the source database -- IDatabaseReader.QueryTable()
/// </example>
/// <remarks>
/// While Reader/Writer repositories should pass in IDatabaseReader/IDatabaseWriter as the ctor parameter
/// which is ultimately assigned to IAugmentException, the context `this` in which AugmentErrorSkipCatchResumeThrow
/// is called refers to the IReaderRepository/IWriterRepository
/// </remarks>
public static class ExceptionAugmentorExtensions
{
    /// <summary>
    /// Augments a <see cref="Exception.Data"/> with classifications about the exception and performs the specified <see cref="AugmentAction"/>
    /// </summary>
    /// <returns>returns `true` when <see cref="AugmentAction.EnterCatch"/> is specified, otherwise returns `false`</returns>
    /// <example>
    /// Transient exception information is stored as <see cref="ITransientErrorInfo"/> and analytics are queued about the <see cref="TransientExceptionType"/>
    /// </example>
    public static bool AugmentAndFilterError(
        this IAugmentException augmentException,
        Exception ex,
        AugmentAction throwOrCatch,
        Action? logAction = null,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        var sourceOfCapture = SourceInfo.FromCallerAttributes(filePath, lineNumber, memberName);
        return AugmentExceptionCommon(augmentException, NoOpDbCommand.Instance, ex, sourceOfCapture, throwOrCatch, logAction);
    }

    /// <summary>
    /// Augments a <see cref="Exception.Data"/> with classifications about the exception and performs the specified <see cref="AugmentAction"/>
    /// </summary>
    /// <returns>returns `true` when <see cref="AugmentAction.EnterCatch"/> is specified, otherwise returns `false`</returns>
    /// <example>
    /// Transient exception information is stored as <see cref="ITransientErrorInfo"/> and analytics are queued about the <see cref="TransientExceptionType"/>
    /// </example>
    public static bool AugmentAndFilterError(
        this IAugmentException augmentException,
        IDbCommand? dbCommand,
        Exception ex,
        AugmentAction throwOrCatch,
        Action? logAction = null,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        var sourceOfCapture = SourceInfo.FromCallerAttributes(filePath, lineNumber, memberName);
        return AugmentExceptionCommon(augmentException, dbCommand, ex, sourceOfCapture, throwOrCatch, logAction);
    }

    /// <summary>
    /// Augments a <see cref="Exception.Data"/> with classifications about the exception and performs the specified <see cref="AugmentAction"/>
    /// </summary>
    /// <returns>returns `true` when <see cref="AugmentAction.EnterCatch"/> is specified, otherwise returns `false`</returns>
    /// <example>
    /// Transient exception information is stored as <see cref="ITransientErrorInfo"/> and analytics are queued about the <see cref="TransientExceptionType"/>
    /// </example>
    public static bool AugmentAndFilterError(
        this IAugmentException augmentException,
        IDbCommand? dbCommand,
        Exception ex,
        Func<bool> shouldEnterCatch,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        var sourceOfCapture = SourceInfo.FromCallerAttributes(filePath, lineNumber, memberName);
        return AugmentExceptionCommon(augmentException,
            dbCommand,
            ex,
            sourceOfCapture,
            shouldEnterCatch() ? AugmentAction.EnterCatch : AugmentAction.ResumeThrow);
    }

    /// <summary>
    /// Augments a <see cref="Exception.Data"/> with classifications about the exception and performs the specified <see cref="AugmentAction"/>
    /// </summary>
    /// <returns>returns `true` when <see cref="AugmentAction.EnterCatch"/> is specified, otherwise returns `false`</returns>
    /// <example>
    /// Transient exception information is stored as <see cref="ITransientErrorInfo"/> and analytics are queued about the <see cref="TransientExceptionType"/>
    /// </example>
    public static bool AugmentAndFilterError(
        this IAugmentException augmentException,
        Exception ex,
        Func<bool> shouldEnterCatch,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "")
    {
        var sourceOfCapture = SourceInfo.FromCallerAttributes(filePath, lineNumber, memberName);
        var augmentAction = shouldEnterCatch() ? AugmentAction.EnterCatch : AugmentAction.ResumeThrow;
        return AugmentExceptionCommon(augmentException,
            NoOpDbCommand.Instance,
            ex,
            sourceOfCapture,
            augmentAction);
    }

    private static bool AugmentExceptionCommon(
        IAugmentException augmentException,
        IDbCommand? dbCommand,
        Exception ex,
        SourceInfo callerSourceInfo,
        AugmentAction throwOrCatch,
        Action? logAction = null
        )
    {
        logAction?.Invoke();

        if (!ex.Data.Contains(ExceptionDataKeys.SourceOfCapture))
        {
            ex.Data[ExceptionDataKeys.SourceOfCapture] = callerSourceInfo;
        }

        //since `dbCommand` may be null, the caller [within this class] passes this sentinel to indicate that
        //the calling context is a database operation not associated with an `IDbCommand` reference
        if (ReferenceEquals(NoOpDbCommand.Instance, dbCommand))
        {
            augmentException.AugmentException(ex);
        }
        else //the calling context has an `IDbCommand` reference available [and its value is allowed to be null]
        {
            augmentException.AugmentException(dbCommand, ex);
        }

        return throwOrCatch == AugmentAction.EnterCatch;
    }

    /// <summary>
    /// Returns whether an exception is transient, regardless of source (.NET / DB).
    /// </summary>
    /// <remarks>
    /// DB-specific implementations should be defined in their respective derived class,
    /// but should almost always call the base method as well.
    /// </remarks>
    public static bool IsTransientException(this IAugmentException augmentException, Exception ex) =>
        augmentException.IsTransientException(ex, out _);
}

/// <summary>
/// specifies the action to take on `catch(Exception ex) when (FilterAndAugmentError(ex))`
/// </summary>
public enum AugmentAction
{
    /// <summary>
    /// Augment error, DO NOT ENTER `catch`, resume `throw`
    /// </summary>
    ResumeThrow = 0,
    /// <summary>
    /// Augment error, ENTER `catch`
    /// </summary>
    EnterCatch = 1
}