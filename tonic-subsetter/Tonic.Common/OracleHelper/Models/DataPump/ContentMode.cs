namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Enables you to filter what Import/Export loads/unloads: data only, metadata only, or both.
/// </summary>
public enum ContentMode
{
    /// <summary>
    /// unloads both data and metadata. This is the default.
    /// </summary>
    [Parameter("ALL")]
    All = 0,
    /// <summary>
    /// unloads only table row data; no database object definitions are unloaded.
    /// </summary>
    [Parameter("DATA_ONLY")]
    DataOnly,
    /// <summary>
    /// unloads only database object definitions; no table row data is unloaded.
    /// Be aware that if you specify CONTENT=METADATA_ONLY, then when the dump file is subsequently imported, any index or table statistics imported from the dump file will be locked after the import.
    /// </summary>
    [Parameter("METADATA_ONLY")]
    MetadataOnly
}