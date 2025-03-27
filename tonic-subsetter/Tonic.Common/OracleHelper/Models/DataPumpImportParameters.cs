using Tonic.Common.OracleHelper.Models.DataPump;

namespace Tonic.Common.OracleHelper.Models;

/// <summary>
/// Parameters available in the command-line mode of Data Pump Import
/// </summary>
/// <remarks>
/// IMPORT: https://docs.oracle.com/database/121/SUTIL/GUID-BA74D4F6-2840-4002-A673-0A7D9CBB3D78.htm#SUTIL903
/// </remarks>
public sealed record DataPumpImportParameters : DataPumpParameters
{
    /// <summary>
    /// Indicates whether to import just the master table and then stop the job so that the contents of the master table can be examined.
    /// </summary>
    /// <remarks>
    /// Default: NO
    ///
    /// Restrictions
    ///     If the NETWORK_LINK parameter is also specified, then MASTER_ONLY=YES is not supported.
    /// </remarks>
    /// <example>
    /// : impdp hr SCHEMAS=hr DIRECTORY=dpump_dir1 LOGFILE=schemas.log DUMPFILE=expdat.dmp MASTER_ONLY=YES
    /// </example>
    [Parameter("MASTER_ONLY")]
    public bool? MasterOnly { get; set; }

    /// <summary>
    /// Specifies how table partitions should be created during an import operation.
    /// </summary>
    /// <remarks>
    /// Default: The default is departition when partition names are specified on the TABLES parameter and TRANPORTABLE=ALWAYS is set (whether on the import operation or during the export).
    /// Otherwise, the default is none.
    ///
    /// Parallel processing during import of partitioned tables is subject to the following:
    ///
    /// If a partitioned table is imported into an existing partitioned table, then Data Pump only processes one partition or subpartition at a time, regardless of any value that might be specified with the PARALLEL parameter.
    ///
    /// If the table into which you are importing does not already exist and Data Pump has to create it, then the import runs in parallel up to the parallelism specified on the PARALLEL parameter when the import is started.
    ///
    /// Restrictions
    ///
    ///     If the export operation that created the dump file was performed with the transportable method and if a partition or subpartition was specified, then the import operation must use the departition option.
    ///
    ///     If the export operation that created the dump file was performed with the transportable method, then the import operation cannot use PARTITION_OPTIONS=MERGE.
    ///
    ///     If there are any grants on objects being departitioned, then an error message is generated and the objects are not loaded.
    /// </remarks>
    /// <example>
    /// : impdp system TABLES=sh.sales PARTITION_OPTIONS=MERGE DIRECTORY=dpump_dir1 DUMPFILE=sales.dmp REMAP_SCHEMA=sh:scott
    /// </example>
    [Parameter("PARTITION_OPTIONS")]
    public PartitionOptions? PartitionOptions { get; set; }

    /// <summary>
    /// Changes the name of the source data file to the target data file name in all SQL statements where the source data file is referenced: CREATE TABLESPACE, CREATE LIBRARY, and CREATE DIRECTORY.
    /// </summary>
    /// <remarks>
    /// Remapping data files is useful when you move databases between platforms that have different file naming conventions.
    /// The source_datafile and target_datafile names should be exactly as you want them to appear in the SQL statements where they are referenced.
    /// Oracle recommends that you enclose data file names in quotation marks to eliminate ambiguity on platforms for which a colon is a valid file specification character.
    ///
    /// Depending on your operating system, the use of quotation marks when you specify a value for this parameter may also require that you use escape characters.
    /// Oracle recommends that you place this parameter in a parameter file, which can reduce the number of escape characters that might otherwise be needed on the command line.
    ///
    /// You must have the DATAPUMP_IMP_FULL_DATABASE role to specify this parameter.
    /// </remarks>
    /// <example>
    /// payroll.par
    ///     DIRECTORY=dpump_dir1
    ///     FULL=YES
    ///     DUMPFILE=db_full.dmp
    ///     REMAP_DATAFILE="'DB1$:[HRDATA.PAYROLL]tbs6.dbf':'/db1/hrdata/payroll/tbs6.dbf'"
    /// : impdp hr PARFILE=payroll.par
    /// </example>
    [Parameter("REMAP_DATAFILE")]
    public string[]? RemapDataFile { get; set; }

    /// <summary>
    /// Loads all objects from the source schema into a target schema.
    /// </summary>
    /// <remarks>
    /// Multiple REMAP_SCHEMA lines can be specified, but the source schema must be different for each one. However, different source schemas can map to the same target schema.
    /// Note that the mapping may not be 100 percent complete; see the Restrictions section below.
    ///
    /// If the schema you are remapping to does not already exist, then the import operation creates it, provided that the dump file set contains the necessary CREATE USER metadata for the sourc schema, and provided that you are importing with enough privileges.
    /// If your dump file set does not contain the metadata necessary to create a schema, or if you do not have privileges, then the target schema must be created before the import operation is performed.
    ///     This is because the unprivileged dump files do not contain the necessary information for the import to create the schema automatically.
    /// If the import operation does create the schema, then after the import is complete, you must assign it a valid password to connect to it. The SQL statement to do this, which requires privileges, is:
    ///
    ///     SQL> ALTER USER schema_name IDENTIFIED BY new_password
    ///
    /// Restrictions
    ///
    ///     Unprivileged users can perform schema remaps only if their schema is the target schema of the remap. (Privileged users can perform unrestricted schema remaps.)
    ///         For example, SCOTT can remap his BLAKE's objects to SCOTT, but SCOTT cannot remap SCOTT's objects to BLAKE.
    ///
    ///     The mapping may not be 100 percent complete because there are certain schema references that Import is not capable of finding.
    ///         For example, Import will not find schema references embedded within the body of definitions of types, views, procedures, and packages.
    ///
    ///     For triggers, REMAP_SCHEMA affects only the trigger owner.
    ///
    ///     If any table in the schema being remapped contains user-defined object types and that table changes between the time it is exported and the time you attempt to import it, then the import of that table will fail. However, the import operation itself will continue.
    ///
    ///     By default, if schema objects on the source database have object identifiers (OIDs), then they are imported to the target database with those same OIDs. If an object is imported back into the same database from which it was exported, but into a different schema, then the OID of the new (imported) object would be the same as that of the existing object and the import would fail. For the import to succeed you must also specify the TRANSFORM=OID:N parameter on the import. The transform OID:N causes a new OID to be created for the new object, allowing the import to succeed.
    /// </remarks>
    /// <example>
    /// : impdp system DIRECTORY=dpump_dir1 DUMPFILE=hr.dmp REMAP_SCHEMA=hr:scott
    /// </example>
    [Parameter("REMAP_SCHEMA")]
    public string[]? RemapSchema { get; set; }

    /// <summary>
    /// Allows you to rename tables during an import operation.
    /// </summary>
    /// <remarks>
    /// Syntax and Description
    ///
    /// You can use either of the following syntaxes (see the Usage Notes below):
    ///
    ///     REMAP_TABLE=[schema.]old_tablename[.partition]:new_tablename
    ///             OR
    ///     REMAP_TABLE=[schema.]old_tablename[:partition]:new_tablename
    ///
    /// You can use the REMAP_TABLE parameter to rename entire tables or to rename table partitions if the table is being departitioned. (See "PARTITION_OPTIONS".)
    ///
    /// You can also use it to override the automatic naming of table partitions that were exported.
    ///
    /// Usage Notes
    ///
    ///     Be aware that with the first syntax, if you specify REMAP_TABLE=A.B:C, then Import assumes that A is a schema name, B is the old table name, and C is the new table name.
    ///     To use the first syntax to rename a partition that is being promoted to a nonpartitioned table, you must specify a schema name.
    ///
    ///     To use the second syntax to rename a partition being promoted to a nonpartitioned table, you only need to qualify it with the old table name.
    ///     No schema name is required.
    ///
    /// Restrictions
    ///
    ///     Only objects created by the Import will be remapped. In particular, preexisting tables will not be remapped.
    ///
    ///     The REMAP_TABLE parameter will not work if the table being remapped has named constraints in the same schema and the constraints need to be created when the table is created.
    /// </remarks>
    /// <example>
    /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expschema.dmp TABLES=hr.employees REMAP_TABLE=hr.employees:emps
    /// </example>
    [Parameter("REMAP_TABLE")]
    public string[]? RemapTable { get; set; }

    /// <summary>
    /// Remaps all objects selected for import with persistent data in the source tablespace to be created in the target tablespace.
    /// </summary>
    /// <remarks>
    /// Multiple REMAP_TABLESPACE parameters can be specified, but no two can have the same source tablespace.
    /// The target schema must have sufficient quota in the target tablespace.
    ///
    /// Note that use of the REMAP_TABLESPACE parameter is the only way to remap a tablespace in Data Pump Import.
    /// This is a simpler and cleaner method than the one provided in the original Import utility.
    /// That method was subject to many restrictions (including the number of tablespace subclauses) which sometimes resulted in the failure of some DDL commands.
    ///
    /// By contrast, the Data Pump Import method of using the REMAP_TABLESPACE parameter works for all objects, including the user, and it works regardless of how many tablespace subclauses are in the DDL statement.
    ///
    /// Restrictions
    ///
    ///     Data Pump Import can only remap tablespaces for transportable imports in databases where the compatibility level is set to 10.1 or later.
    ///
    ///     Only objects created by the Import will be remapped. In particular, the tablespaces for preexisting tables will not be remapped if TABLE_EXISTS_ACTION is set to SKIP, TRUNCATE, or APPEND.
    /// </remarks>
    /// <example>
    /// : impdp hr REMAP_TABLESPACE=tbs_1:tbs_6 DIRECTORY=dpump_dir1 DUMPFILE=employees.dmp
    /// </example>
    [Parameter("REMAP_TABLESPACE")]
    public string[]? RemapTablespace { get; set; }

    /// <summary>
    /// Specifies whether the import job should reuse existing data files for tablespace creation.
    /// </summary>
    /// <remarks>
    /// If the default (n) is used and the data files specified in CREATE TABLESPACE statements already exist, then an error message from the failing CREATE TABLESPACE statement is issued, but the import job continues.
    /// If this parameter is specified as y, then the existing data files are reinitialized.
    /// Caution:
    ///     Specifying REUSE_DATAFILES=YES may result in a loss of data.
    /// </remarks>
    /// <example>
    /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp LOGFILE=reuse.log REUSE_DATAFILES=YES
    /// </example>
    [Parameter("REUSE_DATAFILES")]
    public bool? ReuseDataFiles { get; set; }

    /// <summary>
    /// Specifies whether Import skips loading tables that have indexes that were set to the Index Unusable state (by either the system or the user).
    /// </summary>
    /// <remarks>
    /// Default: the value of the Oracle Database configuration parameter, SKIP_UNUSABLE_INDEXES.
    ///
    /// If SKIP_UNUSABLE_INDEXES is set to YES, and a table or partition with an index in the Unusable state is encountered, then the load of that table or partition proceeds anyway, as if the unusable index did not exist.
    /// If SKIP_UNUSABLE_INDEXES is set to NO, and a table or partition with an index in the Unusable state is encountered, then that table or partition is not loaded. Other tables, with indexes not previously set Unusable, continue to be updated as rows are inserted.
    ///
    /// If the SKIP_UNUSABLE_INDEXES parameter is not specified, then the setting of the Oracle Database configuration parameter, SKIP_UNUSABLE_INDEXES (whose default value is y), will be used to determine how to handle unusable indexes.
    ///
    /// If indexes used to enforce constraints are marked unusable, then the data is not imported into that table.
    ///
    /// Note:
    ///     This parameter is useful only when importing data into an existing table. It has no practical effect when a table is created as part of an import because in that case, the table and indexes are newly created and will not be marked unusable.
    /// </remarks>
    /// <example>
    /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp LOGFILE=skip.log SKIP_UNUSABLE_INDEXES=YES
    /// </example>
    [Parameter("SKIP_UNUSABLE_INDEXES")]
    public bool? SkipUnusableIndexes { get; set; }

    /// <summary>
    /// Specifies a file into which all of the SQL DDL that Import would have executed, based on other parameters, is written.
    /// </summary>
    /// <remarks>
    /// The file_name specifies where the import job will write the DDL that would be executed during the job. The SQL is not actually executed, and the target system remains unchanged.
    /// The file is written to the directory object specified in the DIRECTORY parameter, unless another directory_object is explicitly specified here.
    /// Any existing file that has a name matching the one specified with this parameter is overwritten.
    ///
    /// Note that passwords are not included in the SQL file.
    /// For example, if a CONNECT statement is part of the DDL that was executed, then it will be replaced by a comment with only the schema name shown.
    /// In the following example, the dashes (--) indicate that a comment follows, and the hr schema name is shown, but not the password.
    ///      -- CONNECT hr
    ///
    /// Therefore, before you can execute the SQL file, you must edit it by removing the dashes indicating a comment and adding the password for the hr schema.
    ///
    /// For Streams and other Oracle database options, anonymous PL/SQL blocks may appear within the SQLFILE output. They should not be executed directly.
    ///
    /// Restrictions
    ///
    ///     If SQLFILE is specified, then the CONTENT parameter is ignored if it is set to either ALL or DATA_ONLY.
    ///     To perform a Data Pump Import to a SQL file using Oracle Automatic Storage Management (Oracle ASM), the SQLFILE parameter that you specify must include a directory object that does not use the Oracle ASM + notation. That is, the SQL file must be written to a disk file, not into the Oracle ASM storage.
    ///     The SQLFILE parameter cannot be used in conjunction with the QUERY parameter.
    /// </remarks>
    /// <example>
    /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp SQLFILE=dpump_dir2:expfull.sql
    /// </example>
    [Parameter("SQLFILE")]
    public string? SqlFile { get; set; }

    /// <summary>
    /// Specifies whether to import any Streams metadata that may be present in the export dump file.
    /// </summary>
    /// <remarks>
    /// Default: YES
    /// </remarks>
    /// <example>
    /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp STREAMS_CONFIGURATION=NO
    /// </example>
    [Parameter("STREAMS_CONFIGURATION")]
    public bool? StreamsConfiguration { get; set; }

    /// <summary>
    /// Tells Import what to do if the table it is trying to create already exists.
    /// </summary>
    /// <remarks>
    /// The following considerations apply when you are using these options:
    ///
    ///     When you use TRUNCATE or REPLACE, ensure that rows in the affected tables are not targets of any referential constraints.
    ///
    ///     When you use SKIP, APPEND, or TRUNCATE, existing table-dependent objects in the source, such as indexes, grants, triggers, and constraints, are not modified.
    ///     For REPLACE, the dependent objects are dropped and re-created from the source, if they were not explicitly or implicitly excluded (using EXCLUDE) and they exist in the source dump file or system.
    ///
    ///     When you use APPEND or TRUNCATE, checks are made to ensure that rows from the source are compatible with the existing table before performing any action.
    ///
    ///     If the existing table has active constraints and triggers, then it is loaded using the external tables access method.
    ///     If any row violates an active constraint, then the load fails and no data is loaded.
    ///     You can override this behavior by specifying DATA_OPTIONS=SKIP_CONSTRAINT_ERRORS on the Import command line.
    ///
    ///     If you have data that must be loaded, but may cause constraint violations, then consider disabling the constraints, loading the data, and then deleting the problem rows before reenabling the constraints.
    ///
    ///     When you use APPEND, the data is always loaded into new space; existing space, even if available, is not reused.
    ///     For this reason, you may want to compress your data after the load.
    ///
    ///     Also see the description of the Import PARTITION_OPTIONS parameter for information about how parallel processing of partitioned tables is affected depending on whether the target table already exists or not.
    ///
    /// Note:
    ///
    ///     When Data Pump detects that the source table and target table do not match (the two tables do not have the same number of columns or the target table has a column name that is not present in the source table), it compares column names between the two tables. If the tables have at least one column in common, then the data for the common columns is imported into the table (assuming the data types are compatible). The following restrictions apply:
    ///
    ///         This behavior is not supported for network imports.
    ///
    ///         The following types of columns cannot be dropped: object columns, object attributes, nested table columns, and ref columns based on a primary key.
    ///
    /// Restrictions
    ///
    ///     TRUNCATE cannot be used on clustered tables.
    /// </remarks>
    /// <example>
    /// : impdp hr TABLES=employees DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp TABLE_EXISTS_ACTION=REPLACE
    /// </example>
    [Parameter("TABLE_EXISTS_ACTION")]
    public TableExistsAction? TableExistsAction { get; set; }

    /// <summary>
    /// Specifies the database edition into which objects should be imported.
    /// </summary>
    /// <remarks>
    /// Default: the default database edition on the system
    ///
    /// If TARGET_EDITION=name is specified, then Data Pump Import creates all of the objects found in the dump file.
    /// Objects that are not editionable are created in all editions.
    /// For example, tables are not editionable, so if there is a table in the dump file, then it will be created, and all editions will see it.
    /// Objects in the dump file that are editionable, such as procedures, are created only in the specified target edition.
    ///
    /// If this parameter is not specified, then the default edition on the target database is used, even if an edition was specified in the export job.
    /// If the specified edition does not exist or is not usable, then an error message is returned.
    ///
    /// Restrictions
    ///
    ///     This parameter is only useful if there are two or more versions of the same versionable objects in the database.
    ///     The job version must be 11.2 or later. See "VERSION".
    /// </remarks>
    /// <example>
    /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=exp_dat.dmp TARGET_EDITION=exp_edition
    /// </example>
    [Parameter("TARGET_EDITION")]
    public string? TargetEdition { get; set; }

    /// <summary>
    /// Enables you to alter object creation DDL for objects being imported.
    /// </summary>
    /// <remarks>
    /// The transform_name specifies the name of the transform.
    /// The options and values are defined on <see cref="TransformFactory"/>
    ///     https://docs.oracle.com/database/121/SUTIL/GUID-64FB67BD-EB67-4F50-A4D2-5D34518E6BDB.htm#SUTIL939
    /// </remarks>
    [Parameter("TRANSFORM")]
    public Transform[]? Transform { get; set; }

    /// <summary>
    /// Specifies a list of data files to be imported into the target database by a transportable-tablespace mode import, or by a table-mode or full-mode import if TRANSPORTABLE=ALWAYS was set during the export.
    /// The data files must already exist on the target database system.
    /// </summary>
    /// <remarks>
    /// The datafile_name must include an absolute directory path specification (not a directory object name) that is valid on the system where the target database resides.
    ///
    /// At some point before the import operation, you must copy the data files from the source system to the target system. You can do this using any copy method supported by your operating stem.
    /// If desired, you can rename the files when you copy them to the target system (see Example 2).
    ///
    /// If you already have a dump file set generated by a transportable-tablespace mode export, then you can perform a transportable-mode import of that dump file, by specifying the dump file (which contains the metadata) and the TRANSPORT_DATAFILES parameter.
    /// The presence of the TRANSPORT_DATAFILES parameter tells import that it is a transportable-mode import and where to get the actual data.
    ///
    /// Depending on your operating system, the use of quotation marks when you specify a value for this parameter may also require that you use escape characters.
    /// Oracle recommends that you place this parameter in a parameter file, which can reduce the number of escape characters that might otherwise be needed on the command line.
    ///
    /// Restrictions
    ///
    ///     The TRANSPORT_DATAFILES parameter cannot be used in conjunction with the QUERY parameter.
    ///
    ///     Transportable import jobs cannot be restarted.
    /// </remarks>
    /// <example>
    /// trans_datafiles.par
    ///     DIRECTORY=dpump_dir1
    ///     DUMPFILE=tts.dmp
    ///     TRANSPORT_DATAFILES='/user01/data/tbs1.dbf'
    /// : impdp hr PARFILE=trans_datafiles.par
    /// </example>
    [Parameter("TRANSPORT_DATAFILES")]
    public string[]? TransformDataFiles { get; set; }

    public override OracleTool OracleTool => OracleTool.DataPumpImport;
}