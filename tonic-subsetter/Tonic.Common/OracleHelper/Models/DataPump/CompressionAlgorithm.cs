namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies the compression algorithm to be used when compressing dump file data.
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// Offers a good combination of compression ratios and speed; the algorithm used is the same as in previous versions of Oracle Data Pump.
    /// </summary>
    /// <remarks>
    /// This is the default.
    /// </remarks>
    [Parameter("BASIC")]
    Basic = 0,
    /// <summary>
    /// Least impact on export throughput and suited for environments where CPU resources are the limiting factor.
    /// </summary>
    [Parameter("LOW")]
    Low,
    /// <summary>
    /// Recommended for most environments. This option, like the BASIC option, provides a good combination of compression ratios and speed, but it uses a different algorithm than BASIC.
    /// </summary>
    [Parameter("MEDIUM")]
    Medium,
    /// <summary>
    /// Best suited for situations in which dump files will be copied over slower networks where the limiting factor is network speed.
    /// </summary>
    [Parameter("HIGH")]
    High,
}