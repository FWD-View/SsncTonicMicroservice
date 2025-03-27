namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies which cryptographic algorithm should be used to perform the encryption.
/// </summary>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// AES 128 Bit
    /// </summary>
    /// <remarks>
    /// This is the default
    /// </remarks>
    [Parameter("AES128")]
    Aes128 = 0,
    /// <summary>
    /// AES 192 Bit
    /// </summary>
    [Parameter("AES192")]
    Aes192,
    /// <summary>
    /// AES 256 Bit
    /// </summary>
    [Parameter("AES256")]
    Aes256,
}