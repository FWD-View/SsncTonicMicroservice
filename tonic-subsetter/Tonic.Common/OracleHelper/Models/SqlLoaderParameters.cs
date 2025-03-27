using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Tonic.Common.OracleHelper.Models.SqlLoader;
using ExternalTableBehavior = Tonic.Common.OracleHelper.Models.SqlLoader.ExternalTableBehavior;
using SilentOptions = Tonic.Common.OracleHelper.Models.SqlLoader.SilentOptions;
using TrimBehavior = Tonic.Common.OracleHelper.Models.SqlLoader.TrimBehavior;

namespace Tonic.Common.OracleHelper.Models;

/// <summary>
/// Parameters available in the command-line mode of Sql*Loader
/// </summary>
/// <remarks>
/// https://docs.oracle.com/database/121/SUTIL/GUID-D7A661F1-5EE3-43DF-B3A5-050B2CF66844.htm#SUTIL1018
/// </remarks>
public sealed record SqlLoaderParameters : ParametersBase
{
    /// <summary>
    /// Specifies the name or location, or both, of the bad file associated with the first data file specification.
    /// </summary>
    /// <remarks>
    /// Default: The name of the data file, with an extension of .bad
    ///
    /// The bad file stores records that cause errors during insert or that are improperly formatted.
    /// If you specify the BAD parameter, you must supply either a directory or file name, or both.
    /// If there are rejected records, and you have not specified a name for the bad file, then the name defaults to the name of the data file with an extension or file type of .bad.
    ///
    /// The directory parameter specifies a directory to which the bad file is written. The specification can include the name of a device or network node. The value of directory is determined as follows:
    ///
    ///     If the BAD parameter is not specified at all and a bad file is needed, then the default directory is the one in which the SQL*Loader control file resides.
    ///     If the BAD parameter is specified with a file name but no directory, then the directory defaults to the current directory.
    ///     If the BAD parameter is specified with a directory but no file name, then the specified directory is used and the default is used for the bad file name and extension.
    ///
    /// The filename parameter specifies a file name recognized as valid on your platform. You must specify only a name (and extension, if one other than .bad is desired).
    /// Any spaces or punctuation marks in the file name must be enclosed in single quotation marks.
    ///
    /// A bad file specified on the command line becomes the bad file associated with the first INFILE statement (if there is one) in the control file.
    /// The name of the bad file can also be specified in the SQL*Loader control file, using the BADFILE clause.
    /// If the bad file is specified in the control file, as well as on the command line, then the command-line value is used.
    /// If a bad file with that name already exists, then it is either overwritten or a new version is created, depending on your operating system
    /// </remarks>
    /// <example>
    /// The following specification creates a bad file named emp1.bad in the current directory:
    /// BAD=emp1
    /// </example>
    [Parameter("BAD")]
    public string? BadFile { get; set; }

    /// <summary>
    /// The BINDSIZE parameter specifies the maximum size (in bytes) of the bind array.
    /// </summary>
    /// <remarks>
    /// Default: 256000
    ///
    /// A bind array is an area in memory where SQL*Loader stores data that is to be loaded.
    /// When the bind array is full, the data is transmitted to the database.
    /// The bind array size is controlled by the BINDSIZE and READSIZE parameters.
    ///
    /// The size of the bind array given by BINDSIZE overrides the default size (which is system dependent) and any size determined by ROWS.
    ///
    /// Restrictions
    ///
    ///     The BINDSIZE parameter is used only for conventional path loads.
    /// </remarks>
    /// <example>
    /// The following BINDSIZE specification limits the maximum size of the bind array to 356,000 bytes
    /// BINDSIZE=356000
    /// </example>
    [Parameter("BINDSIZE")]
    public long? BindSize { get; set; }

    /// <summary>
    /// The COLUMNARRAYROWS parameter specifies the number of rows to allocate for direct path column arrays.
    /// </summary>
    /// <remarks>
    /// Default: 5000
    /// The value for this parameter is not calculated by SQL*Loader. You must either specify it or accept the default.
    /// </remarks>
    /// <example>
    /// The following example specifies that 1000 rows are to be allocated for direct path column arrays.
    /// COLUMNARRAYROWS=1000
    /// </example>
    [Parameter("COLUMNARRAYROWS")]
    public long? ColumnArrayRows { get; set; }

    /// <summary>
    /// The CONTROL parameter specifies the name of the SQL*Loader control file that describes how to load the data.
    /// </summary>
    /// <remarks>
    /// If a file extension or file type is not specified, then it defaults to .ctl. If the CONTROL parameter is not specified, then SQL*Loader prompts you for it.
    ///
    /// If the name of your SQL*Loader control file contains special characters, then your operating system may require that they be preceded by an escape character.
    /// Also, if your operating system uses backslashes in its file system paths, then you may need to use multiple escape characters or to enclose the path in quotation marks.
    /// </remarks>
    /// <example>
    /// The following example specifies a control file named emp1. It is automatically given the default extension of .ctl.
    /// CONTROL=emp1
    /// </example>
    [Parameter("CONTROL")]
    public string? ControlFile { get; set; }

    /// <summary>
    /// The DATA parameter specifies the name(s) of the data file(s) containing the data to be loaded.
    /// </summary>
    /// <remarks>
    /// If you do not specify a file extension, then the default is .dat.
    ///
    /// The file specification can contain wildcards (only in the file name and file extension, not in a device or directory name).
    /// An asterisk (*) represents multiple characters and a question mark (?) represents a single character. For example:
    ///
    ///     DATA='emp*.dat'
    ///     DATA='m?emp.dat'
    /// To list multiple data file specifications (each of which can contain wild cards), the file names must be separated by commas.
    ///
    /// If the file name contains any special characters (for example, spaces, *, ?, ), then the entire name must be enclosed within single quotation marks.
    ///
    /// Caution:
    ///
    /// If multiple data files are being loaded and you are also specifying the BAD parameter, it is recommended that you specify only a directory for the bad file, not a file name.
    /// If you specify a file name, and a file with that name already exists, then it is either overwritten or a new version is created, depending on your operating system.
    ///
    /// If you specify data files on the command line with the DATA parameter and also specify data files in the control file with the INFILE clause, then the first INFILE specification in the control file is ignored.
    /// All other data files specified on the command line and in the control file are processed.
    ///
    ///  If you specify a file processing option along with the DATA parameter when loading data from the control file, then a warning message is issued.
    /// </remarks>
    /// <example>
    /// The following example specifies that a data file named employees.dat is to be loaded. The .dat extension is assumed as the default because no extension is provided.
    /// DATA=employees
    /// </example>
    [Parameter("DATA")]
    public string[]? DataFiles { get; set; }

    /// <summary>
    /// The DATA_CACHE parameter specifies the data cache size (in entries).
    /// </summary>
    /// <remarks>
    /// Default: Enabled (for 1000 elements). To completely disable the data cache feature, set it to 0 (zero).
    ///
    /// The data cache is used to store the results of conversions from text strings to internal date format.
    /// The cache is useful because the cost of looking up dates is much less than converting from text format to date format.
    /// If the same dates occur repeatedly in the date file, then using the data cache can improve the speed of a direct path load.
    ///
    /// Every table has its own data cache, if one is needed. A data cache is created only if at least one date or timestamp value is loaded that requires data type conversion in order to be stored in the table.
    ///
    /// The data cache feature is enabled by default. The default data cache size is 1000 elements.
    /// If the default size is used and the number of unique input values loaded exceeds 1000, then the data cache feature is automatically disabled for that table.
    /// However, if you override the default and specify a nonzero data cache size and that size is exceeded, then the cache is not disabled.
    ///
    /// You can use the data cache statistics (entries, hits, and misses) contained in the log file to tune the size of the cache for future similar loads.
    ///
    /// Restrictions
    ///
    ///     The data cache feature is only available for direct path and external tables loads.
    /// </remarks>
    /// <example>
    /// The following specification completely disables the data cache feature.
    /// DATA_CACHE=0
    /// </example>
    [Parameter("DATA_CACHE")]
    public int? DataCache { get; set; }

    /// <summary>
    /// The DEGREE_OF_PARALLELISM parameter specifies the degree of parallelism to use during the load operation.
    /// </summary>
    /// <remarks>
    /// <see cref="Parallelism"/>
    ///
    /// Restrictions
    ///
    ///     The DEGREE_OF_PARALLELISM parameter is valid only when the external table load method is used.
    /// </remarks>
    /// <example>
    /// The following example sets the degree of parallelism for the load to 3.
    /// DEGREE_OF_PARALLELISM=3
    /// </example>
    [Parameter("DEGREE_OF_PARALLELISM")]
    public string? DegreeOfParallelism { get; set; }

    /// <summary>
    /// The DIRECT parameter specifies the load method to use, either conventional path or direct path.
    /// </summary>
    /// <remarks>
    /// A value of TRUE specifies a direct path load. A value of FALSE specifies a conventional path load.
    /// </remarks>
    /// <example>
    /// The following example specifies that the load be performed using conventional path mode.
    /// DIRECT=FALSE
    /// </example>
    [Parameter("DIRECT")]
    public bool? Direct { get; set; }

    /// <summary>
    /// The DISCARD parameter lets you optionally specify a discard file to store records that are neither inserted into a table nor rejected.
    /// </summary>
    /// <remarks>
    /// Default: The same file name as the data file, but with an extension of .dsc.
    ///
    /// They are not bad records, they simply did not match any record-selection criteria specified in the control file, such as a WHEN clause for example.
    ///
    /// If you specify the DISCARD parameter, then you must supply either a directory or file name, or both.
    ///
    /// The directory parameter specifies a directory to which the discard file will be written.
    /// The specification can include the name of a device or network node. The value of directory is determined as follows:
    ///
    ///     If the DISCARD parameter is not specified at all, but the DISCARDMAX parameter is, then the default directory is the one in which the SQL*Loader control file resides.
    ///     If the DISCARD parameter is specified with a file name but no directory, then the directory defaults to the current directory.
    ///     If the DISCARD parameter is specified with a directory but no file name, then the specified directory is used and the default is used for the name and the extension.
    ///
    /// The filename parameter specifies a file name recognized as valid on your platform.
    /// You must specify only a name (and extension, if one other than .dsc is desired). Any spaces or punctuation marks in the file name must be enclosed in single quotation marks.
    ///
    /// If neither the DISCARD parameter nor the DISCARDMAX parameter is specified, then a discard file is not created even if there are discarded records.
    ///
    /// If the DISCARD parameter is not specified, but the DISCARDMAX parameter is, and there are discarded records, then the discard file is created using the default name and the file is written to the same directory in which the SQL*Loader control file resides.
    ///
    /// Caution:
    ///
    ///     If multiple data files are being loaded and you are also specifying the DISCARD parameter, it is recommended that you specify only a directory for the discard file, not a file name.
    ///     If you specify a file name, and a file with that name already exists, then it is either overwritten or a new version is created, depending on your operating system.
    ///
    /// A discard file specified on the command line becomes the discard file associated with the first INFILE statement (if there is one) in the control file.
    /// If the discard file is also specified in the control file, then the command-line value overrides it.
    /// If a discard file with that name already exists, then it is either overwritten or a new version is created, depending on your operating system.
    /// </remarks>
    /// <example>
    /// Assume that you are loading a data file named employees.dat.
    /// The following example supplies only a directory name so the name of the discard file will be employees.dsc and it will be created in the mydir directory.
    /// DISCARD=mydir/
    /// </example>
    [Parameter("DISCARD")]
    public string? Discard { get; set; }

    /// <summary>
    /// The DISCARDMAX parameter specifies the number of discard records to allow before data loading is terminated.
    /// </summary>
    /// <remarks>
    /// Default: ALL
    /// To stop on the first discarded record, specify a value of 0.
    /// If DISCARDMAX is specified, but the DISCARD parameter is not, then the name of the discard file is the name of the data file with an extension of .dsc.
    /// </remarks>
    /// <example>
    /// The following example allows 25 records to be discarded during the load before it is terminated.
    /// DISCARDMAX=25
    /// </example>
    [Parameter("DISCARDMAX")]
    public int? DiscardMax { get; set; }

    /// <summary>
    /// The DNFS_ENABLE parameter lets you enable and disable use of the Direct NFS Client on input data files during a SQL*Loader operation.
    /// </summary>
    /// <remarks>
    /// Default: TRUE
    /// The Direct NFS Client is an API that can be implemented by file servers to allow improved performance when an Oracle database accesses files on those servers.
    ///
    /// SQL*Loader uses the Direct NFS Client interfaces by default when it reads data files over 1 GB. For smaller files, the operating system's I/O interfaces are used.
    /// To use the Direct NFS Client on all input data files, use DNFS_ENABLE=TRUE.
    ///
    /// To disable use of the Direct NFS Client for all data files, specify DNFS_ENABLE=FALSE.
    ///
    /// The DNFS_READBUFFERS parameter can be used to specify the number of read buffers used by the Direct NFS Client; the default is 4.
    /// </remarks>
    /// <example>
    /// The following example disables use of the Direct NFS Client on input data files during the load.
    /// DNFS_ENABLE=FALSE
    /// </example>
    [Parameter("DNFS_ENABLE")]
    public bool? DirectNfsEnable { get; set; }

    /// <summary>
    /// The DNFS_READBUFFERS parameter lets you control the number of read buffers used by the Direct NFS Client.
    /// </summary>
    /// <remarks>
    /// Default: 4
    /// The Direct NFS Client is an API that can be implemented by file servers to allow improved performance when an Oracle database accesses files on those servers.
    ///
    /// Using larger values might compensate for inconsistent I/O from the Direct NFS Client file server, but it may result in increased memory usage.
    ///
    /// Restrictions
    /// To use this parameter without also specifying the DNFS_ENABLE parameter, the input file must be larger than 1 GB.
    /// </remarks>
    /// <example>
    /// The following example specifies 10 read buffers for use by the Direct NFS Client.
    /// DNFS_READBUFFERS=10
    /// </example>
    [Parameter("DNFS_READBUFFERS")]
    public int? DirectNfsReadBuffers { get; set; }

    /// <summary>
    /// The ERRORS parameter specifies the maximum number of insert errors to allow.
    /// </summary>
    /// <remarks>
    /// Default: 50
    /// If the number of errors exceeds the value specified for ERRORS, then SQL*Loader terminates the load. Any data inserted up to that point is committed.
    ///
    /// To permit no errors at all, set ERRORS=0. To specify that all errors be allowed, use a very high number.
    ///
    /// SQL*Loader maintains the consistency of records across all tables.
    /// Therefore, multitable loads do not terminate immediately if errors exceed the error limit.
    /// When SQL*Loader encounters the maximum number of errors for a multitable load, it continues to load rows to ensure that valid rows previously loaded into tables are loaded into all tables and rejected rows are filtered out of all tables.
    ///
    /// In all cases, SQL*Loader writes erroneous records to the bad file.
    /// </remarks>
    /// <example>
    /// The following example specifies a maximum of 25 insert errors for the load. After that, the load is terminated.
    /// ERRORS=25
    /// </example>
    [Parameter("ERRORS")]
    public int? Errors { get; set; }

    /// <summary>
    /// The EXTERNAL_TABLE parameter instructs SQL*Loader whether to load data using the external tables option.
    /// </summary>
    /// <remarks>
    /// Default: NOT_USED
    /// If you use EXTERNAL_TABLE=EXECUTE and also use the SEQUENCE parameter in your SQL*Loader control file, then SQL*Loader creates a database sequence, loads the table using that sequence, and then deletes the sequence.
    /// The results of doing the load this way will be different than if the load were done with conventional or direct path.
    ///
    /// Note:
    ///
    ///     When the EXTERNAL_TABLE parameter is specified, any datetime data types (for example, TIMESTAMP) in a SQL*Loader control file are automatically converted to a CHAR data type and use the external tables date_format_spec clause.
    ///     See "date_format_spec".
    ///
    /// Note that the external table option uses directory objects in the database to indicate where all input data files are stored and to indicate where output files, such as bad files and discard files, are created.
    /// You must have READ access to the directory objects containing the data files, and you must have WRITE access to the directory objects where the output files are created.
    /// If there are no existing directory objects for the location of a data file or output file, then SQL*Loader will generate the SQL statement to create one.
    /// Therefore, when the EXECUTE option is specified, you must have the CREATE ANY DIRECTORY privilege. If you want the directory object to be deleted at the end of the load, then you must also have the DROP ANY DIRECTORY privilege.
    ///
    /// Note:
    ///
    ///     The EXTERNAL_TABLE=EXECUTE qualifier tells SQL*Loader to create an external table that can be used to load data and then executes the INSERT statement to load the data.
    ///     All files in the external table must be identified as being in a directory object. SQL*Loader attempts to use directory objects that already exist and that you have privileges to access.
    ///     However, if SQL*Loader does not find the matching directory object, then it attempts to create a temporary directory object.
    ///     If you do not have privileges to create new directory objects, then the operation fails.
    ///
    ///     To work around this, use EXTERNAL_TABLE=GENERATE_ONLY to create the SQL statements that SQL*Loader would try to execute.
    ///     Extract those SQL statements and change references to directory objects to be the directory object that you have privileges to access.
    ///     Then, execute those SQL statements.
    ///
    /// When using a multi-table load, SQL*Loader does the following:
    ///
    ///     1) Creates a table in the database that describes all fields in the input data file that will be loaded into any table.
    ///     2) Creates an INSERT statement to load this table from an external table description of the data.
    ///     3) Executes one INSERT statement for every table in the control file.
    ///
    /// To see an example of this, run case study 5, but add the EXTERNAL_TABLE=GENERATE_ONLY parameter.
    /// To guarantee unique names in the external table, SQL*Loader uses generated names for all fields.
    /// This is because the field names may not be unique across the different tables in the control file.
    ///
    /// Restrictions
    ///
    ///     Julian dates cannot be used when you insert data into a database table from an external table through SQL*Loader.
    ///     To work around this, use TO_DATE and TO_CHAR to convert the Julian date format, as shown in the following example:
    ///         TO_CHAR(TO_DATE(:COL1, 'MM-DD-YYYY'), 'J')
    ///
    ///     Built-in functions and SQL strings cannot be used for object elements when you insert data into a database table from an external table.
    /// </remarks>
    /// <example>
    /// EXTERNAL_TABLE=EXECUTE
    /// </example>
    [Parameter("EXTERNAL_TABLE")]
    public ExternalTableBehavior? ExternalTable { get; set; }

    /// <summary>
    /// The FILE parameter specifies the database file from which to allocate extents.
    /// </summary>
    /// <remarks>
    /// By varying the value of the FILE parameter for different SQL*Loader processes, data can be loaded onto a system with minimal disk contention.
    ///
    /// Restrictions
    ///
    ///     The FILE parameter is used only for direct path parallel loads.
    /// </remarks>
    /// <example>
    /// FILE=tablespace_file
    /// </example>
    [Parameter("FILE")]
    public string? DatabaseFile { get; set; }

    /// <summary>
    /// The LOAD parameter specifies the maximum number of records to load.
    /// </summary>
    /// <remarks>
    /// Default: All records are loaded.
    ///
    /// If you want to test that all parameters you have specified for the load are set correctly, you can use the LOAD parameter to specify a limited number of records rather than loading all records.
    /// No error occurs if fewer than the maximum number of records are found.
    /// </remarks>
    /// <example>
    /// The following example specifies that a maximum of 10 records be loaded.
    /// LOAD=10
    /// </example>
    [Parameter("LOAD")]
    public int? Load { get; set; }

    /// <summary>
    /// The LOG parameter specifies a directory path, or file name, or both for the log file that SQL*Loader uses to store logging information about the loading process.
    /// </summary>
    /// <remarks>
    /// If you specify the LOG parameter, then you must supply a directory name, or a file name, or both.
    ///
    /// If no directory name is specified, it defaults to the current directory.
    ///
    /// If a directory name is specified without a file name, then the default log file name is used.
    /// </remarks>
    /// <example>
    /// The following example creates a log file named emp1.log in the current directory. The extension .log is used even though it is not specified, because it is the default.
    /// LOG=emp1
    /// </example>
    [Parameter("LOG")]
    public string? LogFile { get; set; }

    /// <summary>
    /// Allows stream building on the client system to be done in parallel with stream loading on the server system.
    /// </summary>
    /// <remarks>
    /// Default: TRUE on multiple-CPU systems, FALSE on single-CPU systems
    ///
    /// By default, the multithreading option is always enabled (set to TRUE) on multiple-CPU systems.
    /// In this case, the definition of a multiple-CPU system is a single system that has more than one CPU.
    ///
    /// On single-CPU systems, multithreading is set to FALSE by default.
    /// To use multithreading between two single-CPU systems, you must enable multithreading; it will not be on by default.
    ///
    /// Restrictions
    ///
    ///     The MULTITHREADING parameter is available only for direct path loads.
    ///     Multithreading functionality is operating system-dependent. Not all operating systems support multithreading.
    /// </remarks>
    /// <example>
    /// The following example enables multithreading on a single-CPU system. On a multiple-CPU system it is enabled by default.
    /// MULTITHREADING=TRUE
    /// </example>
    [Parameter("MULTITHREADING")]
    public bool? Multithreading { get; set; }

    /// <summary>
    /// The NO_INDEX_ERRORS parameter determines whether indexing errors are tolerated during a direct path load.
    /// </summary>
    /// <remarks>
    /// A setting of NO_INDEX_ERRORS=FALSE means that if a direct path load results in an index becoming unusable then the rows are loaded and the index is left in an unusable state.
    /// This is the default behavior.
    ///
    /// A setting of NO_INDEX_ERRORS=TRUE means that if a direct path load results in any indexing errors, then the load is aborted. No rows are loaded and the indexes are left as they were.
    ///
    /// Restrictions
    ///
    ///     The NO_INDEX_ERRORS parameter is valid only for direct path loads. If it is specified for conventional path loads, then it is ignored.
    /// </remarks>
    /// <example>
    /// NO_INDEX_ERRORS=TRUE
    /// </example>
    [Parameter("NO_INDEX_ERRORS")]
    public bool? NoIndexErrors { get; set; }

    /// <summary>
    /// The PARALLEL parameter specifies whether loads that use direct path or external tables can operate in multiple concurrent sessions to load data into the same table.
    /// </summary>
    /// <remarks>
    /// Restrictions
    ///
    ///     The PARALLEL parameter is not valid in conventional path loads.
    /// </remarks>
    /// <example>
    /// The following example specifies that the load will be performed in parallel.
    /// PARALLEL=TRUE
    /// </example>
    [Parameter("PARALLEL")]
    public bool? Parallel { get; set; }

    /// <summary>
    /// The PARFILE parameter specifies the name of a file that contains commonly used command-line parameters.
    /// </summary>
    /// <remarks>
    /// Default: There is no default.
    ///
    /// Instead of specifying each parameter on the command line, you can simply specify the name of the parameter file.
    ///
    ///  Restrictions
    ///
    /// Although it is not usually important, on some systems it may be necessary to have no spaces around the equal sign (=) in the parameter specifications.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter(PARFILE)]
    public override string? ParametersFile { get; set; }

    /// <summary>
    /// The PARTITION_MEMORY parameter lets you limit the amount of memory used when you are loading many partitions.
    /// </summary>
    /// <remarks>
    /// This parameter is helpful in situations in which the number of partitions you are loading use up large amounts of memory, perhaps even exceeding available memory (this can happen especially when the data is compressed).
    ///
    /// Once the specified limit is reached, loading of some partition rows is delayed until memory use falls below the limit.
    ///
    /// The parameter value n is in kilobytes.
    ///
    /// If n is set to 0 (the default), then SQL*Loader uses a value that is a function of the PGA_AGGREGATE_TARGET initialization parameter.
    /// If n is set to -1 (minus 1), then SQL*Loader makes no attempt use less memory when loading many partitions.
    ///
    /// Restrictions
    ///
    ///     This parameter is only valid for direct path loads.
    ///     This parameter is available only in Oracle Database 12c Release 1 (12.1.0.2) and later.
    /// </remarks>
    /// <example>
    /// The following example limits memory use to 1 GB.
    /// : sqlldr hr CONTROL=t.ctl DIRECT=true PARTITION_MEMORY=1000000
    /// </example>
    [Parameter("PARTITION_MEMORY")]
    public int? PartitionMemory { get; set; }

    /// <summary>
    /// The READSIZE parameter lets you specify (in bytes) the size of the read buffer, if you choose not to use the default.
    /// </summary>
    /// <remarks>
    /// Default: 1048576
    /// In the conventional path method, the bind array is limited by the size of the read buffer.
    /// Therefore, the advantage of a larger read buffer is that more data can be read before a commit operation is required.
    ///
    /// Note:
    ///
    ///     If the READSIZE value specified is smaller than the BINDSIZE value, then the READSIZE value will be increased.
    ///
    /// Restrictions
    ///
    ///     The READSIZE parameter is used only when reading data from data files. When reading records from a control file, a value of 64 kilobytes (KB) is always used as the READSIZE.
    ///
    ///     The READSIZE parameter has no effect on LOBs. The size of the LOB read buffer is fixed at 64 kilobytes (KB).
    ///
    ///     The maximum size allowed is platform dependent.
    /// </remarks>
    /// <example>
    /// The following example sets the size of the read buffer to 500,000 bytes which means that commit operations will be required more often than if the default or a value larger than the default were used.
    /// READSIZE=500000
    /// </example>
    [Parameter("READSIZE")]
    public int? ReadSize { get; set; }

    /// <summary>
    /// The RESUMABLE parameter is used to enable and disable resumable space allocation.
    /// </summary>
    /// <remarks>
    /// Default: FALSE
    ///
    /// Restrictions
    ///
    ///     Because this parameter is disabled by default, you must set RESUMABLE=TRUE to use its associated parameters, RESUMABLE_NAME and RESUMABLE_TIMEOUT.
    /// </remarks>
    /// <example>
    /// The following example enables resumable space allocation:
    /// RESUMABLE=TRUE
    /// </example>
    [Parameter("RESUMABLE")]
    public bool? Resumable { get; set; }

    /// <summary>
    /// The RESUMABLE_NAME parameter identifies a statement that is resumable.
    /// </summary>
    /// <remarks>
    /// This value is a user-defined text string that is inserted in either the USER_RESUMABLE or DBA_RESUMABLE view to help you identify a specific resumable statement that has been suspended.
    ///
    /// Restrictions
    ///
    ///     This parameter is ignored unless the RESUMABLE parameter is set to TRUE to enable resumable space allocation.
    /// </remarks>
    /// <example>
    /// RESUMABLE_NAME='my resumable sql'
    /// </example>
    [Parameter("RESUMABLE_NAME")]
    public string? ResumableName { get; set; }

    /// <summary>
    /// The RESUMABLE_TIMEOUT parameter specifies the time period, in seconds, during which an error must be fixed.
    /// </summary>
    /// <remarks>
    /// Default: 7200 seconds (2 hours)
    /// If the error is not fixed within the timeout period, then execution of the statement is terminated, without finishing.
    ///
    /// Restrictions
    ///
    ///     This parameter is ignored unless the RESUMABLE parameter is set to TRUE to enable resumable space allocation.
    /// </remarks>
    /// <example>
    /// The following example specifies that errors must be fixed within ten minutes (600 seconds).
    /// RESUMABLE_TIMEOUT=600
    /// </example>
    [Parameter("RESUMABLE_TIMEOUT")]
    public int? ResumableTimeout { get; set; }

    /// <summary>
    /// For conventional path loads, the ROWS parameter specifies the number of rows in the bind array.
    /// </summary>
    /// <remarks>
    /// Default: Conventional path default is 64. Direct path default is all rows.
    ///  For direct path loads, the ROWS parameter specifies the number of rows to read from the data file(s) before a data save.
    ///
    /// Conventional path loads only: The ROWS parameter specifies the number of rows in the bind array. The maximum number of rows is 65534. See "Bind Arrays and Conventional Path Loads".
    ///
    /// Direct path loads only: The ROWS parameter identifies the number of rows you want to read from the data file before a data save.
    /// The default is to read all rows and save data once at the end of the load.
    /// See "Using Data Saves to Protect Against Data Loss".
    /// The actual number of rows loaded into a table on a save is approximately the value of ROWS minus the number of discarded and rejected records since the last save.
    ///
    /// Note:
    ///
    /// If you specify a low value for ROWS and then attempt to compress data using table compression, the compression ratio will probably be degraded.
    /// Oracle recommends that you either specify a high value or accept the default value when compressing data.
    ///
    /// Restrictions
    ///
    ///     The ROWS parameter is ignored for direct path loads when data is loaded into an Index Organized Table (IOT) or into a table containing VARRAYs, XML columns, or LOBs.
    ///     This means that the load still takes place, but no save points are done.
    /// </remarks>
    /// <example>
    /// In a conventional path load, the following example would result in an error because the specified value exceeds the allowable maximum of 65534 rows.
    /// ROWS=65900
    /// </example>
    [Parameter("ROWS")]
    public long? Rows { get; set; }

    /// <summary>
    /// The SILENT parameter suppresses some of the content that is written to the screen during a SQL*Loader operation.
    /// </summary>
    /// <remarks>
    /// Use the appropriate values to suppress one or more of the following (if more than one option is specified, they must be separated by commas)
    /// </remarks>
    /// <example>
    /// For example, you can suppress the header and feedback messages that normally appear on the screen with the following command-line argument:
    /// SILENT=HEADER, FEEDBACK
    /// </example>
    [Parameter("SILENT")]
    public SilentOptions[]? Silent { get; set; }

    /// <summary>
    /// The SKIP parameter specifies the number of logical records from the beginning of the file that should not be loaded.
    /// This allows you to continue loads that have been interrupted for some reason, without loading records that have already been processed.
    /// </summary>
    /// <remarks>
    /// The SKIP parameter can be used for all conventional loads, for single-table direct path loads, and for multiple-table direct path loads when the same number of records was loaded into each table.
    /// It cannot be used for multiple-table direct path loads when a different number of records was loaded into each table.
    /// If a WHEN clause is also present and the load involves secondary data, then the secondary data is skipped only if the WHEN clause succeeds for the record in the primary data file.
    ///
    /// Restrictions
    ///
    ///     The SKIP parameter cannot be used for external table loads.
    /// </remarks>
    /// <example>
    /// The following example skips the first 500 logical records in the data file(s) before proceeding with the load:
    /// SKIP=500
    /// </example>
    [Parameter("SKIP")]
    public long? Skip { get; set; }

    /// <summary>
    /// The SKIP_INDEX_MAINTENANCE parameter specifies whether to stop index maintenance for direct path loads.
    /// </summary>
    /// <remarks>
    /// If set to TRUE, this parameter causes the index partitions that would have had index keys added to them to instead be marked Index Unusable because the index segment is inconsistent with respect to the data it indexes.
    /// Index segments that are unaffected by the load retain the state they had before the load.
    ///
    /// The SKIP_INDEX_MAINTENANCE parameter:
    ///
    ///     Applies to both local and global indexes
    ///     Can be used (with the PARALLEL parameter) to do parallel loads on an object that has indexes
    ///     Can be used (with the PARTITION parameter on the INTO TABLE clause) to do a single partition load to a table that has global indexes
    ///     Puts a list (in the SQL*Loader log file) of the indexes and index partitions that the load set to an Index Unusable state
    ///
    /// Restrictions
    ///
    ///     The SKIP_INDEX_MAINTENANCE parameter does not apply to conventional path loads.
    ///
    ///     Indexes that are unique and marked Unusable are not allowed to skip index maintenance.
    ///     This rule is enforced by DML operations, and enforced by the direct path load to be consistent with DML.
    /// </remarks>
    /// <example>
    /// The following example stops index maintenance from taking place during a direct path load operation:
    /// SKIP_INDEX_MAINTENANCE=TRUE
    /// </example>
    [Parameter("SKIP_INDEX_MAINTENANCE")]
    public bool? SkipIndexMaintenance { get; set; }

    /// <summary>
    /// The SKIP_UNUSABLE_INDEXES parameter specifies whether to skip an index encountered in an Index Unusable state and continue the load operation.
    /// </summary>
    /// <remarks>
    /// Default: The value of the Oracle Database configuration parameter, SKIP_UNUSABLE_INDEXES, as specified in the initialization parameter file.
    /// The default database setting is TRUE.
    ///
    /// A value of TRUE for SKIP_UNUSABLE_INDEXES means that if an index in an Index Unusable state is encountered, it is skipped and the load operation continues.
    /// This allows SQL*Loader to load a table with indexes that are in an Unusable state prior to the beginning of the load.
    /// Indexes that are not in an Unusable state at load time will be maintained by SQL*Loader.
    /// Indexes that are in an Unusable state at load time will not be maintained but will remain in an Unusable state at load completion.
    ///
    /// Both SQL*Loader and Oracle Database provide a SKIP_UNUSABLE_INDEXES parameter.
    /// The SQL*Loader SKIP_UNUSABLE_INDEXES parameter is specified at the SQL*Loader command line.
    /// The Oracle Database SKIP_UNUSABLE_INDEXES parameter is specified as a configuration parameter in the initialization parameter file.
    /// It is important to understand how they affect each other.
    ///
    /// If you specify a value for SKIP_UNUSABLE_INDEXES at the SQL*Loader command line, then it overrides the value of the SKIP_UNUSABLE_INDEXES configuration parameter in the initialization parameter file.
    ///
    /// If you do not specify a value for SKIP_UNUSABLE_INDEXES at the SQL*Loader command line, then SQL*Loader uses the Oracle Database setting for the SKIP_UNUSABLE_INDEXES configuration parameter, as specified in the initialization parameter file.
    /// If the initialization parameter file does not specify a setting for SKIP_UNUSABLE_INDEXES, then the default setting is TRUE.
    ///
    /// The SKIP_UNUSABLE_INDEXES parameter applies to both conventional and direct path loads.
    ///
    /// Restrictions
    ///
    ///     Indexes that are unique and marked Unusable are not allowed to skip index maintenance.
    ///     This rule is enforced by DML operations, and enforced by the direct path load to be consistent with DML.
    /// </remarks>
    /// <example>
    /// If the Oracle Database initialization parameter had a value of SKIP_UNUSABLE_INDEXES=FALSE, then the following parameter on the SQL*Loader command line would override it.
    /// Therefore, if an index in an Index Unusable state is encountered, it is skipped and the load operation continues.
    /// SKIP_UNUSABLE_INDEXES=TRUE
    /// </example>
    [Parameter("SKIP_UNUSABLE_INDEXES")]
    public bool? SkipUnusableIndexes { get; set; }

    /// <summary>
    /// The STREAMSIZE parameter specifies the size (in bytes) of the data stream sent from the client to the server.
    /// </summary>
    /// <remarks>
    /// Default: 256000
    /// The STREAMSIZE parameter specifies the size of the direct path stream buffer.
    /// The number of column array rows (specified with the COLUMNARRAYROWS parameter) determines the number of rows loaded before the stream buffer is built.
    /// The optimal values for these parameters vary, depending on the system, input data types, and Oracle column data types used.
    /// When you are using optimal values for your particular configuration, the elapsed time in the SQL*Loader log file should go down.
    /// Restrictions
    ///
    ///     The STREAMSIZE parameter applies only to direct path loads.
    ///     The minimum value for STREAMSIZE is 65536. If a value lower than 65536 is specified, then 65536 is used instead.
    /// </remarks>
    /// <example>
    /// The following example specifies a direct path stream buffer size of 300,000 bytes.
    /// STREAMSIZE=300000
    /// </example>
    [Parameter("STREAMSIZE")]
    public long? StreamSize { get; set; }

    /// <summary>
    /// The TRIM parameter specifies that spaces should be trimmed from the beginning of a text field, the end of a text field, or both.
    /// </summary>
    /// <remarks>
    /// Spaces include blanks and other nonprinting characters such as tabs, line feeds, and carriage returns.
    ///
    /// If trimming is specified for a field that is all spaces, then the field is set to NULL.
    ///
    /// Restrictions
    ///
    ///     The TRIM parameter is valid only when the external table load method is used.
    /// </remarks>
    /// <example>
    /// The following example would result in a load operation for which no characters are trimmed from any fields:
    /// TRIM=NOTRIM
    /// </example>
    [Parameter("TRIM")]
    public TrimBehavior? Trim { get; set; }

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
    ///
    [JsonIgnore] //prevent accidentally leaking this property into logs which can contain user AND password
    [IgnoreDataMember]
    [Parameter("USERID")]
    public string? UserId { get; set; }

    public override OracleTool OracleTool => OracleTool.db2cmd;

    internal override string CoerceBool(bool b) => TrueFalse(b);

    internal override bool CoerceBool(string s) => s == "TRUE";
}