namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies that messages displayed during export operations be timestamped.
/// </summary>
/// <remarks>
/// You can use the timestamps to figure out the elapsed time between different phases of a Data Pump operation.
/// Such information can be helpful in diagnosing performance problems and estimating the timing of future similar operations.
/// </remarks>
public enum LogTimeBehavior
{
    /// <summary>
    /// No timestamps on status or log file messages (same as default)
    /// </summary>
    [Parameter("NONE")]
    None = 0,
    /// <summary>
    /// Timestamps on status messages only
    /// </summary>
    [Parameter("STATUS")]
    Status,
    /// <summary>
    /// Timestamps on log file messages only
    /// </summary>
    [Parameter("LOGFILE")]
    LogFile,
    /// <summary>
    /// Timestamps on both status and log file messages
    /// </summary>
    [Parameter("ALL")]
    All,
}