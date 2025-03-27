using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Tonic.Common.Extensions;

namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Enables you to alter object creation DDL for objects being imported.
/// </summary>
/// <remarks>
/// https://docs.oracle.com/database/121/SUTIL/GUID-64FB67BD-EB67-4F50-A4D2-5D34518E6BDB.htm#SUTIL939
/// </remarks>
public static class TransformFactory
{
    private static readonly Dictionary<string, TransformCacheValue> _cache;
    private static readonly Dictionary<ParFileSerializer.EnumValueKey, Enum?> _enumCache;

    private const string DISABLE_ARCHIVE_LOGGING = nameof(DISABLE_ARCHIVE_LOGGING);
    private const string INMEMORY = nameof(INMEMORY);
    private const string INMEMORY_CLAUSE = nameof(INMEMORY_CLAUSE);
    private const string LOB_STORAGE = nameof(LOB_STORAGE);
    private const string OID = nameof(OID);
    private const string PCTSPACE = nameof(PCTSPACE);
    private const string SEGMENT_ATTRIBUTES = nameof(SEGMENT_ATTRIBUTES);
    private const string SEGMENT_CREATION = nameof(SEGMENT_CREATION);
    private const string STORAGE = nameof(STORAGE);
    private const string TABLE_COMPRESSION_CLAUSE = nameof(TABLE_COMPRESSION_CLAUSE);

    static TransformFactory()
    {
        _cache = new Dictionary<string, TransformCacheValue>();

        _cache[DISABLE_ARCHIVE_LOGGING] = new TransformCacheValue(DISABLE_ARCHIVE_LOGGING, typeof(bool), DisableArchiveLogging);
        _cache[INMEMORY] = new TransformCacheValue(INMEMORY, typeof(bool), InMemory);
        _cache[INMEMORY_CLAUSE] = new TransformCacheValue(INMEMORY_CLAUSE, typeof(string), InMemoryClause);
        _cache[LOB_STORAGE] = new TransformCacheValue(LOB_STORAGE, typeof(LargeObjectStorage), LargeObjectStorage);
        _cache[OID] = new TransformCacheValue(OID, typeof(bool), ObjectId);
        _cache[PCTSPACE] = new TransformCacheValue(PCTSPACE, typeof(long), PercentMultiplierSpace);
        _cache[SEGMENT_ATTRIBUTES] = new TransformCacheValue(SEGMENT_ATTRIBUTES, typeof(bool), SegmentAttributes);
        _cache[SEGMENT_CREATION] = new TransformCacheValue(SEGMENT_CREATION, typeof(bool), SegmentCreation);
        _cache[STORAGE] = new TransformCacheValue(STORAGE, typeof(bool), Storage);
        _cache[TABLE_COMPRESSION_CLAUSE] = new TransformCacheValue(TABLE_COMPRESSION_CLAUSE, typeof(string), TableCompressionClause);

        _enumCache = new Dictionary<ParFileSerializer.EnumValueKey, Enum?>();

        ParFileSerializer.CacheEnum(typeof(LargeObjectStorage), OnCacheEnum);
    }

    internal static Transform? Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentNullException(nameof(s));
        }
        int indexOfColon = s.IndexOf(":");
        if (indexOfColon < 0)
        {
            throw new ArgumentException($"Invalid {nameof(Transform)} {s}", nameof(s));
        }
        var transformName = s.Substring(0, indexOfColon);
        var transformValueRaw = s.Substring(indexOfColon + 1);

        int indexOfSecondColon = transformValueRaw.IndexOf(":");

        OracleObjectType? oracleObjectType = null;
        if (indexOfSecondColon > -1)
        {
            var oracleObjectTypeRaw = transformValueRaw.Substring(indexOfSecondColon + 1);

            ParFileSerializer.ParseObjectTypeOrThrow(oracleObjectTypeRaw, out oracleObjectType);

            transformValueRaw = transformValueRaw.Substring(0, indexOfSecondColon);
        }

        var cacheValue = TransformFactory._cache[transformName];
        object? transformValue = null;
        if (cacheValue.DataType.IsEnum)
        {
            ParFileSerializer.EnumValueKey enumValueKey = new ParFileSerializer.EnumValueKey(cacheValue.DataType, transformValueRaw);
            transformValue = TransformFactory._enumCache[enumValueKey];
        }
        else if (cacheValue.DataType == typeof(bool))
        {
            transformValue = transformValueRaw == ParametersBase.YorN(true);
        }
        else if (cacheValue.DataType.IsAssignableTo(typeof(IConvertible)))
        {
            transformValue = Convert.ChangeType(transformValueRaw, cacheValue.DataType, CultureInfo.InvariantCulture);
        }

        return (Transform?) cacheValue.FactoryMethod.DynamicInvoke(transformValue, oracleObjectType);
    }

    /// <summary>
    /// If set to Y, then the logging attributes for the specified object types (TABLE and/or INDEX) are disabled before the data is imported.
    /// If set to N (the default), then archive logging is not disabled during import. After the data has been loaded, the logging attributes for the objects are restored to their original settings.
    /// If no object type is specified, then the DISABLE_ARCHIVE_LOGGING behavior is applied to both TABLE and INDEX object types.
    /// This transform works for both file mode imports and network mode imports. It does not apply to transportable tablespace imports.
    /// </summary>
    /// <remarks>
    /// http://www.dba-oracle.com/t_data_pump_disable_archive_logging.htm
    /// </remarks>
    public static Transform DisableArchiveLogging(bool value, OracleObjectType? objectType = null) =>
        new Transform(DISABLE_ARCHIVE_LOGGING, ParametersBase.YorN(value), objectType);
    /// <summary>
    /// The INMEMORY transform is related to the In-Memory Column Store (IM column store).
    /// The IM column store is an optional portion of the system global area (SGA) that stores copies of tables, table partitions, and other database objects.
    /// In the IM column store, data is populated by column rather than row as it is in other parts of the SGA, and data is optimized for rapid scans.
    /// The IM column store does not replace the buffer cache, but acts as a supplement so that both memory areas can store the same data in different formats.
    /// The IM column store is included with the Oracle Database In-Memory option.
    ///
    /// If Y (the default value) is specified on import, then Data Pump keeps the IM column store clause for all objects that have one.
    /// When those objects are recreated at import time, Data Pump generates the IM column store clause that matches the setting for those objects at export time.
    ///
    /// If N is specified on import, then Data Pump drops the IM column store clause from all objects that have one.
    /// If there is no IM column store clause for an object that is stored in a tablespace, then the object inherits the IM column store clause from the tablespace.
    /// So if you are migrating a database and want the new database to use IM column store features, you could pre-create the tablespaces with the appropriate IM column store clause and then use TRANSFORM=INMEMORY:N on the import command.
    /// The object would then inherit the IM column store clause from the new pre-created tablespace.
    ///
    /// If you do not use the INMEMORY transform, then you must individually alter every object to add the appropriate IM column store clause.
    ///
    /// Note:
    ///
    ///     The INMEMORY transform is available only in Oracle Database 12c Release 1 (12.1.0.2) or later.
    /// </summary>
    public static Transform InMemory(bool value, OracleObjectType? objectType = null) =>
        new Transform(INMEMORY, ParametersBase.YorN(value), objectType);
    /// <summary>
    /// The INMEMORY_CLAUSE transform is related to the In-Memory Column Store (IM column store).
    /// The IM column store is an optional portion of the system global area (SGA) that stores copies of tables, table partitions, and other database objects.
    /// In the IM column store, data is populated by column rather than row as it is in other parts of the SGA, and data is optimized for rapid scans.
    /// The IM column store does not replace the buffer cache, but acts as a supplement so that both memory areas can store the same data in different formats.
    /// The IM column store is included with the Oracle Database In-Memory option.
    ///
    /// When you specify this transform, Data Pump uses the contents of the string as the INMEMORY_CLAUSE for all objects being imported that have an IM column store clause in their DDL.
    /// This transform is useful when you want to override the IM column store clause for an object in the dump file.
    ///
    /// Note:
    ///
    ///     The INMEMORY_CLAUSE transform is available only in Oracle Database 12c Release 1 (12.1.0.2) or later.
    /// </summary>
    public static Transform InMemoryClause(string clause, OracleObjectType? objectType = null) =>
        new Transform(INMEMORY_CLAUSE, clause, objectType);
    /// <summary>
    /// LOB segments are created with the specified storage, either SECUREFILE or BASICFILE. If the value is NO_CHANGE (the default), the LOB segments are created with the same storage they had in the source database.
    /// If the value is DEFAULT, then the keyword (SECUREFILE or BASICFILE) is omitted and the LOB segment is created with the default storage.
    ///
    /// Specifying this transform changes LOB storage for all tables in the job, including tables that provide storage for materialized views.
    ///
    /// The LOB_STORAGE transform is not valid in transportable import jobs.
    /// </summary>
    public static Transform LargeObjectStorage(LargeObjectStorage value, OracleObjectType? objectType = null) =>
        new Transform(LOB_STORAGE, EnumExtension.GetAttribute<ParameterAttribute>(value)?.Name, objectType);
    /// <summary>
    /// If Y (the default value) is specified on import, then the exported OIDs are assigned to new object tables and types.
    /// Data Pump also performs OID checking when looking for an existing matching type on the target database.
    ///
    /// If N is specified on import, then:
    ///     The assignment of the exported OID during the creation of new object tables and types is inhibited. Instead, a new OID is assigned.
    ///     This can be useful for cloning schemas, but does not affect referenced objects.
    ///
    ///     Prior to loading data for a table associated with a type, Data Pump skips normal type OID checking when looking for an existing matching type on the target database.
    ///     Other checks using a type's hash code, version number, and type name are still performed.
    /// </summary>
    public static Transform ObjectId(bool value, OracleObjectType? objectType = null) =>
        new Transform(OID, ParametersBase.YorN(value), objectType);
    /// <summary>
    /// The value supplied for this transform must be a number greater than zero.
    /// It represents the percentage multiplier used to alter extent allocations and the size of data files.
    /// Note that you can use the PCTSPACE transform with the Data Pump Export SAMPLE parameter so that the size of storage allocations matches the sampled data subset. (See "SAMPLE".)
    /// </summary>
    public static Transform PercentMultiplierSpace(long value, OracleObjectType? objectType = null) =>
        new Transform(PCTSPACE, value, objectType);
    /// <summary>
    /// If the value is specified as Y, then segment attributes (physical attributes, storage attributes, tablespaces, and logging) are included, with appropriate DDL.
    /// The default is Y.
    /// </summary>
    public static Transform SegmentAttributes(bool value, OracleObjectType? objectType = null) =>
        new Transform(SEGMENT_ATTRIBUTES, ParametersBase.YorN(value), objectType);
    /// <summary>
    /// If set to Y (the default), then this transform causes the SQL SEGMENT CREATION clause to be added to the CREATE TABLE statement.
    /// That is, the CREATE TABLE statement will explicitly say either SEGMENT CREATION DEFERRED or SEGMENT CREATION IMMEDIATE.
    /// If the value is N, then the SEGMENT CREATION clause is omitted from the CREATE TABLE statement.
    /// Set this parameter to N to use the default segment creation attributes for the table(s) being loaded.
    /// (This functionality is available starting with Oracle Database 11g release 2 (11.2.0.2).)
    /// </summary>
    public static Transform SegmentCreation(bool value, OracleObjectType? objectType = null) =>
        new Transform(SEGMENT_CREATION, ParametersBase.YorN(value), objectType);
    /// <summary>
    /// If the value is specified as Y, then the storage clauses are included, with appropriate DDL.
    /// The default is Y.
    /// This parameter is ignored if SEGMENT_ATTRIBUTES=N.
    /// </summary>
    public static Transform Storage(bool value, OracleObjectType? objectType = null) =>
        new Transform(STORAGE, ParametersBase.YorN(value), objectType);
    /// <summary>
    /// If NONE is specified, the table compression clause is omitted (and the table gets the default compression for the tablespace).
    /// Otherwise the value is a valid table compression clause (for example, NOCOMPRESS, COMPRESS BASIC, and so on).
    /// Tables are created with the specified compression.
    /// See Oracle Database SQL Language Reference for information about valid table compression syntax.
    ///
    /// If the table compression clause is more than one word, it must be contained in single or double quotation marks.
    ///
    /// If the table compression clause is more than one word, then it must be contained in single or double quotation marks.
    /// Additionally, depending on your operating system requirements, you may need to enclose the clause in escape characters (such as the backslash character).
    /// For example:
    ///
    ///     TRANSFORM=TABLE_COMPRESSION_CLAUSE:\"COLUMN STORE COMPRESS FOR QUERY HIGH\"
    /// Specifying this transform changes the type of compression for all tables in the job, including tables that provide storage for materialized views.
    /// </summary>
    public static Transform TableCompressionClause(string clause, OracleObjectType? objectType = null) =>
        new Transform(TABLE_COMPRESSION_CLAUSE, clause, objectType);


    private sealed record TransformCacheValue(string Name, Type DataType, Delegate FactoryMethod);

    private static void OnCacheEnum(FieldInfo enumField, ParameterAttribute attribute, Enum? enumValue)
    {
        var valueKey = new ParFileSerializer.EnumValueKey(enumField.FieldType, attribute.Name);
        _enumCache[valueKey] = enumValue;
    }

    /// <inheritdoc cref="IsObjectTypeValidForTransform"/>
    internal static void Validate(Transform transform, ref List<string> validationErrors)
    {
        if (!IsObjectTypeValidForTransform(transform))
        {
            validationErrors.Add($"{nameof(OracleObjectType)} '{transform.ObjectType}' is not supported by {nameof(Transform)} '{transform.Name}'");
        }
    }

    /// <remarks>
    /// See Table 3-1 Valid Object Types for the Data Pump import TRANSFORM parameter
    ///
    ///     https://docs.oracle.com/database/121/SUTIL/GUID-64FB67BD-EB67-4F50-A4D2-5D34518E6BDB.htm#SUTIL939
    /// </remarks>
    internal static bool IsObjectTypeValidForTransform(Transform transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        switch (transform.Name)
        {
            case DISABLE_ARCHIVE_LOGGING:
                if (transform.ObjectType.HasValue &&
                    !transform.ObjectType.Value.EqualsAny(
                        OracleObjectType.Index,
                                    OracleObjectType.Table
                        )
                    )
                {
                    return false;
                }
                break;
            case INMEMORY:
            case INMEMORY_CLAUSE:
                if (transform.ObjectType.HasValue &&
                    !transform.ObjectType.Value.EqualsAny(
                        OracleObjectType.Table,
                        OracleObjectType.TableSpace
                    )
                   )
                {
                    return false;
                }
                break;
            case LOB_STORAGE:
            case SEGMENT_CREATION:
            case TABLE_COMPRESSION_CLAUSE:
                if (transform.ObjectType.HasValue &&
                    !transform.ObjectType.Value.EqualsAny(
                        OracleObjectType.Table,
                        OracleObjectType.TableSpace
                    )
                   )
                {
                    return false;
                }
                break;
            case OID:
                if (transform.ObjectType.HasValue &&
                    !transform.ObjectType.Value.EqualsAny(
                        OracleObjectType.Table,
                        OracleObjectType.Type
                    )
                   )
                {
                    return false;
                }
                break;
            case PCTSPACE:
            case SEGMENT_ATTRIBUTES:
                if (transform.ObjectType.HasValue &&
                    !transform.ObjectType.Value.EqualsAny(
                        OracleObjectType.Cluster,
                        OracleObjectType.Constraint,
                        OracleObjectType.Index,
                        OracleObjectType.RollbackSegment,
                        OracleObjectType.Table,
                        OracleObjectType.TableSpace
                    )
                   )
                {
                    return false;
                }
                break;
            case STORAGE:
                if (transform.ObjectType.HasValue &&
                    !transform.ObjectType.Value.EqualsAny(
                        OracleObjectType.Cluster,
                        OracleObjectType.Constraint,
                        OracleObjectType.Index,
                        OracleObjectType.RollbackSegment,
                        OracleObjectType.Table
                    )
                   )
                {
                    return false;
                }
                break;
            default:
                throw new ArgumentException($"Unsupported named {nameof(Transform)}: {transform.Name}");
        }

        return true;
    }
}