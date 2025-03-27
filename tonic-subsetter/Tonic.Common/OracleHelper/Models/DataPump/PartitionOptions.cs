namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies how table partitions should be created during an import operation.
/// </summary>
public enum PartitionOptions
{
    /// <summary>
    /// A value of none creates tables as they existed on the system from which the export operation was performed.
    /// </summary>
    /// <remarks>
    /// You cannot use the none option or the merge option if the export was performed with the transportable method, along with a partition or subpartition filter.
    /// In such a case, you must use the departition option.
    /// </remarks>
    [Parameter("NONE")]
    None = 0,
    /// <summary>
    /// A value of departition promotes each partition or subpartition to a new individual table.
    /// </summary>
    /// <remarks>
    /// The default name of the new table will be the concatenation of the table and partition name or the table and subpartition name, as appropriate.
    /// </remarks>
    [Parameter("DEPARTITION")]
    Departition,
    /// <summary>
    /// A value of merge combines all partitions and subpartitions into one table.
    /// </summary>
    [Parameter("MERGE")]
    Merge,
}