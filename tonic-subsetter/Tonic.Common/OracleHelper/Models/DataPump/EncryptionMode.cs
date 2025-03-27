namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies the type of security to use when encryption and decryption are performed.
/// </summary>
public enum EncryptionMode
{
    /// <summary>
    /// mode creates a dump file set that can later be imported either transparently or by specifying a password that was used when the dual-mode encrypted dump file set was created.
    /// </summary>
    /// <remarks>
    /// When you later import the dump file set created in DUAL mode, you can use either the wallet or the password that was specified with the ENCRYPTION_PASSWORD parameter.
    /// DUAL mode is best suited for cases in which the dump file set will be imported on-site using the wallet, but which may also need to be imported offsite where the wallet is not available.
    /// </remarks>
    [Parameter("DUAL")]
    Dual,
    /// <summary>
    /// mode requires that you provide a password when creating encrypted dump file sets. You will need to provide the same password when you import the dump file set.
    /// </summary>
    /// <remarks>
    /// PASSWORD mode requires that you also specify the ENCRYPTION_PASSWORD parameter.
    /// The PASSWORD mode is best suited for cases in which the dump file set will be imported into a different or remote database, but which must remain secure in transit.
    /// </remarks>
    [Parameter("PASSWORD")]
    Password,
    /// <summary>
    /// mode allows an encrypted dump file set to be created without any intervention from a database administrator (DBA), provided the required wallet is available.
    /// </summary>
    /// <remarks>
    /// Therefore, the ENCRYPTION_PASSWORD parameter is not required, and will in fact, cause an error if it is used in TRANSPARENT mode.
    /// This encryption mode is best suited for cases in which the dump file set will be imported into the same database from which it was exported.
    /// </remarks>
    [Parameter("TRANSPARENT")]
    Transparent,
}