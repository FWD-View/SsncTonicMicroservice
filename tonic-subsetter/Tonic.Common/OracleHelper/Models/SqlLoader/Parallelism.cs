namespace Tonic.Common.OracleHelper.Models.SqlLoader;

/// <summary>
/// The DEGREE_OF_PARALLELISM parameter specifies the degree of parallelism to use during the load operation.
/// </summary>
public enum Parallelism
{
    /// <summary>
    /// The load is not performed in parallel
    /// </summary>
    [Parameter("NONE")]
    None = 0,
    /// <summary>
    /// The Oracle database automatically sets the degree of parallelism for the load
    /// </summary>
    [Parameter("AUTO")]
    Auto,
    /// <summary>
    /// The default parallelism of the database (not the default parameter value of AUTO) is used
    /// </summary>
    [Parameter("DEFAULT")]
    Default,
    /// <summary>
    /// A specific degree number
    /// </summary>
    /// <remarks>
    /// a whole number value from 1 to n
    /// </remarks>
    [Parameter("{0}")]
    DegreeNumber
}