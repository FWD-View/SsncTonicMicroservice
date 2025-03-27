namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies whether the transportable option should be used during a table mode export (specified with the TABLES parameter) or a full mode export (specified with the FULL parameter).
/// </summary>
public enum TransportableOption
{
    /// <summary>
    /// Instructs the import/export job to use either the direct path or external table method to unload data rather than the transportable option.
    /// </summary>
    /// <remarks>
    /// This is the default.
    /// </remarks>
    [Parameter("NEVER")]
    Never = 0,
    /// <summary>
    /// Instructs the export job to use the transportable option. If transportable is not possible, then the job fails.
    /// </summary>
    /// <remarks>
    /// In a table mode export, using the transportable option results in a transportable tablespace export in which metadata for only the specified tables, partitions, or subpartitions is exported.
    ///
    /// In a full mode export, using the transportable option results in a full transportable export which exports all objects and data necessary to create a complete copy of the database.
    /// </remarks>
    [Parameter("ALWAYS")]
    Always
}