using Tonic.Common.OracleHelper.Models.DataPump;

namespace Tonic.Common.OracleHelper.Models;

/// <summary>
/// Parameters available in the command-line mode of Data Pump Export
/// </summary>
/// <remarks>
/// EXPORT: https://docs.oracle.com/database/121/SUTIL/GUID-33880357-06B1-4CA2-8665-9D41347C6705.htm#SUTIL836
/// </remarks>
public sealed record DataPumpExportParameters : DataPumpParameters
{
    /// <summary>
    /// Specifies which data to compress before writing to the dump file set.
    /// </summary>
    /// <remarks>
    /// Default: METADATA_ONLY
    /// Restrictions
    ///
    ///     To make full use of all these compression options, the COMPATIBLE initialization parameter must be set to at least 11.0.0.
    ///
    ///     The METADATA_ONLY option can be used even if the COMPATIBLE initialization parameter is set to 10.2.
    ///
    ///     Compression of data using ALL or DATA_ONLY is valid only in the Enterprise Edition of Oracle Database 11g or later, and they require that the Oracle Advanced Compression option be enabled.
    /// </remarks>
    /// <example>
    /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_comp.dmp COMPRESSION=METADATA_ONLY
    /// </example>
    [Parameter("COMPRESSION")]
    public CompressionOptions? Compression { get; set; }

    /// <summary>
    /// Specifies the compression algorithm to be used when compressing dump file data.
    /// </summary>
    /// <remarks>
    /// The performance of a compression algorithm is characterized by its CPU usage and by the compression ratio (the size of the compressed output as a percentage of the uncompressed input).
    /// These measures vary on the size and type of inputs as well as the speed of the compression algorithms used.
    /// The compression ratio generally increases from low to high, with a trade-off of potentially consuming more CPU resources.
    ///
    /// It is recommended that you run tests with the different compression levels on the data in your environment.
    /// Choosing a compression level based on your environment, workload characteristics, and size and type of data is the only way to ensure that the exported dump file set compression level meets your performance and storage requirements.
    ///
    /// Restrictions
    ///
    ///     To use this feature, database compatibility must be set to 12.0.0 or later.
    ///
    ///     This feature requires that the Oracle Advanced Compression option be enabled.
    /// </remarks>
    /// <example>
    /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr.dmp COMPRESSION=DATA_ONLY COMPRESSION_ALGORITHM=LOW
    /// </example>
    [Parameter("COMPRESSION_ALGORITHM")]
    public CompressionAlgorithm? CompressionAlgorithm { get; set; }

    /// <summary>
    /// Specifies whether to encrypt data before writing it to the dump file set.
    /// </summary>
    /// <remarks>
    /// Default: The default value depends upon the combination of encryption-related parameters that are used.
    ///     To enable encryption, either the ENCRYPTION or ENCRYPTION_PASSWORD parameter, or both, must be specified.
    ///          If only the ENCRYPTION_PASSWORD parameter is specified, then the ENCRYPTION parameter defaults to ALL.
    ///          If only the ENCRYPTION parameter is specified and the Oracle encryption wallet is open, then the default mode is TRANSPARENT. If only the ENCRYPTION parameter is specified and the wallet is closed, then an error is returned.
    ///          If neither ENCRYPTION nor ENCRYPTION_PASSWORD is specified, then ENCRYPTION defaults to NONE.
    ///
    /// SecureFiles Considerations for Encryption
    ///
    ///     If the data being exported includes SecureFiles that you want to be encrypted, then you must specify ENCRYPTION=ALL to encrypt the entire dump file set.
    ///     Encryption of the entire dump file set is the only way to achieve encryption security for SecureFiles during a Data Pump export operation.
    ///     For more information about SecureFiles, see Oracle Database SecureFiles and Large Objects Developer's Guide.
    ///
    /// Oracle Database Vault Considerations for Encryption
    ///
    ///     When an export operation is started, Data Pump determines whether Oracle Database Vault is enabled.
    ///     If it is, and dump file encryption has not been specified for the job, a warning message is returned to alert you that secure data is being written in an insecure manner (clear text) to the dump file set:
    ///
    ///         ORA-39327: Oracle Database Vault data is being stored unencrypted in dump file set
    ///
    ///     You can abort the current export operation and start a new one, specifying that the output dump file set be encrypted.
    ///
    /// Restrictions
    ///
    ///     To specify the ALL, DATA_ONLY, or METADATA_ONLY options, the COMPATIBLE initialization parameter must be set to at least 11.0.0.
    ///
    ///     This parameter is valid only in the Enterprise Edition of Oracle Database 11g or later.
    ///
    ///     Data Pump encryption features require that the Oracle Advanced Security option be enabled. See Oracle Database Licensing Information for information about licensing requirements for the Oracle Advanced Security option.
    /// </remarks>
    /// <example>
    /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_enc.dmp JOB_NAME=enc1 ENCRYPTION=data_only ENCRYPTION_PASSWORD=foobar
    /// </example>
    [Parameter("ENCRYPTION")]
    public EncryptionOptions? Encryption { get; set; }

    /// <summary>
    /// Specifies which cryptographic algorithm should be used to perform the encryption.
    /// </summary>
    /// <remarks>
    /// Default: AES128
    ///
    /// Restrictions
    ///
    ///     To use this encryption feature, the COMPATIBLE initialization parameter must be set to at least 11.0.0.
    ///     The ENCRYPTION_ALGORITHM parameter requires that you also specify either the ENCRYPTION or ENCRYPTION_PASSWORD parameter; otherwise an error is returned.
    ///     The ENCRYPTION_ALGORITHM parameter cannot be used in conjunction with ENCRYPTION=ENCRYPTED_COLUMNS_ONLY because columns that are already encrypted cannot have an additional encryption format assigned to them.
    ///     This parameter is valid only in the Enterprise Edition of Oracle Database 11g or later.
    ///     Data Pump encryption features require that the Oracle Advanced Security option be enabled.
    ///         See Oracle Database Licensing Information for information about licensing requirements for the Oracle Advanced Security option.
    /// </remarks>
    /// <example>
    /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_enc3.dmp ENCRYPTION_PASSWORD=foobar ENCRYPTION_ALGORITHM=AES128
    /// </example>
    [Parameter("ENCRYPTION_ALGORITHM")]
    public EncryptionAlgorithm? EncryptionAlgorithm { get; set; }

    /// <summary>
    /// Specifies the type of security to use when encryption and decryption are performed.
    /// </summary>
    /// <remarks>
    /// Default: The default mode depends on which other encryption-related parameters are used.
    ///     If only the ENCRYPTION parameter is specified and the Oracle encryption wallet is open, then the default mode is TRANSPARENT.
    ///     If only the ENCRYPTION parameter is specified and the wallet is closed, then an error is returned.
    ///     If the ENCRYPTION_PASSWORD parameter is specified and the wallet is open, then the default is DUAL.
    ///     If the ENCRYPTION_PASSWORD parameter is specified and the wallet is closed, then the default is PASSWORD.
    ///
    /// Restrictions
    ///
    ///     To use DUAL or TRANSPARENT mode, the COMPATIBLE initialization parameter must be set to at least 11.0.0.
    ///     When you use the ENCRYPTION_MODE parameter, you must also use either the ENCRYPTION or ENCRYPTION_PASSWORD parameter.
    ///         Otherwise, an error is returned.
    ///     When you use the ENCRYPTION=ENCRYPTED_COLUMNS_ONLY, you cannot use the ENCRYPTION_MODE parameter.
    ///         Otherwise, an error is returned.
    ///     This parameter is valid only in the Enterprise Edition of Oracle Database 11g or later.
    ///     Data Pump encryption features require that the Oracle Advanced Security option be enabled.
    ///         See Oracle Database Licensing Information for information about licensing requirements for the Oracle Advanced Security option.
    /// </remarks>
    /// <example>
    /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_enc4.dmp ENCRYPTION=all ENCRYPTION_PASSWORD=secretwords ENCRYPTION_ALGORITHM=AES256 ENCRYPTION_MODE=DUAL
    /// </example>
    [Parameter("ENCRYPTION_MODE")]
    public EncryptionMode? EncryptionMode { get; set; }

    /// <summary>
    /// Instructs Export to estimate the space that a job would consume, without actually performing the export operation.
    /// </summary>
    /// <remarks>
    /// Default: NO
    ///
    /// If ESTIMATE_ONLY=YES, then Export estimates the space that would be consumed, but quits without actually performing the export operation.
    ///
    /// Restrictions
    ///
    ///     The ESTIMATE_ONLY parameter cannot be used in conjunction with the QUERY parameter.
    /// </remarks>
    /// <example>
    /// : expdp hr ESTIMATE_ONLY=YES NOLOGFILE=YES SCHEMAS=HR
    /// </example>
    [Parameter("ESTIMATE_ONLY")]
    public bool? EstimateOnly { get; set; }

    /// <summary>
    /// Specifies the maximum size of each dump file.
    /// If the size is reached for any member of the dump file set, then that file is closed and an attempt is made to create a new file, if the file specification contains a substitution variable or if additional dump files have been added to the job.
    /// </summary>
    /// <remarks>
    /// <see cref="FileSizeUnit"/>
    /// The integer can be immediately followed (do not insert a space) by B, KB, MB, GB, or TB (indicating bytes, kilobytes, megabytes, gigabytes, and terabytes respectively).
    /// Bytes is the default.
    /// The actual size of the resulting file may be rounded down slightly to match the size of the internal blocks used in dump files.
    ///
    /// Restrictions
    ///
    ///     The minimum size for a file is ten times the default Data Pump block size, which is 4 kilobytes.
    ///
    ///     The maximum size for a file is 16 terabytes.
    /// </remarks>
    /// <example>
    /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_3m.dmp FILESIZE=3MB
    ///     If 3 megabytes had not been sufficient to hold all the exported data, then the following error would have been displayed and the job would have stopped:
    ///         ORA-39095: Dump file space has been exhausted: Unable to allocate 217088 bytes
    /// </example>
    [Parameter("FILESIZE")]
    public string? FileSize { get; set; }

    /// <summary>
    /// Specifies whether to overwrite a preexisting dump file.
    /// </summary>
    /// <remarks>
    /// Normally, Data Pump Export will return an error if you specify a dump file name that already exists.
    /// The REUSE_DUMPFILES parameter allows you to override that behavior and reuse a dump file name.
    /// For example, if you performed an export and specified DUMPFILE=hr.dmp and REUSE_DUMPFILES=YES, then hr.dmp would be overwritten if it already existed.
    /// Its previous contents would be lost and it would contain data for the current export instead.
    /// </remarks>
    /// <example>
    /// expdp hr DIRECTORY=dpump_dir1 DUMPFILE=enc1.dmp TABLES=employees REUSE_DUMPFILES=YES
    /// </example>
    [Parameter("REUSE_DUMPFILES")]
    public bool? ReuseDumpFiles { get; set; }

    /// <summary>
    /// Allows you to specify a percentage of the data rows to be sampled and unloaded from the source database.
    /// </summary>
    /// <remarks>
    /// This parameter allows you to export subsets of data by specifying the percentage of data to be sampled and exported.
    /// The sample_percent indicates the probability that a row will be selected as part of the sample.
    /// It does not mean that the database will retrieve exactly that amount of rows from the table.
    /// The value you supply for sample_percent can be anywhere from .000001 up to, but not including, 100.
    ///
    /// The sample_percent can be applied to specific tables. In the following example, 50% of the HR.EMPLOYEES table will be exported:
    ///
    ///     SAMPLE="HR"."EMPLOYEES":50
    /// If you specify a schema, then you must also specify a table. However, you can specify a table without specifying a schema; the current user will be assumed. If no table is specified, then the sample_percent value applies to the entire export job.
    ///
    ///     You can use this parameter with the Data Pump Import PCTSPACE transform, so that the size of storage allocations matches the sampled data subset. (See the Import "TRANSFORM" parameter.)
    ///
    /// Restrictions
    ///
    ///     The SAMPLE parameter is not valid for network exports.
    /// </remarks>
    /// <example>
    /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=sample.dmp SAMPLE=70
    /// </example>
    [Parameter("SAMPLE")]
    public string? Sample { get; set; }

    public override OracleTool OracleTool => OracleTool.DataPumpExport;
}