namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// B, KB, MB, GB, or TB (indicating bytes, kilobytes, megabytes, gigabytes, and terabytes respectively).
/// </summary>
public enum FileSizeUnit
{
    /// <summary>
    /// Bytes
    /// </summary>
    B,
    /// <summary>
    /// Kilobytes
    /// </summary>
    KB,
    /// <summary>
    /// Megabytes
    /// </summary>
    MB,
    /// <summary>
    /// Gigabytes
    /// </summary>
    GB,
    /// <summary>
    /// Terabytes
    /// </summary>
    TB
}