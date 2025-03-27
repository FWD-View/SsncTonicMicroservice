using System.Diagnostics.CodeAnalysis;

namespace Tonic.Common.OracleHelper.Models.Enums;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum OracleSessionStatus
{
    /// <summary>
    /// Session NOT executing SQL
    /// </summary>
    INACTIVE = 0,
    /// <summary>
    /// Session currently executing SQL
    /// </summary>
    ACTIVE,
    /// <summary>
    /// Session marked to be killed
    /// </summary>
    KILLED,
    /// <summary>
    /// Session temporarily cached for use by Oracle*XA
    /// </summary>
    CACHED,
    /// <summary>
    /// Session inactive, waiting on the client
    /// </summary>
    SNIPED
}