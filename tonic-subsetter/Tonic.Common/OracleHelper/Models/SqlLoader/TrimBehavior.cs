namespace Tonic.Common.OracleHelper.Models.SqlLoader;

/// <summary>
/// The TRIM parameter specifies that spaces should be trimmed from the beginning of a text field, the end of a text field, or both.
/// </summary>
/// <remarks>
/// Spaces include blanks and other nonprinting characters such as tabs, line feeds, and carriage returns.
/// </remarks>
public enum TrimBehavior
{
    /// <summary>
    /// If the field is not a delimited field, then spaces will be trimmed from the right.
    /// If the field is a delimited field with OPTIONALLY ENCLOSED BY specified, and the optional enclosures are missing for a particular instance, then spaces will be trimmed from the left.
    /// </summary>
    /// <remarks>
    /// Except in these two special cases, this behaves the same as NOTRIM
    /// This is the default
    /// </remarks>
    [Parameter("LDRTRIM")]
    LeftDelimitedRightTrim = 0,
    /// <summary>
    /// both leading and trailing spaces are trimmed
    /// </summary>
    [Parameter("LRTRIM")]
    LeftRightTrim,
    /// <summary>
    /// no characters will be trimmed from the field
    /// </summary>
    /// <remarks>
    /// This setting generally yields that fastest performance.
    /// </remarks>
    [Parameter("NOTRIM")]
    NoTrim,
    /// <summary>
    /// leading spaces will be trimmed
    /// </summary>
    [Parameter("LTRIM")]
    LeftTrim,
    /// <summary>
    /// trailing spaces are trimmed
    /// </summary>
    [Parameter("RTRIM")]
    RightTrim,
}