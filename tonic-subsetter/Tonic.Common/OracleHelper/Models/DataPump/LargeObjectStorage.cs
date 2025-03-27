namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// TRANSFORM values for the LOB_STORAGE
/// </summary>
/// <remarks>
/// LOB segments are created with the specified storage, either SECUREFILE or BASICFILE.
/// If the value is NO_CHANGE (the default), the LOB segments are created with the same storage they had in the source database.
/// If the value is DEFAULT, then the keyword (SECUREFILE or BASICFILE) is omitted and the LOB segment is created with the default storage.
///
/// Specifying this transform changes LOB storage for all tables in the job, including tables that provide storage for materialized views.
///
/// The LOB_STORAGE transform is not valid in transportable import jobs.
/// </remarks>
public enum LargeObjectStorage
{
    [Parameter("DEFAULT")]
    Default = 0,
    [Parameter("NO_CHANGE")]
    NoChange,
    [Parameter("SECUREFILE")]
    SecureFile,
    [Parameter("BASICFILE")]
    BasicFile,
}