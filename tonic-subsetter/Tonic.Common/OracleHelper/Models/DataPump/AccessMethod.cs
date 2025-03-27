namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Instructs Import/Export to use a particular method to load/unload data.
/// </summary>
public enum AccessMethod
{
    [Parameter("AUTOMATIC")]
    Automatic = 0,
    [Parameter("DIRECT_PATH")]
    DirectPath,
    [Parameter("EXTERNAL_TABLE")]
    ExternalTable
}