using System.Diagnostics.CodeAnalysis;

namespace Tonic.Common.OracleHelper.Models.Enums;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DisplayFormat
{
    /// <summary>
    /// displays most relevant information
    /// </summary>
    TYPICAL = 0,
    /// <summary>
    /// displays minimum information
    /// </summary>
    BASIC,
    /// <summary>
    /// like TYPICAL but without parallel information
    /// </summary>
    SERIAL,
    /// <summary>
    /// displays all information
    /// </summary>
    ALL
}