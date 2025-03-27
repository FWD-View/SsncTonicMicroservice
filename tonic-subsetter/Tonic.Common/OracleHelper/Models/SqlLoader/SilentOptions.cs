namespace Tonic.Common.OracleHelper.Models.SqlLoader;

/// <summary>
/// The SILENT parameter suppresses some of the content that is written to the screen during a SQL*Loader operation.
/// </summary>
public enum SilentOptions
{
    /// <summary>
    /// Suppresses the SQL*Loader header messages that normally appear on the screen. Header messages still appear in the log file.
    /// </summary>
    [Parameter("HEADER")]
    Header,
    /// <summary>
    /// Suppresses the "commit point reached" messages and the status messages for the load that normally appear on the screen.
    /// </summary>
    [Parameter("FEEDBACK")]
    Feedback,
    /// <summary>
    /// Suppresses the data error messages in the log file that occur when a record generates an Oracle error that causes it to be written to the bad file. A count of rejected records still appears.
    /// </summary>
    [Parameter("ERRORS")]
    Errors,
    /// <summary>
    /// Suppresses the messages in the log file for each record written to the discard file.
    /// </summary>
    [Parameter("DISCARDS")]
    Discard,
    /// <summary>
    /// Disables writing the per-partition statistics to the log file during a direct load of a partitioned table.
    /// </summary>
    [Parameter("PARTITIONS")]
    Partitions,
    /// <summary>
    /// Implements all of the suppression values: HEADER, FEEDBACK, ERRORS, DISCARDS, and PARTITIONS.
    /// </summary>
    [Parameter("ALL")]
    All,

}