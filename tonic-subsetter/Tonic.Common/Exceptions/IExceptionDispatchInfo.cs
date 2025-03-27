using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace Tonic.Common.Exceptions;

/// <summary>
/// Represents an exception whose state is captured at a certain point in code.
/// </summary>
/// <remarks>
/// This class defines support for separating the exception dispatch details
/// (like stack trace, watson buckets, etc) from the actual managed exception
/// object. This allows us to track error (via the exception object) independent
/// of the path the error takes.
///
/// This is particularly useful for frameworks that wish to propagate
/// exceptions (i.e. errors to be precise) across threads.
/// </remarks>
public interface IExceptionDispatchInfo
{
    /// <summary>
    /// Gets the exception object represented by this <see cref="IExceptionDispatchInfo"/> instance.
    /// </summary>
    Exception SourceException { get; }

    /// <summary>
    /// Throws the exception that is represented by the current <see cref="IExceptionDispatchInfo"/> object,
    /// after restoring the state that was saved when the exception was captured.
    /// </summary>
    /// <remarks>
    /// When a framework needs to "Rethrow" an exception on a thread different (but not necessarily so) from
    /// where it was thrown, it should invoke this method against the <see cref="IExceptionDispatchInfo"/>
    /// created for the exception in question.
    ///
    /// This method will restore the original stack trace and bucketing details before throwing
    /// the exception so that it is easy, from debugging standpoint, to understand what really went wrong on
    /// the original thread.
    /// </remarks>
    [DoesNotReturn]
    [StackTraceHidden]
    public void Throw();
}

public static class ExceptionDispatchInfoExtensions
{
    public static IExceptionDispatchInfo CaptureDispatchInfo(this Exception exception) =>
        new CapturedException(exception);

    //wrapper suitable for mocks
    private sealed class CapturedException : IExceptionDispatchInfo
    {
        private readonly ExceptionDispatchInfo _exceptionDispatchInfo;

        internal CapturedException(Exception exception)
        {
            _exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
        }

        public Exception SourceException => _exceptionDispatchInfo.SourceException;

        [DoesNotReturn]
        [StackTraceHidden]
        public void Throw() => _exceptionDispatchInfo.Throw();
    }
}