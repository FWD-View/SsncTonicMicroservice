namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Specifies the version of database objects to be imported/exported. Only database objects and attributes that are compatible with the specified release will be imported/exported
/// </summary>
/// <remarks>
/// This parameter can be used to load a target system whose Oracle database is at an earlier compatibility release than that of the source system.
/// Database objects or attributes on the source system that are incompatible with the specified release will not be moved to the target
/// </remarks>
public enum ObjectVersion
{
    /// <summary>
    /// The version of the metadata corresponds to the database compatibility level as specified on the COMPATIBLE initialization parameter.
    /// </summary>
    /// <remarks>
    /// This is the default value.
    ///  Database compatibility must be set to 9.2 or later.
    /// </remarks>
    [Parameter("COMPATIBLE")]
    Compatible = 0,
    /// <summary>
    /// The version of the metadata and resulting SQL DDL corresponds to the database release regardless of its compatibility level.
    /// </summary>
    [Parameter("LATEST")]
    Latest,
    /// <summary>
    /// A specific database release
    /// </summary>
    /// <remarks>
    /// (for example, 11.2.0). In Oracle Database 11g, this value cannot be lower than 9.2.
    /// </remarks>
    [Parameter("{0}")]
    VersionString
}