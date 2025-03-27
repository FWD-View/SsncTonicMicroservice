using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Tonic.Common.OracleHelper.Models.DataPump;

namespace Tonic.Common.OracleHelper.Models
{
    /// <summary>
    /// Parameters available in the command-line mode of BOTH Data Pump Import and Export
    /// </summary>
    /// <remarks>
    /// EXPORT: https://docs.oracle.com/database/121/SUTIL/GUID-33880357-06B1-4CA2-8665-9D41347C6705.htm#SUTIL836
    /// IMPORT: https://docs.oracle.com/database/121/SUTIL/GUID-BA74D4F6-2840-4002-A673-0A7D9CBB3D78.htm#SUTIL903
    /// </remarks>
    public abstract record DataPumpParameters : ParametersBase
    {
        /// <summary>
        /// Used to stop the job after it is initialized. This allows the master table to be queried before any data is imported/exported.
        /// </summary>
        /// <remarks>
        /// The possible values correspond to a process order number in the master table. The result of using each number is as follows:
        ///
        ///  BOTH: n  - If the value is zero or greater, then the export operation is started and the job is aborted at the object that is stored in the master table with the corresponding process order number.
        ///  EXPORT: -1 - If the value is negative one (-1) then abort the job after setting it up, but before exporting any objects or data.
        ///  IMPORT: -1 and the job is an import using a NETWORK_LINK -- Abort the job after setting it up but before importing any objects.
        ///  IMPORT: -1 and the job is an import that does not use NETWORK_LINK -- Abort the job after loading the master table and applying filters.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=expdat.dmp SCHEMAS=hr ABORT_STEP=-1
        /// : impdp hr SCHEMAS=hr DIRECTORY=dpump_dir1 LOGFILE=schemas.log DUMPFILE=expdat.dmp ABORT_STEP=-1
        /// </example>
        [Parameter("ABORT_STEP")]
        public int? AbortStep { get; set; }

        /// <summary>
        /// Instructs Import/Export to use a particular method to load/unload data.
        /// </summary>
        /// <remarks>
        /// The ACCESS_METHOD parameter is provided so that you can try an alternative method if the default method does not work for some reason.
        /// Oracle recommends that you use the default option (AUTOMATIC) whenever possible because it allows Data Pump to automatically select the most efficient method.
        ///
        /// Restrictions
        ///
        ///     If the NETWORK_LINK parameter is also specified, then direct path mode is not supported.
        ///
        ///     The ACCESS_METHOD parameter for Data Pump Import/Export is not valid for transportable tablespace jobs.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=expdat.dmp SCHEMAS=hr ACCESS_METHOD=EXTERNAL_TABLE
        /// : impdp hr SCHEMAS=hr DIRECTORY=dpump_dir1 LOGFILE=schemas.log DUMPFILE=expdat.dmp ACCESS_METHOD=CONVENTIONAL
        /// </example>
        [Parameter("ACCESS_METHOD")]
        public AccessMethod? AccessMethod { get; set; }

        /// <summary>
        /// Attaches the client session to an existing import/export job and automatically places you in the interactive-command interface.
        /// </summary>
        /// <remarks>
        /// Default: job currently in the user's schema, if there is only one
        /// Export: displays a description of the job to which you are attached and also displays the Export prompt.
        ///
        /// The schema_name is optional. To specify a schema other than your own, you must have the DATAPUMP_EXP_FULL_DATABASE role.
        ///
        ///     The job_name is optional if only one import/export job is associated with your schema and the job is active.
        ///     To attach to a stopped job, you must supply the job name. To see a list of Data Pump job names, you can query the DBA_DATAPUMP_JOBS view or the USER_DATAPUMP_JOBS view.
        ///
        ///     When you are attached to the job, Import/Export displays a description of the job and then displays the Import/2Export prompt.
        ///
        /// Restrictions
        ///
        ///     When you specify the ATTACH parameter, the only other Data Pump parameter you can specify on the command line is ENCRYPTION_PASSWORD.
        ///
        ///     If the job you are attaching to was initially started using an encryption password, then when you attach to the job you must again enter the ENCRYPTION_PASSWORD parameter on the command line to re-specify that password.
        ///     The only exception to this is if the job was initially started with the ENCRYPTION=ENCRYPTED_COLUMNS_ONLY parameter.
        ///     In that case, the encryption password is not needed when attaching to the job.
        ///
        ///     You cannot attach to a job in another schema unless it is already running.
        ///
        ///     If the dump file set or master table for the job have been deleted, then the attach operation will fail.
        ///
        ///     Altering the master table in any way will lead to unpredictable results.
        /// </remarks>
        /// <example>
        /// : expdp hr ATTACH=hr.export_job
        /// : impdp hr ATTACH=import_job
        /// </example>
        [Parameter("ATTACH")]
        public string? Attach { get; set; }

        /// <summary>
        /// Determines whether Data Pump can use Oracle Real Application Clusters (Oracle RAC) resources and start workers on other Oracle RAC instances.
        /// </summary>
        /// <remarks>
        ///  To force Data Pump Import/Export to use only the instance where the job is started and to replicate pre-Oracle Database 11g release 2 (11.2) behavior, specify CLUSTER=NO.
        ///
        ///  To specify a specific, existing service and constrain worker processes to run only on instances defined for that service, use the SERVICE_NAME parameter with the CLUSTER=YES parameter.
        ///
        ///   Use of the CLUSTER parameter may affect performance because there is some additional overhead in distributing the import/export job across Oracle RAC instances.
        ///   For small jobs, it may be better to specify CLUSTER=NO to constrain the job to run on the instance where it is started.
        ///   Jobs whose performance benefits the most from using the CLUSTER parameter are those involving large amounts of data.
        /// </remarks>
        /// <example>
        ///  : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_clus%U.dmp CLUSTER=NO PARALLEL=3
        ///  : impdp hr DIRECTORY=dpump_dir1 SCHEMAS=hr CLUSTER=NO PARALLEL=3 NETWORK_LINK=dbs1
        /// </example>
        [Parameter("CLUSTER")]
        public bool? Cluster { get; set; }

        /// <summary>
        /// Enables you to filter what Import/Export loads/unloads: data only, metadata only, or both.
        /// </summary>
        /// <remarks>
        /// Restrictions
        ///     The CONTENT=METADATA_ONLY parameter cannot be used with the TRANSPORT_TABLESPACES (transportable-tablespace mode) parameter or with the QUERY parameter.
        ///     IMPORT: The CONTENT=ALL and CONTENT=DATA_ONLY parameter and values cannot be used in conjunction with the SQLFILE parameter.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr.dmp CONTENT=METADATA_ONLY
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp CONTENT=METADATA_ONLY
        /// </example>
        [Parameter("CONTENT")]
        public ContentMode? Content { get; set; }

        /// <summary>
        /// The DATA_OPTIONS parameter designates how certain types of data should be handled during import/export operations.
        /// </summary>
        /// <remarks>
        /// Default: There is no default. If this parameter is not used, then the special data handling options it provides simply do not take effect.
        ///
        /// Restrictions
        ///
        ///     Using the XML_CLOBS option requires that the same XML schema be used at both export and import time.
        ///
        ///     The Export DATA_OPTIONS parameter requires the job version to be set to 11.0.0 or later. See "VERSION".
        ///
        ///     If DISABLE_APPEND_HINT is used, then it can take longer for data objects to load.
        ///
        ///     If SKIP_CONSTRAINT_ERRORS is used and if a data object has unique indexes or constraints defined on it at the time of the load, then the APPEND hint will not be used for loading that data object.
        ///     Therefore, loading such data objects will take longer when the SKIP_CONSTRAINT_ERRORS option is used.
        ///
        ///     Even if SKIP_CONSTRAINT_ERRORS is specified, it is not used unless a data object is being loaded using the external table access method.
        /// </remarks>
        /// <example>
        /// : expdp hr TABLES=hr.xdb_tab1 DIRECTORY=dpump_dir1 DUMPFILE=hr_xml.dmp VERSION=11.2 DATA_OPTIONS=XML_CLOBS
        /// : impdp hr TABLES=employees CONTENT=DATA_ONLY DUMPFILE=dpump_dir1:table.dmp DATA_OPTIONS=skip_constraint_errors
        /// </example>
        [Parameter("DATA_OPTIONS")]
        public DataOptions? DataOptions { get; set; }

        /// <summary>
        /// Specifies the default location to which Import/Export can write the dump file set and the log file.
        /// </summary>
        /// <remarks>
        /// DIRECTORY=directory_object
        ///     The directory_object is the name of a database directory object (not the file path of an actual directory).
        ///     Upon installation, privileged users have access to a default directory object named DATA_PUMP_DIR.
        ///     Users with access to the default DATA_PUMP_DIR directory object do not need to use the DIRECTORY parameter at all.
        ///
        ///     A directory object specified on the DUMPFILE or LOGFILE parameter overrides any directory object that you specify for the DIRECTORY parameter.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=employees.dmp CONTENT=METADATA_ONLY
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp LOGFILE=dpump_dir2:expfull.log
        /// </example>
        [Parameter("DIRECTORY")]
        public string? Directory { get; set; }

        /// <summary>
        /// Specifies the names, and optionally, the directory objects of dump files for an import/export job.
        /// </summary>
        /// <remarks>
        /// EXPORT: The directory_object is optional if one has already been established by the DIRECTORY parameter.
        /// If you supply a value here, then it must be a directory object that already exists and that you have access to.
        /// A database directory object that is specified as part of the DUMPFILE parameter overrides a value specified by the DIRECTORY parameter or by the default directory object.
        ///
        /// EXPORT: You can supply multiple file_name specifications as a comma-delimited list or in separate DUMPFILE parameter specifications.
        /// If no extension is given for the file name, then Export uses the default file extension of .dmp.
        /// The file names can contain a substitution variable (%U), which implies that multiple files may be generated.
        /// The substitution variable is expanded in the resulting file names into a 2-digit, fixed-width, incrementing integer starting at 01 and ending at 99.
        /// If a file specification contains two substitution variables, both are incremented at the same time. For example, exp%Uaa%U.dmp would resolve to exp01aa01.dmp, exp02aa02.dmp, and so forth.
        ///
        /// EXPORT: If the FILESIZE parameter is specified, then each dump file will have a maximum of that size and be nonextensible.
        /// If more space is required for the dump file set and a template with a substitution variable (%U) was supplied, then a new dump file is automatically created of the size specified by the FILESIZE parameter, if there is room on the device.
        ///
        /// EXPORT: As each file specification or file template containing a substitution variable is defined, it is instantiated into one fully qualified file name and Export attempts to create it.
        /// The file specifications are processed in the order in which they are specified.
        /// If the job needs extra files because the maximum file size is reached, or to keep parallel workers active, then additional files are created if file templates with substitution variables were specified.
        ///
        /// EXPORT: Although it is possible to specify multiple files using the DUMPFILE parameter, the export job may only require a subset of those files to hold the exported data.
        /// The dump file set displayed at the end of the export job shows exactly which files were used.
        /// It is this list of files that is required to perform an import operation using this dump file set. Any files that were not used can be discarded.
        ///
        /// IMPORT: The directory_object is optional if one has already been established by the DIRECTORY parameter. If you do supply a value here, then it must be a directory object that already exists and that you have access to. A database directory object that is specified as part of the DUMPFILE parameter overrides a value specified by the DIRECTORY parameter.
        /// IMPORT: The file_name is the name of a file in the dump file set. The file names can also be templates that contain the substitution variable, %U. If %U is used, then Import examines each file that matches the template (until no match is found) to locate all files that are part of the dump file set. The %U expands to a 2-digit incrementing integer starting with 01.
        /// IMPORT: Sufficient information is contained within the files for Import to locate the entire set, provided the file specifications in the DUMPFILE parameter encompass the entire set. The files are not required to have the same names, locations, or order that they had at export time.
        ///
        /// Restrictions
        ///
        ///     Any resulting dump file names that match preexisting dump file names will generate an error and the preexisting dump files will not be overwritten.
        ///     You can override this behavior by specifying the Export parameter REUSE_DUMPFILES=YES.
        ///
        ///     Dump files created on Oracle Database 11g releases with the Data Pump parameter VERSION=12 can only be imported on Oracle Database 12c Release 1 (12.1) and later.
        /// </remarks>
        /// <example>
        /// : expdp hr SCHEMAS=hr DIRECTORY=dpump_dir1 DUMPFILE=dpump_dir2:exp1.dmp,exp2%U.dmp PARALLEL=3
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=dpump_dir2:exp1.dmp, exp2%U.dmp
        /// </example>
        [Parameter("DUMPFILE")]
        public string? DumpFile { get; set; }

        /// <summary>
        /// Specifies a password for encrypting/accessing encrypted column data, metadata, or table data in the export dumpfile.
        /// This prevents unauthorized access to an encrypted dump file set.
        /// </summary>
        /// <remarks>
        /// The password value that is supplied specifies a key for re-encrypting encrypted table columns, metadata, or table data so that they are not written as clear text in the dump file set.
        /// If the export operation involves encrypted table columns, but an encryption password is not supplied, then the encrypted columns are written to the dump file set as clear text and a warning is issued.
        ///
        /// The password that you enter is echoed to the screen. If you do not want the password shown on the screen as you enter it, then use the ENCRYPTION_PWD_PROMPT parameter.
        ///
        ///     The maximum length allowed for an encryption password depends on the option specified on the ENCRYPTION parameter. If ENCRYPTION=ENCRYPTED_COLUMNS_ONLY is specified, then the maximum length of the encryption password is 30 bytes.
        /// If the ENCRYPTION parameter is specified as ALL, DATA_ONLY, or METADATA_ONLY, or if the default is used, then the maximum length of the encryption password is 128 bytes.
        ///
        ///    For export operations, this parameter is required if the ENCRYPTION_MODE parameter is set to either PASSWORD or DUAL.
        ///
        /// Default: There is no default; the value is user-provided.
        ///
        /// Restrictions
        ///
        ///     This parameter is valid only in the Enterprise Edition of Oracle Database 11g or later.
        ///
        ///     The ENCRYPTION_PASSWORD parameter is required for the transport of encrypted tablespaces and tablespaces containing tables with encrypted columns in a full transportable export.
        ///
        ///     Data Pump encryption features require that the Oracle Advanced Security option be enabled.
        ///     See Oracle Database Licensing Information for information about licensing requirements for the Oracle Advanced Security option.
        ///
        ///     If ENCRYPTION_PASSWORD is specified but ENCRYPTION_MODE is not specified, then it is not necessary to have Oracle Advanced Security Transparent Data Encryption enabled since ENCRYPTION_MODE will default to PASSWORD.
        ///
        ///     The ENCRYPTION_PASSWORD parameter is not valid if the requested encryption mode is/was TRANSPARENT.
        ///
        ///     The ENCRYPTION_PASSWORD parameter is required for network-based full transportable imports where the source database has encrypted tablespaces or tables with encrypted columns.
        ///
        ///     To use the ENCRYPTION_PASSWORD parameter if ENCRYPTION_MODE is set to DUAL, you must have Oracle Advanced Security Transparent Data Encryption (TDE) enabled.
        ///     See Oracle Database Advanced Security Guide for more information about TDE.
        ///
        ///     For network exports, the ENCRYPTION_PASSWORD parameter in conjunction with ENCRYPTION=ENCRYPTED_COLUMNS_ONLY is not supported with user-defined external tables that have encrypted columns.
        ///     The table will be skipped and an error message will be displayed, but the job will continue.
        ///
        ///     Encryption attributes for all columns must match between the exported table definition and the target table. For example, suppose you have a table, EMP, and one of its columns is named EMPNO.
        ///     Both of the following situations would result in an error because the encryption attribute for the EMP column in the source table would not match the encryption attribute for the EMP column in the target table:
        ///         The EMP table is exported with the EMPNO column being encrypted, but before importing the table you remove the encryption attribute from the EMPNO column.
        ///         The EMP table is exported without the EMPNO column being encrypted, but before importing the table you enable encryption on the EMPNO column.
        ///
        ///     Data Pump encryption functionality changed as of Oracle Database 11g release 1 (11.1). Before release 11.1, the ENCRYPTION_PASSWORD parameter applied only to encrypted columns.
        ///     However, as of release 11.1, the new ENCRYPTION parameter provides options for encrypting other types of data.
        ///     This means that if you now specify ENCRYPTION_PASSWORD without also specifying ENCRYPTION and a specific option, then all data written to the dump file will be encrypted (equivalent to specifying ENCRYPTION=ALL).
        ///     If you want to re-encrypt only encrypted columns, then you must now specify ENCRYPTION=ENCRYPTED_COLUMNS_ONLY in addition to ENCRYPTION_PASSWORD.
        /// </remarks>
        /// <example>
        /// : expdp hr TABLES=employee_s_encrypt DIRECTORY=dpump_dir1 DUMPFILE=dpcd2be1.dmp ENCRYPTION=ENCRYPTED_COLUMNS_ONLY ENCRYPTION_PASSWORD=123456
        /// : impdp hr TABLES=employee_s_encrypt DIRECTORY=dpump_dir DUMPFILE=dpcd2be1.dmp ENCRYPTION_PASSWORD=123456
        /// </example>
        [JsonIgnore] //prevent accidentally leaking this property into logs
        [Parameter("ENCRYPTION_PASSWORD")]
        public string? EncryptionPassword { get; set; }

        /// <summary>
        /// Specifies the method that Import/Export will use to estimate how much disk space each table in the export job will consume (in bytes) or
        /// how much data will be generated by an import job (in bytes)
        /// </summary>
        /// <remarks>
        /// Default: BLOCKS
        ///
        /// IMPORT: The estimate that is generated can be used to determine a percentage complete throughout the execution of the import job.
        /// EXPORT: The estimate is printed in the log file and displayed on the client's standard output device.
        /// EXPORT: The estimate is for table row data only; it does not include metadata.
        ///
        /// Restrictions
        ///
        ///     If the Data Pump export job involves compressed tables, then the default size estimation given for the compressed table is inaccurate when ESTIMATE=BLOCKS is used.
        ///     This is because the size estimate does not reflect that the data was stored in a compressed form. To get a more accurate size estimate for compressed tables, use ESTIMATE=STATISTICS.
        ///
        ///     The Import ESTIMATE parameter is valid only if the NETWORK_LINK parameter is also specified.
        ///
        ///     When the import source is a dump file set, the amount of data to be loaded is already known, so the percentage complete is automatically calculated.
        ///
        ///     The estimate may also be inaccurate if either the QUERY or REMAP_DATA parameter is used.
        /// </remarks>
        /// <example>
        /// : expdp hr TABLES=employees ESTIMATE=STATISTICS DIRECTORY=dpump_dir1 DUMPFILE=estimate_stat.dmp
        /// : impdp hr TABLES=job_history NETWORK_LINK=source_database_link DIRECTORY=dpump_dir1 ESTIMATE=STATISTICS
        /// </example>
        [Parameter("ESTIMATE")]
        public EstimateMethod? Estimate { get; set; }

        /// <summary>
        /// Enables you to filter the metadata that is imported/exported by specifying objects and object types to be excluded from the import/export operation.
        /// </summary>
        /// <remarks>
        /// The object_type specifies the type of object to be excluded.
        /// To see a list of valid values for object_type, query the following views: DATABASE_EXPORT_OBJECTS for full mode, SCHEMA_EXPORT_OBJECTS for schema mode, and TABLE_EXPORT_OBJECTS for table and tablespace mode.
        /// The values listed in the OBJECT_PATH column are the valid object types. (See "Metadata Filters" for an example of how to perform such a query.)
        ///
        /// All object types for the given mode of import/export are included in the import/export except those specified in an EXCLUDE statement.
        /// If an object is excluded, then all of its dependent objects are also excluded.
        /// For example, excluding a table will also exclude all indexes and triggers on the table.
        ///
        /// The name_clause is optional.
        /// It allows selection of specific objects within an object type.
        /// It is a SQL expression used as a filter on the type's object names.
        /// It consists of a SQL operator and the values against which the object names of the specified type are to be compared.
        /// The name_clause applies only to object types whose instances have names (for example, it is applicable to TABLE, but not to GRANT).
        /// It must be separated from the object type with a colon and enclosed in double quotation marks, because single quotation marks are required to delimit the name strings.
        /// For example, you could set EXCLUDE=INDEX:"LIKE 'EMP%'" to exclude all indexes whose names start with EMP.
        ///
        /// The name that you supply for the name_clause must exactly match, including upper and lower casing, an existing object in the database.
        /// For example, if the name_clause you supply is for a table named EMPLOYEES, then there must be an existing table named EMPLOYEES using all upper case.
        /// If the name_clause were supplied as Employees or employees or any other variation, then the table would not be found.
        ///
        /// If no name_clause is provided, then all objects of the specified type are excluded.
        ///
        /// More than one EXCLUDE statement can be specified.
        ///
        /// Depending on your operating system, the use of quotation marks when you specify a value for this parameter may also require that you use escape characters.
        /// Oracle recommends that you place this parameter in a parameter file, which can reduce the number of escape characters that might otherwise be needed on the command line.
        ///
        /// If the object_type you specify is CONSTRAINT, GRANT, or USER, then you should be aware of the effects, as described in the following paragraphs.
        ///
        /// Excluding Constraints
        ///
        ///     The following constraints cannot be explicitly excluded:
        ///
        ///         Constraints needed for the table to be created and loaded successfully; for example, primary key constraints for index-organized tables, or REF SCOPE and WITH ROWID constraints for tables with REF columns
        ///
        ///     This means that the following EXCLUDE statements are interpreted as follows:
        ///
        ///         EXCLUDE=CONSTRAINT excludes all constraints, except for any constraints needed for successful table creation and loading.
        ///
        ///         EXCLUDE=REF_CONSTRAINT excludes referential integrity (foreign key) constraints.
        ///
        /// Excluding Grants and Users
        ///
        ///     Specifying EXCLUDE=GRANT excludes object grants on all object types and system privilege grants.
        ///
        ///     Specifying EXCLUDE=USER excludes only the definitions of users, not the objects contained within users' schemas.
        ///
        ///     To exclude a specific user and all objects of that user, specify a command such as the following, where hr is the schema name of the user you want to exclude.
        ///
        ///     expdp FULL=YES DUMPFILE=expfull.dmp EXCLUDE=SCHEMA:"='HR'"
        ///
        ///     Note that in this situation, an export mode of FULL is specified. If no mode were specified, then the default mode, SCHEMAS, would be used. This would cause an error because the command would indicate that the schema should be both exported and excluded at the same time.
        ///
        ///     If you try to exclude a user by using a statement such as EXCLUDE=USER:"='HR'", then only the information used in CREATE USER hr DDL statements is excluded, and you may not get the results you expect.
        ///
        /// Restrictions
        ///
        ///     The EXCLUDE and INCLUDE parameters are mutually exclusive.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_exclude.dmp EXCLUDE=VIEW,PACKAGE, FUNCTION
        /// : impdp system DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp PARFILE=exclude.par
        /// </example>
        [Parameter("EXCLUDE")]
        public OracleObjectType[]? Exclude { get; set; }

        /// <summary>
        /// Specifies the system change number (SCN) that Import/Export will use to enable the Flashback Query utility.
        /// </summary>
        /// <remarks>
        /// SCN is a 6 byte (48 bits) number.
        ///
        /// The import/export operation is performed with data that is consistent up to the specified SCN.
        /// If the NETWORK_LINK parameter is specified, then the SCN refers to the SCN of the source database.
        ///
        /// Default: There is no default
        ///
        /// Restrictions
        ///
        ///     FLASHBACK_SCN and FLASHBACK_TIME are mutually exclusive.
        ///
        ///     The FLASHBACK_SCN parameter pertains only to the Flashback Query capability of Oracle Database.
        ///     It is not applicable to Flashback Database, Flashback Drop, or Flashback Data Archive.
        ///
        ///     IMPORT: The FLASHBACK_SCN parameter is valid only when the NETWORK_LINK parameter is also specified.
        /// Note
        ///
        ///     If you are on a logical standby system and using a network link to access the logical standby primary, then the FLASHBACK_SCN parameter is ignored because SCNs are selected by logical standby.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_scn.dmp FLASHBACK_SCN=384632
        /// : impdp hr DIRECTORY=dpump_dir1 FLASHBACK_SCN=123456 NETWORK_LINK=source_database_link
        /// </example>
        [Parameter("FLASHBACK_SCN")]
        public long? FlashbackSystemChangeNumber { get; set; }

        /// <summary>
        /// The SCN that most closely matches the specified time is found, and this SCN is used to enable the Flashback utility.
        /// </summary>
        /// <remarks>
        /// SCN is a 6 byte (48 bits) number.
        ///
        /// The import/export operation is performed with data that is consistent up to the specified SCN.
        /// If the NETWORK_LINK parameter is specified, then the SCN refers to the SCN of the source database.
        ///
        /// Default: There is no default
        ///
        /// You can specify the time in any format that the DBMS_FLASHBACK.ENABLE_AT_TIME procedure accepts.
        ///
        /// Restrictions
        ///
        ///     FLASHBACK_TIME and FLASHBACK_SCN are mutually exclusive.
        ///
        ///     The FLASHBACK_TIME parameter pertains only to the flashback query capability of Oracle Database. It is not applicable to Flashback Database, Flashback Drop, or Flashback Data Archive.
        ///
        ///     IMPORT: The FLASHBACK_SCN parameter is valid only when the NETWORK_LINK parameter is also specified.
        ///
        /// Note
        ///
        ///     If you are on a logical standby system and using a network link to access the logical standby primary, then the FLASHBACK_SCN parameter is ignored because SCNs are selected by logical standby.
        /// </remarks>
        /// <example>
        /// flashback.par
        ///     DIRECTORY=dpump_dir1
        ///     DUMPFILE=hr_time.dmp
        ///     FLASHBACK_TIME="TO_TIMESTAMP('27-10-2012 13:16:00', 'DD-MM-YYYY HH24:MI:SS')"
        /// : expdp hr PARFILE=flashback.par
        /// flashback_imp.par
        ///     FLASHBACK_TIME="TO_TIMESTAMP()"
        /// : impdp hr DIRECTORY=dpump_dir1 PARFILE=flashback_imp.par NETWORK_LINK=source_database_link
        /// </example>
        [Parameter("FLASHBACK_TIME")]
        public string? FlashbackTime { get; set; }

        /// <summary>
        /// Specifies that you want to perform a full database mode export.
        /// </summary>
        /// <remarks>
        /// FULL=YES indicates that all data and metadata are to be imported/exported.
        ///
        /// EXPORT: To perform a full export, you must have the DATAPUMP_EXP_FULL_DATABASE role.
        /// EXPORT: Default: No
        /// EXPORT: Filtering can restrict what is exported using this export mode. See "Filtering During Export Operations".
        /// EXPORT: You can perform a full mode export using the transportable option (TRANSPORTABLE=ALWAYS).
        /// EXPORT: This is referred to as a full transportable export, which exports all objects and data necessary to create a complete copy of the database. See "Using the Transportable Option During Full Mode Exports".
        ///
        /// IMPORT The source can be a dump file set for a file-based import or it can be another database, specified with the NETWORK_LINK parameter, for a network import.
        /// IMPORT: Default: Yes
        /// IMPORT: If you are importing from a file and do not have the DATAPUMP_IMP_FULL_DATABASE role, then only schemas that map to your own schema are imported.
        /// IMPORT: If the NETWORK_LINK parameter is used and the user executing the import job has the DATAPUMP_IMP_FULL_DATABASE role on the target database, then that user must also have the DATAPUMP_EXP_FULL_DATABASE role on the source database.
        /// IMPORT: Filtering can restrict what is imported using this import mode (see "Filtering During Import Operations").
        /// IMPORT: FULL is the default mode, and does not need to be specified on the command line when you are performing a file-based import, but if you are performing a network-based full import then you must specify FULL=Y on the command line.
        /// IMPORT: You can use the transportable option during a full-mode import to perform a full transportable import. See "Using the Transportable Option During Full Mode Imports".
        ///
        /// Restrictions
        ///
        ///    EXPORT: To use the FULL parameter in conjunction with TRANSPORTABLE (a full transportable export), either the Data Pump VERSION parameter must be set to at least 12.0. or the COMPATIBLE database initialization parameter must be set to at least 12.0 or later.
        ///    EXPORT: A full export does not, by default, export system schemas that contain Oracle-managed data and metadata. Examples of system schemas that are not exported by default include SYS, ORDSYS, and MDSYS.
        ///    EXPORT: Grants on objects owned by the SYS schema are never exported.
        ///    EXPORT: A full export operation exports objects from only one database edition; by default it exports the current edition but you can use the Export SOURCE_EDITION parameter to specify a different edition.
        ///    EXPORT: If you are exporting data that is protected by a realm, then you must have authorization for that realm.
        ///    EXPORT: The Automatic Workload Repository (AWR) is not moved in a full database export and import operation. (See Oracle Database Performance Tuning Guide for information about using Data Pump to move AWR snapshots.)
        ///    EXPORT: The XDB repository is not moved in a full database export and import operation. User created XML schemas are moved.
        ///
        ///    IMPORT: The Automatic Workload Repository (AWR) is not moved in a full database export and import operation. (See Oracle Database Performance Tuning Guide for information about using Data Pump to move AWR snapshots.)
        ///    IMPORT: The XDB repository is not moved in a full database export and import operation. User created XML schemas are moved.
        ///    IMPORT: Full imports performed over a network link require that you set VERSION=12 if the target is Oracle Database 12c Release 1 (12.1.0.1) or later and the source is Oracle Database 11g Release 2 (11.2.0.3) or later.
        ///
        /// Note:
        ///
        ///     Be aware that when you later import a dump file that was created by a full-mode export, the import operation attempts to copy the password for the SYS account from the source database.
        ///     This sometimes fails (for example, if the password is in a shared password file).
        ///     If it does fail, then after the import completes, you must set the password for the SYS account at the target database to a password of your choice.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir2 DUMPFILE=expfull.dmp FULL=YES NOLOGFILE=YES
        /// : impdp hr DUMPFILE=dpump_dir1:expfull.dmp FULL=YES LOGFILE=dpump_dir2:full_imp.log
        /// </example>
        [Parameter("FULL")]
        public bool? Full { get; set; }

        /// <summary>
        /// Displays online help for the Import/Export utility.
        /// </summary>
        /// <remarks>
        /// If HELP=YES is specified, then Import/Export displays a summary of all Import/Export command-line parameters and interactive commands.
        /// </remarks>
        /// <example>
        /// : expdp HELP = YES
        /// : impdp HELP = YES
        /// </example>
        [Parameter("HELP")]
        public bool? Help { get; set; }

        /// <summary>
        /// Enables you to filter the metadata that is imported/exported by specifying objects and object types for the current import/export mode.
        /// </summary>
        /// <remarks>
        /// EXPORT: The specified objects and all their dependent objects are exported. Grants on these objects are also exported.
        ///
        /// EXPORT: The object_type specifies the type of object to be included.
        /// To see a list of valid values for object_type, query the following views: DATABASE_EXPORT_OBJECTS for full mode, SCHEMA_EXPORT_OBJECTS for schema mode, and TABLE_EXPORT_OBJECTS for table and tablespace mode.
        /// The values listed in the OBJECT_PATH column are the valid object types. (See "Metadata Filters" for an example of how to perform such a query.)
        ///
        /// IMPORT: Only object types in the source (and their dependents) that are explicitly specified in the INCLUDE statement are imported.
        ///
        /// The name_clause is optional. It allows fine-grained selection of specific objects within an object type. It is a SQL expression used as a filter on the object names of the type. It consists of a SQL operator and the values against which the object names of the specified type are to be compared. The name_clause applies only to object types whose instances have names (for example, it is applicable to TABLE, but not to GRANT). It must be separated from the object type with a colon and enclosed in double quotation marks, because single quotation marks are required to delimit the name strings.
        ///
        /// The name that you supply for the name_clause must exactly match, including upper and lower casing, an existing object in the database. For example, if the name_clause you supply is for a table named EMPLOYEES, then there must be an existing table named EMPLOYEES using all upper case. If the name_clause were supplied as Employees or employees or any other variation, then the table would not be found.
        ///
        /// More than one INCLUDE statement can be specified.
        ///
        /// Including Constraints
        ///
        ///     If the object_type you specify is a CONSTRAINT, then you should be aware of the effects this will have.
        ///
        ///     The following constraints cannot be explicitly included:
        ///         NOT NULL constraints
        ///         Constraints needed for the table to be created and loaded successfully; for example, primary key constraints for index-organized tables, or REF SCOPE and WITH ROWID constraints for tables with REF columns
        ///
        ///     This means that the following INCLUDE statements will be interpreted as follows:
        ///         INCLUDE=CONSTRAINT will include all (nonreferential) constraints, except for NOT NULL constraints and any constraints needed for successful table creation and loading
        ///         INCLUDE=REF_CONSTRAINT will include referential integrity (foreign key) constraints
        ///
        /// Restrictions
        ///
        ///     The INCLUDE and EXCLUDE parameters are mutually exclusive.
        ///
        /// Grants on objects owned by the SYS schema are never exported.
        /// </remarks>
        /// <example>
        /// hr.par
        ///     SCHEMAS=HR
        ///     DUMPFILE=expinclude.dmp
        ///     DIRECTORY=dpump_dir1
        ///     LOGFILE=expinclude.log
        ///     INCLUDE=TABLE:"IN ('EMPLOYEES', 'DEPARTMENTS')"
        ///     INCLUDE=PROCEDURE
        ///     INCLUDE=INDEX:"LIKE 'EMP%'"
        /// : expdp hr PARFILE=hr.par
        /// imp_include.par
        ///     INCLUDE=FUNCTION
        ///     INCLUDE=PROCEDURE
        ///     INCLUDE=PACKAGE
        ///     INCLUDE=INDEX:"LIKE 'EMP%' "
        /// : impdp system SCHEMAS=hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp PARFILE=imp_include.par
        /// </example>
        [Parameter("INCLUDE")]
        public OracleObjectType[]? Include { get; set; }

        /// <summary>
        /// Used to identify the import/export job in subsequent actions, such as when the ATTACH parameter is used to attach to a job, or to identify the job using the DBA_DATAPUMP_JOBS or USER_DATAPUMP_JOBS views.
        /// </summary>
        /// <remarks>
        /// Default: system-generated name of the form SYS_&lt;EXPORT or IMPORT or SQLFILE&gt;_&lt;mode&gt;_NN
        ///
        /// The jobname_string specifies a name of up to 30 bytes for this export job. The bytes must represent printable characters and spaces.
        /// If spaces are included, then the name must be enclosed in single quotation marks (for example, 'Thursday Export').
        /// The job name is implicitly qualified by the schema of the user performing the export operation.
        /// The job name is used as the name of the master table, which controls the export job.
        ///
        /// The default job name is system-generated in the form SYS_EXPORT_&lt;mode&gt;_NN, where NN expands to a 2-digit incrementing integer starting at 01. An example of a default name is 'SYS_EXPORT_TABLESPACE_02'.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=exp_job.dmp JOB_NAME=exp_job NOLOGFILE=YES
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp JOB_NAME=impjob01
        /// </example>
        [Parameter("JOB_NAME")]
        public string? JobName { get; set; }

        /// <summary>
        /// Indicates whether the master table should be deleted or retained at the end of a Data Pump job that completes successfully.
        /// The master table is automatically retained for jobs that do not complete successfully.
        /// </summary>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=expdat.dmp SCHEMAS=hr KEEP_MASTER=YES
        /// : impdp hr SCHEMAS=hr DIRECTORY=dpump_dir1 LOGFILE=schemas.log DUMPFILE=expdat.dmp KEEP_MASTER=YES
        /// </example>
        [Parameter("KEEP_MASTER")]
        public bool? KeepMaster { get; set; }

        /// <summary>
        /// Specifies the name, and optionally, a directory, for the log file of the IMPORT/export job.
        /// </summary>
        /// <remarks>
        /// EXPORT: Default: export.log
        /// EXPORT: The default behavior is to create a file named export.log in the directory referenced by the directory object specified in the DIRECTORY parameter.
        /// IMPORT: Default: import.log
        /// IMPORT: The default behavior is to create import.log in the directory referenced by the directory object specified in the DIRECTORY parameter.
        ///
        /// You can specify a database directory_object previously established by the DBA, assuming that you have access to it. This overrides the directory object specified with the DIRECTORY parameter.
        ///
        /// The file_name specifies a name for the log file.
        ///
        /// All messages regarding work in progress, work completed, and errors encountered are written to the log file. (For a real-time status of the job, use the STATUS command in interactive mode.)
        ///
        /// A log file is always created for an export job unless the NOLOGFILE parameter is specified. As with the dump file set, the log file is relative to the server and not the client.
        ///
        /// An existing file matching the file name will be overwritten.
        ///
        /// Restrictions
        ///
        ///     To perform a Data Pump Import/Export using Oracle Automatic Storage Management (Oracle ASM), you must specify a LOGFILE parameter that includes a directory object that does not include the Oracle ASM + notation.
        ///     That is, the log file must be written to a disk file, and not written into the Oracle ASM storage. Alternatively, you can specify NOLOGFILE=YES.
        ///     However, this prevents the writing of the log file.
        ///
        /// Note:
        ///
        ///     Data Pump Import/Export writes the log file using the database character set.
        ///     If your client NLS_LANG environment setting sets up a different client character set from the database character set, then it is possible that table names may be different in the log file than they are when displayed on the client output screen.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr.dmp LOGFILE=hr_export.log
        /// : impdp hr SCHEMAS=HR DIRECTORY=dpump_dir2 LOGFILE=imp.log DUMPFILE=dpump_dir1:expfull.dmp
        /// </example>
        [Parameter("LOGFILE")]
        public string? LogFile { get; set; }

        /// <summary>
        /// Specifies that messages displayed during import/export operations be timestamped.
        /// </summary>
        /// <remarks>
        /// You can use the timestamps to figure out the elapsed time between different phases of a Data Pump operation.
        /// Such information can be helpful in diagnosing performance problems and estimating the timing of future similar operations.
        ///
        /// Default: No timestamps are recorded
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=expdat.dmp SCHEMAS=hr LOGTIME=ALL
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expdat.dmp SCHEMAS=hr LOGTIME=ALL TABLE_EXISTS_ACTION=REPLACE
        /// </example>
        [Parameter("LOGTIME")]
        public LogTimeBehavior? LogTime { get; set; }

        /// <summary>
        /// Indicates whether additional information about the job should be reported to the Data Pump log file.
        /// </summary>
        /// <remarks>
        /// When METRICS=YES is used, the number of objects and the elapsed time are recorded in the Data Pump log file.
        ///
        /// Default: NO
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=expdat.dmp SCHEMAS=hr METRICS=YES
        /// : impdp hr SCHEMAS=hr DIRECTORY=dpump_dir1 LOGFILE=schemas.log DUMPFILE=expdat.dmp METRICS=YES
        /// </example>
        [Parameter("METRICS")]
        public bool? Metrics { get; set; }

        /// <summary>
        /// Enables an import/export from a (source) database identified by a valid database link.
        /// </summary>
        /// <remarks>
        /// Default: There is no default
        ///
        /// EXPORT: The data from the source database instance is written to a dump file set on the connected database instance.
        /// EXPORT: This means that the system to which the expdp client is connected contacts the source database referenced by the source_database_link, retrieves data from it, and writes the data to a dump file set back on the connected system.
        ///
        /// IMPORT: The data from the source database instance is written directly back to the connected database instance.
        /// IMPORT: This means that the system to which the impdp client is connected contacts the source database referenced by the source_database_link, retrieves data from it, and writes the data directly to the database on the connected instance. There are no dump files involved.
        ///
        /// The NETWORK_LINK parameter initiates an import/export using a database link.
        ///
        /// The source_database_link provided must be the name of a database link to an available database.
        /// If the database on that instance does not already have a database link, then you or your DBA must create one using the SQL CREATE DATABASE LINK statement.
        ///
        /// IMPORT: When you perform a network import using the transportable method, you must copy the source data files to the target database before you start the import.
        ///
        /// If the source database is read-only, then the user on the source database must have a locally managed temporary tablespace assigned as the default temporary tablespace. Otherwise, the job will fail.
        ///
        /// The following types of database links are supported for use with Data Pump Import/Export:
        ///
        ///     Public fixed user
        ///     Public connected user
        ///     Public shared user (only when used by link owner)
        ///     Private shared user (only when used by link owner)
        ///     Private fixed user (only when used by link owner)
        ///
        /// Caution:
        ///
        ///     If an import/export operation is performed over an unencrypted network link, then all data is exported as clear text even if it is encrypted in the database.
        ///     See Oracle Database Security Guide for more information about network security.
        ///
        /// Restrictions
        ///
        ///     The following types of database links are not supported for use with Data Pump Export:
        ///
        ///         Private connected user
        ///         Current user
        ///
        ///     Network imports do not support the use of evolved types.
        ///     Network imports/exports do not support LONG columns.
        ///
        ///     When transporting a database over the network using full transportable import/export, tables with LONG or LONG RAW columns that reside in administrative tablespaces (such as SYSTEM or SYSAUX) are not supported.
        ///
        ///     When transporting a database over the network using full transportable import/export, auditing cannot be enabled for tables stored in an administrative tablespace (such as SYSTEM and SYSAUX) if the audit trail information itself is stored in a user-defined tablespace.
        ///
        ///     When operating across a network link, Data Pump requires that the source and target databases differ by no more than two versions.
        ///     For example, if one database is Oracle Database 12c, then the other database must be 12c, 11g, or 10g.
        ///     Note that Data Pump checks only the major version number (for example, 10g,11g, 12c), not specific release numbers (for example, 12.1,10.1, 10.2, 11.1, or 11.2).
        ///
        ///     IMPORT: If the USERID that is executing the import job has the DATAPUMP_IMP_FULL_DATABASE role on the target database, then that user must also have the DATAPUMP_EXP_FULL_DATABASE role on the source database.
        ///
        ///     IMPORT: Network mode import does not use parallel query (PQ) slaves. See "Using PARALLEL During a Network Mode Import".
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 NETWORK_LINK=source_database_link DUMPFILE=network_export.dmp LOGFILE=network_export.log
        /// : impdp hr TABLES=employees DIRECTORY=dpump_dir1 NETWORK_LINK=source_database_link EXCLUDE=CONSTRAINT
        /// </example>
        [Parameter("NETWORK_LINK")]
        public string? NetworkLink { get; set; }

        /// <summary>
        /// Specifies whether to suppress creation of a log file.
        /// </summary>
        /// <remarks>
        /// Specify NOLOGFILE =YES to suppress the default behavior of creating a log file.
        /// Progress and error information is still written to the standard output device of any attached clients, including the client that started the original export operation.
        /// If there are no clients attached to a running job and you specify NOLOGFILE=YES, then you run the risk of losing important progress and error information.
        /// Default: NO
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr.dmp NOLOGFILE=YES
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp NOLOGFILE=YES
        /// </example>
        [Parameter("NOLOGFILE")]
        public bool? NoLogFile { get; set; }

        /// <summary>
        /// Specifies the maximum number of processes of active execution operating on behalf of the import/export job.
        /// This execution set consists of a combination of worker processes and parallel I/O server processes.
        /// The master control process and worker processes acting as query coordinators in parallel query operations do not count toward this total.
        /// </summary>
        /// <remarks>
        /// This parameter enables you to make trade-offs between resource consumption and elapsed time.
        ///
        /// EXPORT: The value you specify for integer should be less than, or equal to, the number of files in the dump file set (or you should specify substitution variables in the dump file specifications).
        ///
        /// EXPORT: Because each active worker process or I/O server process writes exclusively to one file at a time, an insufficient number of files can have adverse effects.
        /// Some of the worker processes will be idle while waiting for files, thereby degrading the overall performance of the job.
        /// More importantly, if any member of a cooperating group of parallel I/O server processes cannot obtain a file for output, then the export operation will be stopped with an ORA-39095 error.
        /// Both situations can be corrected by attaching to the job using the Data Pump Export utility, adding more files using the ADD_FILE command while in interactive mode, and in the case of a stopped job, restarting the job.
        ///
        /// IMPORT: The value you specify for integer specifies the maximum number of processes of active execution operating on behalf of the import job.
        /// This execution set consists of a combination of worker processes and parallel I/O server processes.
        /// The master control process, idle workers, and worker processes acting as parallel execution coordinators in parallel I/O operations do not count toward this total.
        /// This parameter enables you to make trade-offs between resource consumption and elapsed time.
        ///
        /// IMPORT: If the source of the import is a dump file set consisting of files, then multiple processes can read from the same file, but performance may be limited by I/O contention.
        /// To increase or decrease the value of PARALLEL during job execution, use interactive-command mode.
        ///
        /// Decreasing parallelism does not result in fewer worker processes associated with the job; it decreases the number of worker processes that will be executing at any given time.
        /// Also, any ongoing work must reach an orderly completion point before the decrease takes effect.
        /// Therefore, it may take a while to see any effect from decreasing the value. Idle workers are not deleted until the job exits.
        ///
        /// Increasing the parallelism takes effect immediately if there is work that can be performed in parallel.
        ///
        /// IMPORT: Parallelism is used for loading user data and package bodies, and for building indexes.
        ///
        /// Default: 1
        ///
        /// Restrictions
        ///
        ///     This parameter is valid only in the Enterprise Edition of Oracle Database 11g or later.
        ///
        ///     To import/export a table or table partition in parallel (using PQ slaves), you must have the DATAPUMP_EXP_FULL_DATABASE role.
        ///
        /// Using PARALLEL During a Network Mode Import
        ///
        ///     During a network mode import, the PARALLEL parameter defines the maximum number of worker processes that can be assigned to the job.
        ///     To understand the effect of the PARALLEL parameter during a network import mode, it is important to understand the concept of "table_data objects" as defined by Data Pump. When Data Pump moves data, it considers the following items to be individual "table_data objects":
        ///
        ///         a complete table (one that is not partitioned or subpartitioned)
        ///         partitions, if the table is partitioned but not subpartitioned
        ///         subpartitions, if the table is subpartitioned
        ///
        ///     During a network mode import, each table_data object is assigned its own worker process, up to the value specified for the PARALLEL parameter. No parallel query (PQ) slaves are assigned because network mode import does not use parallel query (PQ) slaves. Multiple table_data objects can be unloaded at the same time, but each table_data object is unloaded using a single process.
        ///
        /// Using PARALLEL During An Export In An Oracle RAC Environment
        ///
        ///     In an Oracle Real Application Clusters (Oracle RAC) environment, if an export operation has PARALLEL=1, then all Data Pump processes reside on the instance where the job is started. Therefore, the directory object can point to local storage for that instance.
        ///
        ///     If the export operation has PARALLEL set to a value greater than 1, then Data Pump processes can reside on instances other than the one where the job was started. Therefore, the directory object must point to shared storage that is accessible by all instances of the Oracle RAC.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 LOGFILE=parallel_export.log JOB_NAME=par4_job DUMPFILE=par_exp%u.dmp PARALLEL=4
        /// : impdp hr DIRECTORY=dpump_dir1 LOGFILE=parallel_import.log JOB_NAME=imp_par3 DUMPFILE=par_exp%U.dmp PARALLEL=3
        /// </example>
        [Parameter("PARALLEL")]
        public int? Parallel { get; set; }

        /// <summary>
        /// Specifies the name of an import/export parameter file.
        /// </summary>
        /// <remarks>
        /// A parameter file allows you to specify Data Pump parameters within a file, and then that file can be specified on the command line instead of entering all the individual commands.
        /// This can be useful if you use the same parameter combination many times.
        /// The use of parameter files is also highly recommended if you are using parameters whose values require the use of quotation marks.
        ///
        /// A directory object is not specified for the parameter file because unlike dump files, log files, and SQL files which are created and written by the server, the parameter file is opened and read by the impdp/expdp client.
        /// The default location of the parameter file is the user's current directory.
        ///
        /// Within a parameter file, a comma is implicit at every newline character so you do not have to enter commas at the end of each line.
        /// If you have a long line that wraps, such as a long table name, enter the backslash continuation character (\) at the end of the current line to continue onto the next line.
        ///
        /// Default: There is no default
        ///
        /// Restrictions
        ///
        ///     The PARFILE parameter cannot be specified within a parameter file.
        /// </remarks>
        /// <example>
        /// : expdp hr PARFILE=hr.par
        /// : impdp hr PARFILE=hr_imp.par
        /// </example>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Parameter(PARFILE)]
        public override string? ParametersFile { get; set; }

        /// <summary>
        /// Allows you to specify a query clause that is used to filter the data that gets exported.
        /// </summary>
        /// <remarks>
        /// The query_clause is typically a SQL WHERE clause for fine-grained row selection, but could be any SQL clause.
        /// For example, an ORDER BY clause could be used to speed up a migration from a heap-organized table to an index-organized table.
        /// If a schema and table name are not supplied, then the query is applied to (and must be valid for) all tables in the export job.
        /// A table-specific query overrides a query applied to all tables.
        ///
        /// When the query is to be applied to a specific table, a colon must separate the table name from the query clause. More than one table-specific query can be specified, but only one query can be specified per table.
        ///
        /// If the NETWORK_LINK parameter is specified along with the QUERY parameter, then any objects specified in the query_clause that are on the remote (source) node must be explicitly qualified with the NETWORK_LINK value.
        /// Otherwise, Data Pump assumes that the object is on the local (target) node; if it is not, then an error is returned and the import of the table from the remote (source) system fails.
        ///
        /// When the QUERY parameter is used, the external tables method (rather than the direct path method) is used for data access.
        ///
        /// To specify a schema other than your own in a table-specific query, you must be granted access to that specific table.
        ///
        /// Restrictions
        ///
        ///     EXPORT: The QUERY parameter cannot be used with the following parameters:
        ///         CONTENT=METADATA_ONLY
        ///         ESTIMATE_ONLY
        ///         TRANSPORT_TABLESPACES
        ///
        ///     IMPORT: The QUERY parameter cannot be used with the following parameters:
        ///         CONTENT=METADATA_ONLY
        ///         SQLFILE
        ///         TRANSPORT_DATAFILES
        ///
        ///     When the QUERY parameter is specified for a table, Data Pump uses external tables to unload the target table. External tables uses a SQL CREATE TABLE AS SELECT statement.
        ///     The value of the QUERY parameter is the WHERE clause in the SELECT portion of the CREATE TABLE statement.
        ///     If the QUERY parameter includes references to another table with columns whose names match the table being unloaded, and if those columns are used in the query, then you will need to use a table alias to distinguish between columns in the table being unloaded and columns in the SELECT statement with the same name.
        ///     The table alias used by Data Pump for the table being unloaded is KU$.
        ///
        ///     The maximum length allowed for a QUERY string is 4000 bytes including quotation marks, which means that the actual maximum length allowed is 3998 bytes.
        /// </remarks>
        /// <example>
        /// emp_query.par
        ///     QUERY=employees:"WHERE department_id &gt; 10 AND salary &gt; 10000"
        ///     NOLOGFILE=YES
        ///     DIRECTORY=dpump_dir1
        ///     DUMPFILE=exp1.dmp
        /// : expdp hr PARFILE=emp_query.par
        /// query_imp.par
        ///     QUERY=departments:"WHERE department_id &lt; 120"
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp PARFILE=query_imp.par NOLOGFILE=YES
        /// </example>
        [Parameter("QUERY")]
        public string? Query { get; set; }

        /// <summary>
        /// EXPORT: The REMAP_DATA parameter allows you to specify a remap function that takes as a source the original value of the designated column and returns a remapped value that will replace the original value in the dump file.
        /// IMPORT: The REMAP_DATA parameter allows you to remap data as it is being inserted into a new database.
        /// </summary>
        /// <remarks>
        /// EXPORT: A common use for this option is to mask data when moving from a production system to a test system.
        ///         For example, a column of sensitive customer data such as credit card numbers could be replaced with numbers generated by a REMAP_DATA function.
        ///         This would allow the data to retain its essential formatting and processing characteristics without exposing private data to unauthorized personnel.
        /// IMPORT: A common use is to regenerate primary keys to avoid conflict when importing a table into a preexisting table on the target database.
        /// IMPORT: You can specify a remap function that takes as a source the value of the designated column from either the dump file or a remote database.
        ///         The remap function then returns a remapped value that will replace the original value in the target database.
        ///
        /// The same function can be applied to multiple columns being dumped.
        /// This is useful when you want to guarantee consistency in remapping both the child and parent column in a referential constraint.
        ///
        /// The description of each syntax element, in the order in which they appear in the syntax, is as follows:
        ///     schema: the schema containing the table to be remapped. By default, this is the schema of the user doing the import/export.
        ///     tablename : the table whose column will be remapped.
        ///     column_name: the column whose data is to be remapped.
        ///     schema : the schema containing the PL/SQL package you have created that contains the remapping function. As a default, this is the schema of the user doing the import/export.
        ///     pkg: the name of the PL/SQL package you have created that contains the remapping function.
        ///     function: the name of the function within the PL/SQL that will be called to remap the column table in each row of the specified table.
        ///
        /// Restrictions
        ///
        ///     The data types of the source argument and the returned value should both match the data type of the designated column in the table.
        ///
        ///     Remapping functions should not perform commits or rollbacks except in autonomous transactions.
        ///
        ///     The use of synonyms as values for the REMAP_DATA parameter is not supported.
        ///     For example, if the regions table in the hr schema had a synonym of regn, an error would be returned if you specified regn as part of the REMPA_DATA specification.
        ///
        ///     Remapping LOB column data of a remote table is not supported.
        ///
        ///     Columns of the following types are not supported byREMAP_DATA: User Defined Types, attributes of User Defined Types, LONGs, REFs, VARRAYs, Nested Tables, BFILEs, and XMLtype.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=remap1.dmp TABLES=employees REMAP_DATA=hr.employees.employee_id:hr.remap.minus10 REMAP_DATA=hr.employees.first_name:hr.remap.plusx
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expschema.dmp TABLES=hr.employees REMAP_DATA=hr.employees.first_name:hr.remap.plusx
        /// </example>
        [Parameter("REMAP_DATA")]
        public string[]? RemapData { get; set; }

        /// <summary>
        /// Specifies that you want to perform a schema-mode import/export.
        /// </summary>
        /// <remarks>
        /// This is the default mode for Export.
        /// Schema mode is the default mode when you are performing a network-based import.
        ///
        /// If you have the DATAPUMP_EXP_FULL_DATABASE role, then you can specify a single schema other than your own or a list of schema names.
        ///
        /// EXPORT: The DATAPUMP_EXP_FULL_DATABASE role also allows you to export additional nonschema object information for each specified schema so that the schemas can be re-created at import time.
        /// This additional information includes the user definitions themselves and all associated system and role grants, user password history, and so on.
        /// Filtering can further restrict what is imported/exported using schema mode (see "Filtering During Import/Export Operations").
        ///
        /// IMPORT: First, the user definitions are imported (if they do not already exist), including system and role grants, password history, and so on.
        /// Then all objects contained within the schemas are imported. Unprivileged users can specify only their own schemas or schemas remapped to their own schemas.
        /// In that case, no information about the schema definition is imported, only the objects contained within it.
        ///
        /// Restrictions
        ///
        ///     If you do not have the DATAPUMP_EXP_FULL_DATABASE role, then you can specify only your own schema.
        ///     The SYS schema cannot be used as a source schema for export jobs.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=expdat.dmp SCHEMAS=hr,sh,oe
        /// : impdp hr SCHEMAS=hr DIRECTORY=dpump_dir1 LOGFILE=schemas.log DUMPFILE=expdat.dmp
        /// </example>
        [Parameter("SCHEMAS")]
        public string[]? Schemas { get; set; }

        /// <summary>
        /// Used to specify a service name to be used in conjunction with the CLUSTER parameter.
        /// </summary>
        /// <remarks>
        /// The SERVICE_NAME parameter can be used with the CLUSTER=YES parameter to specify an existing service associated with a resource group that defines a set of Oracle Real Application Clusters (Oracle RAC) instances belonging to that resource group, typically a subset of all the Oracle RAC instances.
        ///
        ///  The service name is only used to determine the resource group and instances defined for that resource group. The instance where the job is started is always used, regardless of whether it is part of the resource group.
        ///
        ///  The SERVICE_NAME parameter is ignored if CLUSTER=NO is also specified.
        ///
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=hr_svname2.dmp SERVICE_NAME=sales
        /// : impdp system DIRECTORY=dpump_dir1 SCHEMAS=hr SERVICE_NAME=sales NETWORK_LINK=dbs1
        /// </example>
        [Parameter("SERVICE_NAME")]
        public string? ServiceName { get; set; }

        /// <summary>
        /// Specifies the database edition from which objects will be imported/exported.
        /// </summary>
        /// <remarks>
        /// EXPORT: Default: the default database edition on the system
        /// IMPORT: Default: the default database edition on the remote node from which objects will be fetched
        ///
        /// If SOURCE_EDITION=edition_name is specified, then the objects from that edition are imported/exported.
        /// Data Pump selects all inherited objects that have not changed and all actual objects that have changed.
        ///
        /// If this parameter is not specified, then the default edition is used.
        /// If the specified edition does not exist or is not usable, then an error message is returned.
        ///
        /// Restrictions
        ///
        ///     The SOURCE_EDITION parameter is valid on an import operation only when the NETWORK_LINK parameter is also specified. See "NETWORK_LINK".
        ///
        ///     This parameter is only useful if there are two or more versions of the same versionable objects in the database.
        ///
        ///     The job version must be 11.2 or later. See "VERSION".
        /// </remarks>
        /// <example>
        ///  : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=exp_dat.dmp SOURCE_EDITION=exp_edition EXCLUDE=USER
        ///  : impdp hr DIRECTORY=dpump_dir1 SOURCE_EDITION=exp_edition NETWORK_LINK=source_database_link EXCLUDE=USER
        /// </example>
        [Parameter("SOURCE_EDITION")]
        public string? SourceEdition { get; set; }

        /// <summary>
        /// Specifies the frequency at which the job status display is updated.Specifies the frequency at which the job status display is updated.
        /// </summary>
        /// <remarks>
        /// Default: 0
        ///
        /// If you supply a value for integer, it specifies how frequently, in seconds, job status should be displayed in logging mode.
        /// If no value is entered or if the default value of 0 is used, then no additional information is displayed beyond information about the completion of each object type, table, or partition.
        ///
        /// This status information is written only to your standard output device, not to the log file (if one is in effect).
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 SCHEMAS=hr,sh STATUS=300
        /// : impdp hr NOLOGFILE=YES STATUS=120 DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp
        /// </example>
        [Parameter("STATUS")]
        public int? Status { get; set; }

        /// <summary>
        /// Specifies that you want to perform a table-mode import/export.
        /// </summary>
        /// <remarks>
        /// EXPORT: Filtering can restrict what is exported using this mode (see "Filtering During Export Operations").
        /// EXPORT:You can filter the data and metadata that is exported, by specifying a comma-delimited list of tables and partitions or subpartitions.
        /// EXPORT:If a partition name is specified, then it must be the name of a partition or subpartition in the associated table. Only the specified set of tables, partitions, and their dependent objects are unloaded.
        ///
        /// EXPORT:If an entire partitioned table is exported, then it will be imported in its entirety, as a partitioned table.
        /// EXPORT:The only case in which this is not true is if PARTITION_OPTIONS=DEPARTITION is specified during import.
        ///
        /// EXPORT:The table name that you specify can be preceded by a qualifying schema name.
        /// EXPORT:The schema defaults to that of the current user.
        /// EXPORT:To specify a schema other than your own, you must have the DATAPUMP_EXP_FULL_DATABASE role.
        ///
        /// IMPORT: In a table-mode import, you can filter the data that is imported from the source by specifying a comma-delimited list of tables and partitions or subpartitions.
        ///
        /// IMPORT: If you do not supply a schema_name, then it defaults to that of the current user.
        /// IMPORT: To specify a schema other than your own, you must either have the DATAPUMP_IMP_FULL_DATABASE role or remap the schema to the current user.
        ///
        /// IMPORT: The use of filtering can restrict what is imported using this import mode. See "Filtering During Import Operations".
        ///
        /// Use of the wildcard character, %, to specify table names and partition names is supported.
        ///
        /// The following restrictions apply to table names:
        ///
        ///     By default, table names in a database are stored as uppercase.
        ///     If you have a table name in mixed-case or lowercase, and you want to preserve case-sensitivity for the table name, then you must enclose the name in quotation marks.
        ///     The name must exactly match the table name stored in the database.
        ///
        ///     Some operating systems require that quotation marks on the command line be preceded by an escape character
        ///
        ///     Table names specified on the command line cannot include a pound sign (#), unless the table name is enclosed in quotation marks.
        ///     Similarly, in the parameter file, if a table name includes a pound sign (#), then the Export utility interprets the rest of the line as a comment, unless the table name is enclosed in quotation marks.
        ///
        /// Using the Transportable Option During Table-Mode Export
        ///
        ///     To use the transportable option during a table-mode export, specify the TRANSPORTABLE=ALWAYS parameter with the TABLES parameter.
        ///     Metadata for the specified tables, partitions, or subpartitions is exported to the dump file.
        ///     To move the actual data, you copy the data files to the target database.
        ///
        ///     If only a subset of a table's partitions are exported and the TRANSPORTABLE=ALWAYS parameter is used, then on import each partition becomes a non-partitioned table.
        ///
        /// Restrictions
        ///
        ///     Cross-schema references are not exported. For example, a trigger defined on a table within one of the specified schemas, but that resides in a schema not explicitly specified, is not exported.
        ///
        ///     Types used by the table are not exported in table mode.
        ///     This means that if you subsequently import the dump file and the type does not already exist in the destination database, then the table creation will fail.
        ///
        ///     The use of synonyms as values for the TABLES parameter is not supported.
        ///     For example, if the regions table in the hr schema had a synonym of regn, then it would not be valid to use TABLES=regn. An error would be returned.
        ///
        ///
        ///     The export of tables that include a wildcard character, %, in the table name is not supported if the table has partitions.
        ///
        ///     The length of the table name list specified for the TABLES parameter is limited to a maximum of 4 MB, unless you are using the NETWORK_LINK parameter to an Oracle Database release 10.2.0.3 or earlier or to a read-only database.
        ///     In such cases, the limit is 4 KB.
        ///
        ///     EXPORT: You can only specify partitions from one table if TRANSPORTABLE=ALWAYS is also set on the export.
        ///     IMPORT: You can only specify partitions from one table if PARTITION_OPTIONS=DEPARTITION is also specified on the import.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=tables.dmp TABLES=employees,jobs,departments
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp TABLES=employees,jobs
        /// </example>
        [Parameter("TABLES")]
        public string[]? Tables { get; set; }

        /// <summary>
        /// Specifies a list of tablespace names to be imported/exported in tablespace mode.
        /// </summary>
        /// <remarks>
        /// EXPORT: In tablespace mode, only the tables contained in a specified set of tablespaces are unloaded.
        /// EXPORT: If a table is unloaded, then its dependent objects are also unloaded. Both object metadata and data are unloaded.
        /// EXPORT: If any part of a table resides in the specified set, then that table and all of its dependent objects are exported.
        /// EXPORT: Privileged users get all tables. Unprivileged users get only the tables in their own schemas
        ///
        /// IMPORT: Use TABLESPACES to specify a list of tablespace names whose tables and dependent objects are to be imported from the source (full, schema, tablespace, or table-mode export dump file set or another database).
        /// IMPORT: During the following import situations, Data Pump automatically creates the tablespaces into which the data will be imported:
        ///     The import is being done in FULL or TRANSPORT_TABLESPACES mode
        ///     The import is being done in table mode with TRANSPORTABLE=ALWAYS
        /// IMPORT: In all other cases, the tablespaces for the selected objects must already exist on the import database.
        /// You could also use the Import REMAP_TABLESPACE parameter to map the tablespace name to an existing tablespace on the import database.
        ///
        /// Filtering can restrict what is exported using this mode (see "Filtering During Export Operations").
        ///
        /// Restrictions
        ///
        ///     The length of the tablespace name list specified for the TABLESPACES parameter is limited to a maximum of 4 MB, unless you are using the NETWORK_LINK to an Oracle Database release 10.2.0.3 or earlier or to a read-only database.
        ///     In such cases, the limit is 4 KB.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=tbs.dmp TABLESPACES=tbs_4, tbs_5, tbs_6
        /// : impdp hr DIRECTORY=dpump_dir1 DUMPFILE=expfull.dmp TABLESPACES=tbs_1,tbs_2,tbs_3,tbs_4
        /// </example>
        [Parameter("TABLESPACES")]
        public string[]? Tablespaces { get; set; }

        /// <summary>
        /// Specifies whether to check for dependencies between those objects inside the transportable set and those outside the transportable set.
        /// This parameter is applicable only to a transportable-tablespace mode export.
        /// </summary>
        /// <remarks>
        /// Default: NO
        ///
        /// If TRANSPORT_FULL_CHECK=YES, then Import/Export verifies that there are no dependencies between those objects inside the transportable set and those outside the transportable set.
        /// The check addresses two-way dependencies. For example, if a table is inside the transportable set but its index is not, then a failure is returned and the import/export operation is terminated.
        /// Similarly, a failure is also returned if an index is in the transportable set but the table is not.
        ///
        /// If TRANSPORT_FULL_CHECK=NO, then Import/Export verifies only that there are no objects within the transportable set that are dependent on objects outside the transportable set.
        /// This check addresses a one-way dependency. For example, a table is not dependent on an index, but an index is dependent on a table, because an index without a table has no meaning.
        /// Therefore, if the transportable set contains a table, but not its index, then this check succeeds.
        /// However, if the transportable set contains an index, but not the table, then the import/export operation is terminated.
        ///
        /// EXPORT: There are other checks performed as well.
        /// For instance, export always verifies that all storage segments of all tables (and their indexes) defined within the tablespace set specified by TRANSPORT_TABLESPACES are actually contained within the tablespace set.
        ///
        /// IMPORT: In addition to this check, Import always verifies that all storage segments of all tables (and their indexes) defined within the tablespace set specified by TRANSPORT_TABLESPACES are actually contained within the tablespace set.
        /// Restrictions
        ///
        ///     This parameter is valid for transportable mode (or table mode or full mode when TRANSPORTABLE=ALWAYS was specified on the export) only when the NETWORK_LINK parameter is specified.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=tts.dmp TRANSPORT_TABLESPACES=tbs_1 TRANSPORT_FULL_CHECK=YES LOGFILE=tts.log
        /// full_check.par
        ///     DIRECTORY=dpump_dir1
        ///     TRANSPORT_TABLESPACES=tbs_6
        ///     NETWORK_LINK=source_database_link
        ///     TRANSPORT_FULL_CHECK=YES
        ///     TRANSPORT_DATAFILES='/wkdir/data/tbs6.dbf'
        /// : impdp hr PARFILE=full_check.par
        /// </example>
        [Parameter("TRANSPORT_FULL_CHECK")]
        public bool? TransportFullCheck { get; set; }

        /// <summary>
        /// EXPORT: Specifies that you want to perform an export in transportable-tablespace mode.
        /// IMPORT: Specifies that you want to perform an import in transportable-tablespace mode over a database link (as specified with the NETWORK_LINK parameter.)
        /// </summary>
        /// <remarks>
        /// Use the TRANSPORT_TABLESPACES parameter to specify a list of tablespace names for which object metadata will be imported/exported from the source database into the target database.
        ///
        /// EXPORT: The log file for the export lists the data files that are used in the transportable set, the dump files, and any containment violations.
        ///
        /// EXPORT: The TRANSPORT_TABLESPACES parameter exports metadata for all objects within the specified tablespaces.
        /// EXPORT: If you want to perform a transportable export of only certain tables, partitions, or subpartitions, then you must use the TABLES parameter with the TRANSPORTABLE=ALWAYS parameter.
        ///
        /// IMPORT: Because this is a transportable-mode import, the tablespaces into which the data is imported are automatically created by Data Pump.You do not need to pre-create them. However, the data files should be copied to the target database before starting the import.
        ///
        /// IMPORT: When you specify TRANSPORT_TABLESPACES on the import command line, you must also use the NETWORK_LINK parameter to specify a database link. A database link is a connection between two physical database servers that allows a client to access them as one logical database. Therefore, the NETWORK_LINK parameter is required because the object metadata is exported from the source (the database being pointed to by NETWORK_LINK) and then imported directly into the target (database from which the impdp command is issued), using that database link. There are no dump files involved in this situation. You would also need to specify the TRANSPORT_DATAFILES parameter to let the import know where to find the actual data, which had been copied to the target in a separate operation using some other means.
        ///
        /// Note:
        ///
        ///     EXPORT: You cannot export transportable tablespaces and then import them into a database at a lower release level.
        ///         The target database must be at the same or later release level as the source database.
        ///     IMPORT: If you already have a dump file set generated by a transportable-tablespace mode export, then you can perform a transportable-mode import of that dump file, but in this case you do not specify TRANSPORT_TABLESPACES or NETWORK_LINK. Doing so would result in an error.
        ///         Rather, you specify the dump file (which contains the metadata) and the TRANSPORT_DATAFILES parameter.
        ///         The presence of the TRANSPORT_DATAFILES parameter tells import that it's a transportable-mode import and where to get the actual data.
        ///
        /// IMPORT: When transportable jobs are performed, it is best practice to keep a copy of the data files on the source system until the import job has successfully completed on the target system.
        ///     If the import job should fail for some reason, you will still have uncorrupted copies of the data files.
        ///
        /// Restrictions
        ///
        ///     IMPORT: The TRANSPORT_TABLESPACES parameter is valid only when the NETWORK_LINK parameter is also specified.
        ///     IMPORT: To use the TRANSPORT_TABLESPACES parameter to perform a transportable tablespace import, the COMPATIBLE initialization parameter must be set to at least 11.0.0.
        ///     Transportable tablespace mode does not support encrypted columns.
        ///     Transportable tablespace jobs are not restartable.
        ///     Transportable tablespace jobs are restricted to a degree of parallelism of 1.
        ///     Transportable tablespace mode requires that you have the DATAPUMP_EXP_FULL_DATABASE role.
        ///     The default tablespace of the user performing the export must not be set to one of the tablespaces being transported.
        ///     The SYSTEM and SYSAUX tablespaces are not transportable in transportable tablespace mode.
        ///     All tablespaces in the transportable set must be set to read-only.
        ///     If the Data Pump Export VERSION parameter is specified along with the TRANSPORT_TABLESPACES parameter, then the version must be equal to or greater than the Oracle Database COMPATIBLE initialization parameter.
        ///     The TRANSPORT_TABLESPACES parameter cannot be used in conjunction with the QUERY parameter.
        ///     Transportable tablespace jobs do not support the ACCESS_METHOD parameter for Data Pump Export.
        /// </remarks>
        /// <example>
        /// : expdp hr DIRECTORY=dpump_dir1 DUMPFILE=tts.dmp TRANSPORT_TABLESPACES=tbs_1 TRANSPORT_FULL_CHECK=YES LOGFILE=tts.log
        /// tablespaces.par
        ///     DIRECTORY=dpump_dir1
        ///     NETWORK_LINK=source_database_link
        ///     TRANSPORT_TABLESPACES=tbs_6
        ///     TRANSPORT_FULL_CHECK=NO
        ///     TRANSPORT_DATAFILES='user01/data/tbs6.dbf'
        /// : impdp hr PARFILE=tablespaces.par
        /// </example>
        [Parameter("TRANSPORT_TABLESPACES")]
        public string[]? TransportTablespaces { get; set; }

        /// <summary>
        /// Specifies whether the transportable option should be used during a table mode import/export (specified with the TABLES parameter) or a full mode import/export (specified with the FULL parameter).
        /// </summary>
        /// <remarks>
        /// Default: NEVER
        ///
        /// EXPORT: If you want to export an entire tablespace in transportable mode, then use the TRANSPORT_TABLESPACES parameter.
        /// EXPORT: If only a subset of a table's partitions are exported and the TRANSPORTABLE=ALWAYS parameter is used, then on import each partition becomes a non-partitioned table.
        /// EXPORT: If only a subset of a table's partitions are exported and the TRANSPORTABLE parameter is not used at all or is set to NEVER (the default), then on import:
        /// EXPORT: If PARTITION_OPTIONS=DEPARTITION is used, then each partition included in the dump file set is created as a non-partitioned table.
        /// EXPORT: If PARTITION_OPTIONS is not used, then the complete table is created.
        ///     That is, all the metadata for the complete table is present so that the table definition looks the same on the target system as it did on the source.
        ///     But only the data that was exported for the specified partitions is inserted into the table.
        ///
        /// IMPORT: In a table mode import, using the transportable option results in a transportable tablespace import in which only metadata for the specified tables, partitions, or subpartitions is imported.
        /// IMPORT: In a full mode import, using the transportable option results in a full transportable import in which metadata for all objects in the specified database is imported.
        /// IMPORT: In both cases you must copy (and possibly convert) the actual data files to the target database in a separate operation.
        /// IMPORT: When transportable jobs are performed, it is best practice to keep a copy of the data files on the source system until the import job has successfully completed on the target system.
        ///     If the import job should fail for some reason, you will still have uncorrupted copies of the data files.
        /// IMPORT:If only a subset of a table's partitions are imported and the TRANSPORTABLE=ALWAYS parameter is used, then each partition becomes a non-partitioned table.
        /// IMPORT: If only a subset of a table's partitions are imported and the TRANSPORTABLE parameter is not used or is set to NEVER (the default), then:
        /// IMPORT: If PARTITION_OPTIONS=DEPARTITION is used, then each partition is created as a non-partitioned table.
        /// IMPORT:     If PARTITION_OPTIONS is not used, then the complete table is created. That is, all the metadata for the complete table is present so that the table definition looks the same on the target system as it did on the source. But only the data for the specified partitions is inserted into the table.
        ///
        /// Restrictions
        ///
        ///     EXPORT: The TRANSPORTABLE parameter is only valid in table mode exports and full mode exports.
        ///     IMPORT: The Import TRANSPORTABLE parameter is valid only if the NETWORK_LINK parameter is also specified.
        ///     IMPORT: The TRANSPORTABLE parameter is only valid in table mode imports and full mode imports.
        ///
        ///     EXPORT: To use the TRANSPORTABLE parameter, the COMPATIBLE initialization parameter must be set to at least 11.0.0.
        ///
        ///     To use the FULL parameter in conjunction with TRANSPORTABLE (to perform a full transportable export), the Data Pump VERSION parameter must be set to at least 12.0.
        ///     If the VERSION parameter is not specified, then the COMPATIBLE database initialization parameter must be set to at least 12.0 or later.
        ///
        ///     The user performing a transportable export requires the DATAPUMP_EXP_FULL_DATABASE privilege.
        ///
        ///     Tablespaces associated with tables, partitions, and subpartitions must be read-only.
        ///
        ///     EXPORT: A full transportable export uses a mix of data movement methods.
        ///     Objects residing in a transportable tablespace have only their metadata unloaded; data is copied when the data files are copied from the source system to the target system.
        ///     The data files that must be copied are listed at the end of the log file for the export operation.
        ///     Objects residing in non-transportable tablespaces (for example, SYSTEM and SYSAUX) have both their metadata and data unloaded into the dump file set.
        ///     (See Oracle Database Administrator's Guide for more information about performing full transportable exports.)
        ///
        ///     EXPORT: The default tablespace of the user performing the export must not be set to one of the tablespaces being transported.
        ///
        ///     IMPORT: All objects with storage that are selected for network import must have all of their storage segments on the source system either entirely within administrative, non-transportable tablespaces (SYSTEM / SYSAUX) or entirely within user-defined, transportable tablespaces.
        ///         Storage for a single object cannot straddle the two kinds of tablespaces.
        ///     IMPORT:To use the TRANSPORTABLE parameter to perform a network-based full transportable import, the Data Pump VERSION parameter must be set to at least 12.0 if the source database is release 11.2.0.3.
        ///         If the source database is release 12.1 or later, then the VERSION parameter is not required, but the COMPATIBLE database initialization parameter must be set to 12.0.0 or later.
        /// </remarks>
        /// <example>
        /// : expdp sh DIRECTORY=dpump_dir1 DUMPFILE=tto1.dmp TABLES=sh.sales2 TRANSPORTABLE=ALWAYS
        /// : impdp system TABLES=hr.sales TRANSPORTABLE=ALWAYS DIRECTORY=dpump_dir1 NETWORK_LINK=dbs1 PARTITION_OPTIONS=DEPARTITION TRANSPORT_DATAFILES=datafile_name
        /// </example>
        [Parameter("TRANSPORTABLE")]
        public TransportableOption? Transportable { get; set; }


        /// <summary>
        /// The USERID parameter is used to provide your Oracle username and password.
        /// </summary>
        /// <remarks>
        /// Specify a user name. For security reasons, Oracle recommends that you specify only the user name on the command line. SQL*Loader then prompts you for a password.
        ///
        /// If you do not specify the USERID parameter, then you are prompted for it. If only a slash is used, then USERID defaults to your operating system login.
        ///
        /// If you connect as user SYS, then you must also specify AS SYSDBA in the connect string.
        ///
        /// Restrictions
        ///
        ///     Because the string, AS SYSDBA, contains a blank, some operating systems may require that the entire connect string be placed in quotation marks or marked as a literal by some method. Some operating systems also require that quotation marks on the command line be preceded by an escape character, such as backslashes.
        ///
        ///     See your Oracle operating system-specific documentation for information about special and reserved characters on your system.
        /// </remarks>
        /// <example>
        /// The following example specifies a user name of hr. SQL*Loader then prompts for a password. Because it is the first and only parameter specified, you do not need to include the parameter name USERID:
        /// : sqlldr hr
        /// Password:
        /// </example>
        [JsonIgnore] //prevent accidentally leaking this property into logs which can contain user AND password
        [IgnoreDataMember]
        [Parameter("USERID")]
        public string? UserId { get; set; }

        /// <summary>
        /// Specifies the version of database objects to be imported/exported (that is, only database objects and attributes that are compatible with the specified release will be imported/exported).
        /// </summary>
        /// <remarks>
        /// <see cref="ObjectVersion"/>
        ///
        /// Data Pump Import/Export only works with Oracle Database 10g release 1 (10.1) or later.
        ///
        /// Only database objects and attributes that are compatible with the specified release will be imported/exported.
        /// This can be used to create a dump file set that is compatible with a previous release of Oracle Database.
        ///
        /// On Oracle Database 11g release 2 (11.2.0.3) or later, the VERSION parameter can be specified as VERSION=12 in conjunction with FULL=Y to generate a full export dump file that is ready for import into Oracle Database 12c.
        /// The export will include information from registered database options and components.
        /// (This dump file set can only be imported into Oracle Database 12c Release 1 (12.1.0.1) and later.)
        /// If VERSION=12 is used in conjunction with FULL=Y and also with TRANSPORTABLE=ALWAYS, then a full transportable export dump file is generated that is ready for import into Oracle Database 12c.
        /// (See "Using the Transportable Option During Full Mode Exports".)
        ///
        /// Restrictions
        ///
        ///     Exporting a table with archived LOBs to a database release earlier than 11.2 is not allowed.
        ///
        ///     If the Data Pump Export VERSION parameter is specified along with the TRANSPORT_TABLESPACES parameter, then the value must be equal to or greater than the Oracle Database COMPATIBLE initialization parameter.
        ///
        ///     If the Data Pump VERSION parameter is specified as any value earlier than 12.1, then the Data Pump dump file excludes any tables that contain VARCHAR2 or NVARCHAR2 columns longer than 4000 bytes and any RAW columns longer than 2000 bytes.
        ///
        ///     Full imports performed over a network link require that you set VERSION=12 if the target is Oracle Database 12c Release 1 (12.1.0.1) or later and the source is Oracle Database 11g Release 2 (11.2.0.3) or later.
        ///
        ///     Dump files created on Oracle Database 11g releases with the Data Pump parameter VERSION=12 can only be imported on Oracle Database 12c Release 1 (12.1) and later.
        /// </remarks>
        /// <example>
        /// : expdp hr TABLES=hr.employees VERSION=LATEST DIRECTORY=dpump_dir1 DUMPFILE=emp.dmp NOLOGFILE=YES
        /// : impdp hr FULL=Y DIRECTORY=dpump_dir1 NETWORK_LINK=source_database_link VERSION=12
        /// </example>
        [Parameter("VERSION")]
        public string? Version { get; set; }

        /// <summary>
        /// Specifies that one or more views are to be imported/exported as tables.
        /// </summary>
        /// <remarks>
        /// Data Pump imports/exports a table with the same columns as the view and with row data fetched from the view.
        /// Data Pump also imports/exports objects dependent on the view, such as grants and constraints.
        /// Dependent objects that do not apply to tables (for example, grants of the UNDER object privilege) are not imported/exported.
        /// The VIEWS_AS_TABLES parameter can be used by itself or along with the TABLES parameter.
        /// If either is used, Data Pump performs a table-mode import/export.
        ///
        /// The syntax elements are defined as follows:
        ///
        ///     schema_name--The name of the schema in which the view resides.
        ///         If a schema name is not supplied, it defaults to the user performing the export.
        ///     view_name--The name of the view to be exported as a table.
        ///         The view must exist and it must be a relational view with only scalar, non-LOB columns.
        ///         If you specify an invalid or non-existent view, the view is skipped and an error message is returned.
        ///     table_name--The name of a table to serve as the source of the metadata for the exported view.
        ///         By default Data Pump automatically creates a temporary "template table" with the same columns and data types as the view, but no rows.
        ///         If the database is read-only, then this default creation of a template table will fail.
        ///         In such a case, you can specify a table name.
        ///         The table must be in the same schema as the view.
        ///         It must be a non-partitioned relational table with heap organization. It cannot be a nested table.
        ///
        /// If the export job contains multiple views with explicitly specified template tables, the template tables must all be different.
        ///
        /// Restrictions
        ///
        ///     The VIEWS_AS_TABLES parameter cannot be used with the TRANSPORTABLE=ALWAYS parameter.
        ///
        ///     Tables created using the VIEWS_AS_TABLES parameter do not contain any hidden columns that were part of the specified view.
        ///
        ///     The VIEWS_AS_TABLES parameter does not support tables that have columns with a data type of LONG.
        /// </remarks>
        /// <example>
        /// : expdp scott/tiger views_as_tables=view1 directory=data_pump_dir dumpfile=scott1.dmp
        /// : impdp scott/tiger views_as_tables=view1 directory=data_pump_dir dumpfile=scott1.dmp
        /// </example>
        [Parameter("VIEWS_AS_TABLES")]
        public string[]? ViewsAsTables { get; set; }

        internal override string CoerceBool(bool b) => YesNo(b);

        internal override bool CoerceBool(string s) => s == "YES";
    }
}