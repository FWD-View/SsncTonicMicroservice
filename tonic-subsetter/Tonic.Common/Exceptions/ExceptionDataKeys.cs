using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Tonic.Common.Exceptions;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ExceptionDataKeys
{
    /// <summary>
    /// a <see cref="TextPosition"/> where an error occured in some source text (e.g. in code or a query)
    /// </summary>
    public const string ErrorTextPosition = nameof(ErrorTextPosition);

    /// <summary>
    /// indicates that a command timeout was observed in 'AugmentAndFilterError' methods
    /// </summary>
    public const string CommandTimeout = nameof(CommandTimeout);

    /// <summary>
    /// contains the <see cref="ITransientErrorInfo"/> of a transient exception observed in 'AugmentAndFilterError' methods
    /// </summary>
    public const string TransientErrorInfo = nameof(TransientErrorInfo);

    /// <summary>
    /// contains the <see cref="SourceInfo"/> representing the source location where the <see cref="Exception"/> was first augmented by
    /// <see cref="ExceptionAugmentorExtensions"/>.AugmentAndFilterError
    /// </summary>
    public const string SourceOfCapture = nameof(SourceOfCapture);

    /// <summary>
    /// the count of exceptions of a particular type observed during a retry loop
    /// </summary>
    public const string OccurrencesDuringRetry = nameof(OccurrencesDuringRetry);

    /// <summary>
    /// the count of retries during of a retry loop
    /// </summary>
    public const string RetryCount = nameof(RetryCount);

    /// <summary>
    /// the outcome of a retry loop for a transient error
    /// </summary>
    public const string TransientRetryOutcome = nameof(TransientRetryOutcome);

    /// <summary>
    /// <see cref="ExceptionDataKeys"/> that contain sensitive data and need to be obfuscated in logs and such
    /// </summary>
    public static readonly IReadOnlySet<string> ObfuscateDataForTheseKeys = new[]
    {
        ErrorTextPosition,
        TransientErrorInfo
    }.ToHashSet();
}