namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// The DATA_OPTIONS parameter designates how certain types of data should be handled during import/export operations.
/// </summary>
public enum DataOptions
{
    /// <summary>
    /// Specifies that you do not want the import operation to use the APPEND hint while loading the data object.
    /// Disabling the APPEND hint can be useful if there is a small set of data objects to load that already exist in the database and some other application may be concurrently accessing one or more of the data objects.
    /// </summary>
    /// <remarks>
    /// If DISABLE_APPEND_HINT is not set, then the default behavior is to use the APPEND hint for loading data objects.
    /// </remarks>
    [Parameter("DISABLE_APPEND_HINT")]
    DisableAppendHint,

    /// <summary>
    /// affects how non-deferred constraint violations are handled while a data object (table, partition, or subpartition) is being loaded.
    /// It has no effect on the load if deferred constraint violations are encountered. Deferred constraint violations always cause the entire load to be rolled back.
    /// </summary>
    /// <remarks>
    /// The SKIP_CONSTRAINT_ERRORS option specifies that you want the import operation to proceed even if non-deferred constraint violations are encountered.
    /// It logs any rows that cause non-deferred constraint violations, but does not stop the load for the data object experiencing the violation.
    /// </remarks>
    [Parameter("SKIP_CONSTRAINT_ERRORS")]
    SkipConstraintErrors,

    /// <summary>
    /// specifies that you want the import operation to reject any rows that experience data loss because the default replacement character was used during character set conversion.
    /// </summary>
    /// <remarks>
    /// If REJECT_ROWS_WITH_REPL_CHAR is not set, then the default behavior is to load the converted rows with replacement characters.
    /// </remarks>
    [Parameter("REJECT_ROWS_WITH_REPL_CHAR")]
    RejectRowsWithReplacementCharacter,

    /// <summary>
    /// EXPORT: The XML_CLOBS option specifies that XMLType columns are to be imported/exported in uncompressed CLOB format regardless of the XMLType storage format that was defined for them.
    /// </summary>
    /// <remarks>
    /// XMLType stored as CLOB is deprecated as of Oracle Database 12c Release 1 (12.1). XMLType tables and columns are now stored as binary XML.
    /// If a table has XMLType columns stored only in CLOB format, then it is not necessary to specify the XML_CLOBS option because Data Pump automatically exports them in CLOB format.If a table has XMLType columns stored as any combination of object-relational (schema-based), binary, or CLOB formats, then Data Pump exports them in compressed format, by default. This is the preferred method. However, if you need to export the data in uncompressed CLOB format, you can use the XML_CLOBS option to override the default.
    /// </remarks>
    [Parameter("XML_CLOBS")]
    XmlClobs,
}