using System.Diagnostics;

namespace Tonic.Common.OracleHelper.ErrorCodes;

/// <summary>
/// interface to be used in conjunction with <see cref="DebuggerDisplayAttribute"/>
/// </summary>
public interface IDebuggerDisplay
{
    /// <summary>
    /// property to be used in conjunction with <see cref="DebuggerDisplayAttribute"/>
    /// </summary>
    /// <remarks>
    /// When implemented by a `record` type, as long as the type does NOT implement 'set',
    /// this will NOT participate in the compiler-generated `Equals` and `GetHashCode` implementations
    /// (and this is a good thing!)
    /// </remarks>
    string DebuggerDisplay { get; }
}