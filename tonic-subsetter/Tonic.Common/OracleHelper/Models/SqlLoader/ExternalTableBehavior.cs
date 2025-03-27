namespace Tonic.Common.OracleHelper.Models.SqlLoader;

/// <summary>
/// The EXTERNAL_TABLE parameter instructs SQL*Loader whether to load data using the external tables option.
/// </summary>
public enum ExternalTableBehavior
{
    /// <summary>
    /// It means the load is performed using either conventional or direct path mode.
    /// </summary>
    /// <remarks>this is the default</remarks>
    [Parameter("NOT_USED")]
    NotUsed = 0,

    /// <summary>
    /// Places all the SQL statements needed to do the load using external tables, as described in the control file, in the SQL*Loader log file.
    /// </summary>
    /// <remarks>
    /// These SQL statements can be edited and customized.
    /// The actual load can be done later without the use of SQL*Loader by executing these statements in SQL*Plus.
    /// </remarks>
    [Parameter("GENERATE_ONLY")]
    GenerateOnly,

    /// <summary>
    /// Attempts to execute the SQL statements that are needed to do the load using external tables.
    /// However, if any of the SQL statements returns an error, then the attempt to load stops.
    /// Statements are placed in the log file as they are executed.
    /// This means that if a SQL statement returns an error, then the remaining SQL statements required for the load will not be placed in the log file.
    /// </summary>
    [Parameter("EXECUTE")]
    Execute,
}