namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Tells Import what to do if the table it is trying to create already exists.
/// </summary>
public enum TableExistsAction
{
    /// <summary>
    /// leaves the table as is and moves on to the next object
    /// </summary>
    /// <remarks>
    /// This is not a valid option if the CONTENT parameter is set to DATA_ONLY, otherwise it is the default value
    /// </remarks>
    [Parameter("SKIP")]
    Skip = 0,
    /// <summary>
    /// loads rows from the source and leaves existing rows unchanged.
    /// </summary>
    [Parameter("APPEND")]
    Append,
    /// <summary>
    /// deletes existing rows and then loads rows from the source.
    /// </summary>
    [Parameter("TRUNCATE")]
    Truncate,
    /// <summary>
    /// drops the existing table and then creates and loads it from the source.
    /// </summary>
    /// <remarks>
    /// This is not a valid option if the CONTENT parameter is set to DATA_ONLY.
    /// </remarks>
    [Parameter("REPLACE")]
    Replace,
}