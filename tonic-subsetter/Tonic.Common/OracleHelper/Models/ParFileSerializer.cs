using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Tonic.Common.Extensions;
using Tonic.Common.OracleHelper.Models.DataPump;
using Tonic.Common.OracleHelper.Models.SqlLoader;

namespace Tonic.Common.OracleHelper.Models;

/// <summary>
/// Strongly-typed Serializer for the Oracle PAR file format
/// </summary>
/// <remarks>
///     This format is used by Data Pump Import (impdp)/Export (expdp) as well as SqlLoader (sqlldr)
///
///     https://docs.oracle.com/database/121/SUTIL/GUID-7A045C82-5993-44EB-AFAD-B7D39C34BCCD.htm#SUTIL859
///
/// NOTE
///     End-of-line comments are currently not preserved round-trip because the parameters objects do not model them
/// </remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class ParFileSerializer
{
    private static readonly PropertyInfo[] _importParameters;
    private static readonly PropertyInfo[] _exportParameters;
    private static readonly PropertyInfo[] _loaderParameters;
    private static readonly Dictionary<string, ParameterCacheValue> _importParameterCache;
    private static readonly Dictionary<string, ParameterCacheValue> _exportParameterCache;
    private static readonly Dictionary<string, ParameterCacheValue> _loaderParameterCache;
    private static readonly Dictionary<Enum, ParameterCacheValue> _enumTypeCache;
    private static readonly Dictionary<EnumValueKey, Enum> _enumValueCache;
    private static readonly Type TypeOfOracleObjectType = typeof(OracleObjectType);
    private static readonly PropertyInfo? _headerCommentsProperty;

    static ParFileSerializer()
    {
        _headerCommentsProperty = typeof(ParametersBase).GetProperty(nameof(ParametersBase.HeaderComments));

        _enumTypeCache = new Dictionary<Enum, ParameterCacheValue>();
        _enumValueCache = new Dictionary<EnumValueKey, Enum>();

        _importParameterCache = new Dictionary<string, ParameterCacheValue>();
        _importParameters = CacheProperties(typeof(DataPumpImportParameters), _importParameterCache);

        _exportParameterCache = new Dictionary<string, ParameterCacheValue>();
        _exportParameters = CacheProperties(typeof(DataPumpExportParameters), _exportParameterCache);

        _loaderParameterCache = new Dictionary<string, ParameterCacheValue>();
        _loaderParameters = CacheProperties(typeof(SqlLoaderParameters), _loaderParameterCache);
    }

    /// <summary>
    /// Caches the reflection-work for the PAR component objects up front
    /// </summary>
    private static PropertyInfo[] CacheProperties(Type type, Dictionary<string, ParameterCacheValue> cache)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        if (cache == null)
        {
            throw new ArgumentNullException(nameof(cache));
        }
        if (!type.IsAssignableTo(typeof(ParametersBase)))
        {
            throw new ArgumentException($"{nameof(type)} must derive from {nameof(ParametersBase)}", nameof(type));
        }

        var nonParProperties = new string[]
        {
            nameof(ParametersBase.HeaderComments),
            nameof(ParametersBase.OracleTool),
            nameof(ParametersBase.ParametersFile)
        };

        var properties = type.GetProperties()
                                        .Where(x => !nonParProperties.Contains(x.Name))
                                        .OrderBy(x => x.Name).ToArray();

        for (int i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var attribute = property.GetCustomAttribute<ParameterAttribute>();
            if (attribute == null)
            {
                throw new ArgumentNullException($"{type.Name}.{property.Name} must have a {nameof(ParameterAttribute)}");
            }

            var cacheValue = new ParameterCacheValue(property, attribute);
            cache[property.Name] = cacheValue;
            cache[attribute.Name] = cacheValue;

            var propertyType = property.PropertyType;
            if (IsNullableEnum(propertyType, out var enumType))
            {
                CacheEnum(enumType, OnCacheEnum);
            }

            if (propertyType.IsArray &&
                propertyType.GetElementType() is var elementType &&
                elementType is not null &&
                elementType.IsEnum)
            {
                CacheEnum(elementType, OnCacheEnum);
            }
        }

        return properties;
    }

    private static void OnCacheEnum(FieldInfo enumField, ParameterAttribute attribute, Enum enumValue)
    {
        var cacheValue = new ParameterCacheValue(enumField, attribute);

        _enumTypeCache[enumValue] = cacheValue;
        var valueKey = new EnumValueKey(enumField.FieldType, cacheValue.ParameterAttribute.Name);
        _enumValueCache[valueKey] = enumValue;
    }

    /// <summary>
    /// Writes parameters to individual lines in the PAR file format
    /// </summary>
    public static string Serialize(ParametersBase parameters) => string.Join(Environment.NewLine, SerializeLines(parameters));

    /// <summary>
    /// Writes parameters to individual lines in the PAR file format
    /// </summary>
    public static string[] SerializeLines(ParametersBase parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        List<string> lines = new List<string>();

        var headerComments = parameters.HeaderComments;
        if (headerComments != null &&
            headerComments.Length > 0)
        {
            for (int i = 0; i < headerComments.Length; i++)
            {
                lines.Add($"#{headerComments[i]}");
            }
        }
        else
        {
            lines.Add($"#PARFILE for {parameters.OracleTool}");
        }

        PropertyInfo[] properties;
        Dictionary<string, ParameterCacheValue> cache;

        if (parameters is DataPumpImportParameters)
        {
            properties = _importParameters;
            cache = _importParameterCache;
        }
        else if (parameters is DataPumpExportParameters)
        {
            properties = _exportParameters;
            cache = _exportParameterCache;
        }
        else if (parameters is SqlLoaderParameters)
        {
            properties = _loaderParameters;
            cache = _loaderParameterCache;
        }
        else
        {
            throw new ArgumentException($"Unexpected type {parameters.GetType()}");
        }

        for (int i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var propertyType = property.PropertyType;
            object? propertyValue = property.GetValue(parameters);
            if (!ReferenceEquals(propertyValue, null))
            {
                if (!cache.TryGetValue(property.Name, out var cacheValue))
                {
                    throw new ArgumentException($"{property.Name} was not located in the cache of all properties");
                }

                if (propertyValue is string[] strings)
                {
                    for (int j = 0; j < strings.Length; j++)
                    {
                        var statement = $"{cacheValue.ParameterAttribute.Name}={strings[j]}";

                        lines.Add(statement);
                    }
                }
                else if (propertyType.IsArray)
                {
                    var elementType = propertyType.GetElementType();
                    if (elementType == TypeOfOracleObjectType &&
                        propertyValue is OracleObjectType[] objectTypes)
                    {
                        for (int j = 0; j < objectTypes.Length; j++)
                        {
                            var statement = $"{cacheValue.ParameterAttribute.Name}={_enumTypeCache[objectTypes[j]].ParameterAttribute.Name}";

                            lines.Add(statement);
                        }
                    }
                    else if (elementType == typeof(SilentOptions) &&
                             propertyValue is SilentOptions[] silentOptions)
                    {
                        for (int j = 0; j < silentOptions.Length; j++)
                        {
                            var statement = $"{cacheValue.ParameterAttribute.Name}={_enumTypeCache[silentOptions[j]].ParameterAttribute.Name}";

                            lines.Add(statement);
                        }
                    }
                    else if (elementType == typeof(Transform) &&
                             propertyValue is Transform[] transforms)
                    {
                        for (int j = 0; j < transforms.Length; j++)
                        {
                            var transform = transforms[j];
                            var statement = $"{cacheValue.ParameterAttribute.Name}={transform.Name}:{transform.Value}";

                            if (transform.ObjectType.HasValue)
                            {
                                statement = $"{statement}:{_enumTypeCache[transform.ObjectType.Value].ParameterAttribute.Name}";
                            }

                            lines.Add(statement);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected array of {elementType}");
                    }
                }
                else
                {
                    var value = CoerceValue(parameters, cacheValue, propertyValue);

                    var statement = $"{cacheValue.ParameterAttribute.Name}={value}";

                    lines.Add(statement);
                }
            }
        }

        return lines.ToArray();
    }

    /// <summary>
    /// Reads parameters from a string in PAR file format
    /// </summary>
    public static T Deserialize<T>(string parFileContent)
        where T : ParametersBase, new() => Deserialize<T>(parFileContent.Split(Environment.NewLine));

    /// <summary>
    /// Reads parameters from individual lines in PAR file format
    /// </summary>
    public static T Deserialize<T>(string[] parFileLines)
        where T : ParametersBase, new()
    {
        if (parFileLines == null || parFileLines.Length == 0)
        {
            throw new ArgumentNullException(nameof(parFileLines));
        }

        Type typeOfT = typeof(T);

        Dictionary<string, ParameterCacheValue> cache;

        if (typeOfT == typeof(DataPumpImportParameters))
        {
            cache = _importParameterCache;
        }
        else if (typeOfT == typeof(DataPumpExportParameters))
        {
            cache = _exportParameterCache;
        }
        else if (typeOfT == typeof(SqlLoaderParameters))
        {
            cache = _loaderParameterCache;
        }
        else
        {
            throw new ArgumentException($"Unexpected type {typeOfT}");
        }

        T result = new T();

        for (int i = 0; i < parFileLines.Length; i++)
        {
            var line = parFileLines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("#"))
            {
                string comment = line.Substring(1);
                CreateOrAppendToArray(result, _headerCommentsProperty, comment);
                continue;
            }

            var indexOfEquals = line.IndexOf("=", StringComparison.InvariantCultureIgnoreCase);
            if (indexOfEquals < 0)
            {
                throw new ArgumentException($"Line[{i}]: missing parameter operator: {line}");
            }

            var parameterName = line.Substring(0, indexOfEquals);
            var parameterValueRaw = line.Substring(indexOfEquals + 1);

            var property = (PropertyInfo) cache[parameterName].Member;
            var propertyType = property.PropertyType;
            object? parameterValue;

            if (propertyType.IsArray)
            {
                if (propertyType == typeof(string[]))
                {
                    CreateOrAppendToArray(result, property, parameterValueRaw);
                }
                else if (propertyType == typeof(Transform[]))
                {
                    Transform? transform = Transform.Parse(parameterValueRaw);
                    CreateOrAppendToArray(result, property, transform);
                }
                else if (propertyType == typeof(OracleObjectType[]))
                {
                    ParseObjectTypeOrThrow(parameterValueRaw, out var objectType);
                    if (objectType == null)
                    {
                        throw new ArgumentException($"null encountered deserializing {nameof(OracleObjectType)}", nameof(objectType));
                    }
                    CreateOrAppendToArray(result, property, (OracleObjectType) objectType);
                }
                else if (propertyType == typeof(SilentOptions[]))
                {
                    var valueKey = new EnumValueKey(typeof(SilentOptions), parameterValueRaw);
                    SilentOptions objectType = (SilentOptions) _enumValueCache[valueKey];
                    CreateOrAppendToArray(result, property, objectType);
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected array property type {propertyType}");
                }
            }
            else if (propertyType == typeof(bool?))
            {
                parameterValue = result.CoerceBool(parameterValueRaw);
                property.SetPropertyValue(result, parameterValue);
            }
            else if (propertyType.IsAssignableTo(typeof(IConvertible)))
            {
                parameterValue = Convert.ChangeType(parameterValueRaw, propertyType, CultureInfo.InvariantCulture);
                property.SetPropertyValue(result, parameterValue);
            }
            else if (IsNullableEnum(propertyType, out var enumType))
            {
                var valueKey = new EnumValueKey(enumType, parameterValueRaw);
                object? enumValue = ActivateNullable(enumType, _enumValueCache[valueKey]);
                parameterValue = enumValue;
                property.SetPropertyValue(result, parameterValue, null);
            }
            else if (IsNullableValueType(propertyType, out var underlyingType))
            {
                object? nullableValue = ActivateNullable(enumType, Convert.ChangeType(parameterValueRaw, underlyingType, CultureInfo.InvariantCulture));
                parameterValue = nullableValue;
                property.SetPropertyValue(result, parameterValue, null);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected property type {propertyType}");
            }
        }

        return result;
    }

    private static void CreateOrAppendToArray<T>(object result, PropertyInfo? property, T value)
    {
        var array = (T[]?) property?.GetValue(result);
        T[] newArray;
        if (ReferenceEquals(array, null))
        {
            newArray = new[] { value };
        }
        else
        {
            newArray = new T[array.Length + 1];
            array.CopyTo(newArray, 0);
            newArray[^1] = value;
        }
        property.SetPropertyValue(result, newArray);
    }

    private sealed record ParameterCacheValue(MemberInfo Member, ParameterAttribute ParameterAttribute);

    private static string CoerceValue(ParametersBase parameters, ParameterCacheValue cacheValue, object propertyValue)
    {
        if (propertyValue is bool b)
        {
            return parameters.CoerceBool(b);
        }

        if (propertyValue is Enum e)
        {
            return _enumTypeCache[e].ParameterAttribute.Name;
        }

        if (propertyValue is Transform t)
        {
            return $"{cacheValue.ParameterAttribute.Name}={t.Name}:{t.Value}";
        }

        if (propertyValue is string s)
        {
            if (cacheValue.Member.Name == nameof(DataPumpExportParameters.FileSize))
            {
                //ensure that it parses correctly
                var digits = string.Concat(s.Where(char.IsDigit));
                var letters = string.Concat(s.Where(x => !char.IsDigit(x)));

                var count = long.Parse(digits);
                var unit = Enum.Parse<FileSizeUnit>(letters);

                return $"{count}{unit}";
            }
        }

        return propertyValue.ToString()!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNullableEnum(this Type type, [NotNullWhen(true)] out Type? underlyingEnumType)
    {
        underlyingEnumType = Nullable.GetUnderlyingType(type);
        return underlyingEnumType is { IsEnum: true };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNullableValueType(this Type type, [NotNullWhen(true)] out Type? underlyingType)
    {
        underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType is { IsEnum: false, IsValueType: true };
    }

    private static object? ActivateNullable(Type? underlyingType, object value)
    {
        if (ReferenceEquals(underlyingType, null))
        {
            throw new ArgumentNullException(nameof(underlyingType));
        }
        if (ReferenceEquals(value, null))
        {
            throw new ArgumentNullException(nameof(value));
        }

        Type[] typeArgs = new[] { underlyingType };

        Type constructed = typeof(Nullable<>).MakeGenericType(typeArgs);

        object? nullableObjectValue = Activator.CreateInstance(constructed, new object[] { value });

        return nullableObjectValue;
    }

    /// <remarks>
    /// <see cref="PropertyInfo"/>.SetValue is annotated with <see cref="DebuggerHiddenAttribute"/> preventing
    /// the <see cref="Debugger"/> from breaking when it throws.  This wrapper catches exceptions from within
    /// 'user code' so that the debugger can break when they occur.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetPropertyValue(this PropertyInfo? propertyInfo, object obj, object? value) =>
        SetPropertyValue(propertyInfo, obj, value, index: null);

    /// <remarks>
    /// <see cref="PropertyInfo"/>.SetValue is annotated with <see cref="DebuggerHiddenAttribute"/> preventing
    /// the <see cref="Debugger"/> from breaking when it throws.  This wrapper catches exceptions from within
    /// 'user code' so that the debugger can break when they occur.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetPropertyValue(this PropertyInfo? propertyInfo, object obj, object? value, object[]? index)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        try
        {
            propertyInfo.SetValue(obj, value, index);
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw;
        }
    }

    internal static void CacheEnum(Type enumType, Action<FieldInfo, ParameterAttribute, Enum> onCacheEnum)
    {
        foreach (Enum enumValue in Enum.GetValues(enumType))
        {
            ParameterAttribute? attribute = enumValue.GetAttribute<ParameterAttribute>();
            if (attribute == null)
            {
                throw new ArgumentNullException($"{enumType.Name}.{enumValue} must have a {nameof(ParameterAttribute)}");
            }

            var enumField = enumType.GetField(enumValue.ToString());

            if (enumField == null)
            {
                throw new ArgumentNullException($"{enumType.Name}.{enumValue} must exist");
            }

            onCacheEnum(enumField, attribute, enumValue);
        }
    }

    internal sealed record EnumValueKey(Type Type, string Name);

    internal static ParameterAttribute? GetParameterAttribute(OracleTool oracleTool, string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            throw new ArgumentNullException(nameof(propertyName));
        }
        if (string.Equals(propertyName, nameof(ParametersBase.ParametersFile)))
        {
            return new ParameterAttribute(ParametersBase.PARFILE);
        }
        switch (oracleTool)
        {
            case OracleTool.DataPumpImport:
                return _importParameterCache[propertyName]?.ParameterAttribute;
            case OracleTool.DataPumpExport:
                return _exportParameterCache[propertyName]?.ParameterAttribute;
            case OracleTool.SqlLoader:
                return _loaderParameterCache[propertyName]?.ParameterAttribute;
            default:
                throw new ArgumentException($"Unexpected {nameof(OracleTool)}: {oracleTool}");
        }
    }

    internal static void ParseObjectTypeOrThrow(string parameterValueRaw, out OracleObjectType? oracleObjectType)
    {
        oracleObjectType = null;

        if (parameterValueRaw != null)
        {
            var valueKey = new EnumValueKey(TypeOfOracleObjectType, parameterValueRaw);

            if (_enumValueCache.TryGetValue(valueKey, out var objectTypeEnum) &&
                objectTypeEnum != null)
            {
                oracleObjectType = (OracleObjectType) objectTypeEnum;
            }
            else
            {
                throw new ArgumentException($"Invalid {nameof(OracleObjectType)} {parameterValueRaw}", nameof(parameterValueRaw));
            }
        }
    }
}