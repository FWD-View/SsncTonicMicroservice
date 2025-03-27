namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies whether to encrypt data before writing it to the dump file set.
/// </summary>
public enum EncryptionOptions
{
    /// <summary>
    /// specifies that no data is written to the dump file set in encrypted format.
    /// </summary>
    [Parameter("NONE")]
    None = 0,
    /// <summary>
    /// enables encryption for all data and metadata in the export operation.
    /// </summary>
    [Parameter("ALL")]
    All,
    /// <summary>
    /// specifies that only data is written to the dump file set in encrypted format.
    /// </summary>
    [Parameter("DATA_ONLY")]
    DataOnly,
    /// <summary>
    /// specifies that only encrypted columns are written to the dump file set in encrypted format.
    /// </summary>
    /// <remarks>
    /// This option cannot be used in conjunction with the ENCRYPTION_ALGORITHM parameter because the columns already have an assigned encryption format and by definition, a column can have only one form of encryption.
    ///
    /// If you specify the ENCRYPTED_COLUMNS_ONLY option, then the maximum length allowed for an encryption password (specified with ENCRYPTION_PASSWORD) is 30 bytes.
    ///
    /// If you specify the ALL, DATA_ONLY, or METADATA_ONLY options or if you accept the default, then the maximum length allowed for an encryption password is 128 bytes.
    ///
    /// To use the ENCRYPTED_COLUMNS_ONLY option, you must have Oracle Advanced Security Transparent Data Encryption (TDE) enabled. See Oracle Database Advanced Security Guide for more information about TDE.
    /// </remarks>
    [Parameter("ENCRYPTED_COLUMNS_ONLY")]
    EncryptedColumnsOnly,
    /// <summary>
    /// specifies that only metadata is written to the dump file set in encrypted format.
    /// </summary>
    [Parameter("METADATA_ONLY")]
    MetadataOnly,
}