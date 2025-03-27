namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies which data to compress before writing to the dump file set.
/// </summary>
public enum CompressionOptions
{
    /// <summary>
    /// results in all metadata being written to the dump file in compressed format.
    /// </summary>
    /// <remarks>
    /// This is the default.
    /// </remarks>
    [Parameter("METADATA_ONLY")]
    MetaDataOnly = 0,
    /// <summary>
    /// results in all data being written to the dump file in compressed format.
    /// </summary>
    /// <remarks>
    /// The DATA_ONLY option requires that the Oracle Advanced Compression option be enabled.
    /// </remarks>
    [Parameter("DATA_ONLY")]
    DataOnly,
    /// <summary>
    /// enables compression for the entire export operation.
    /// </summary>
    /// <remarks>
    /// The ALL option requires that the Oracle Advanced Compression option be enabled.
    /// </remarks>
    [Parameter("ALL")]
    All,
    /// <summary>
    /// disables compression for the entire export operation.
    /// </summary>
    [Parameter("NONE")]
    None,
}