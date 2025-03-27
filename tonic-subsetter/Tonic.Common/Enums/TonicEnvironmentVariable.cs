using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Serilog;
using Tonic.Common.Extensions;

namespace Tonic.Common.Enums
{
    /// <summary>
    /// Catalog of all 'environment variables' available to configure Tonic and THE source of truth
    /// for [non-secretive] default/fallback values if applicable
    /// </summary>
    /// <remarks>
    /// Adding/removing/changing 'default values' here will change them everywhere the variable is used
    ///
    /// Documentation about configurable variables can be generated from this type
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum TonicEnvironmentVariable
    {
        /// <summary>
        /// 'default' should not resolve to any variable name
        /// </summary>
        [Private]
        [Description("")]
        [DefaultValue(true)] //for unit tests
        Unspecified = 0,
        
        //https://www.oracle.com/database/technologies/instant-client/downloads.html (Basic + Tools package)
        [Private]
        [Description("Specifies the path to the Oracle executables (impdp, sqlldr, expdp)")]
        [Category(TonicVariableCategories.Common)]
        ORACLE_HOME,

        [Private]
        [Description("Specifies the path to the DB2 executables (load)")]
        [Category(TonicVariableCategories.Common)]
        DB2_HOME,

        [Description("The size of Standard Data Unit in Oracle.")]
        [Category(TonicVariableCategories.Common)]
        [DefaultValue(0)]
        TONIC_ORACLE_SDU_SIZE,

        /// <summary>The path that the worker uses to store temporary data files.</summary>
        /// <remarks>
        ///     <para>The default value is <code>/tmp/tonic/</code>.</para>
        /// </remarks>
        [Description("The path that the worker uses to store temporary data files.")]
        [Category(TonicVariableCategories.Common)]
        [DefaultValue("/tmp/")]
        TONIC_SHARED_TMP_FILE_PATH,
        
        /// <summary>The character set that is used by the command line tools for Oracle.</summary>
        /// <remarks>
        ///     <para>Only applies to the Oracle data connector.</para>
        ///     <para>The default value is <code>AMERICAN_AMERICA.AL32UTF8</code>.</para>
        /// </remarks>
        [Description("The character set used by the command line tools for Oracle.")]
        [Category(TonicVariableCategories.Common)]
        [DefaultValue("AMERICAN_AMERICA.AL32UTF8")]
        TONIC_ORACLE_CHARSET,
        
        /// <summary>The encryption key that is used to encrypt sensitive connection information in the Tonic application database.</summary>
        [SecretValue]
        [Description("The encryption key used to encrypt sensitive connection information in the Tonic application database.")]
        [Category(TonicVariableCategories.Common)]
        TONIC_SECRET,
        
        [SecretValue]
        [Description("Testing mode for running locally.")]
        [Category(TonicVariableCategories.Common)]
        [DefaultValue(false)]
        TONIC_TEST_MODE
    }

    //this file is allowed to use 'System.Environment.GetEnvironmentVariable*' methods
    [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs")]
    public static class TonicEnvironmentVariableHelper
    {
        /// <summary>
        /// Returns an environment variable value as a <see cref="string"/>
        /// will fallback to a <see cref="DefaultValueAttribute"/> value if applicable
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? Get(this TonicEnvironmentVariable environmentVariable, bool logNonDefaultValue = false)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariable.ToString());

            if (!string.IsNullOrEmpty(value))
            {
                LogNonDefaultValueBeingReturned(environmentVariable, value, logNonDefaultValue);
                return value;
            }

            if (environmentVariable.TryGetDefaultValue(out value))
            {
                LogDefaultValueBeingReturned(environmentVariable, value);
                return value;
            }

            //this will return values containing non-empty whitespace
            //e.g. strings containing ' ', '\t', '\r', '\n' and so on
            return value;
        }

        /// <summary>
        /// Returns an environment variable value as a <typeparamref name="T"/>
        /// will fallback to a <see cref="DefaultValueAttribute"/> value if applicable
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(
            this TonicEnvironmentVariable environmentVariable,
            bool logNonDefaultValue = false,
            [CallerFilePath] string? filePath = null,
            [CallerMemberName] string? memberName = null) where T : IConvertible
        {
            //Get() will return 'default value' if applicable
            var valueRaw = environmentVariable.Get();
            if (valueRaw != null)
            {
                var value = (T) Convert.ChangeType(valueRaw, typeof(T), CultureInfo.InvariantCulture);
                // Log here after we convert as opposed to using get to log
                LogNonDefaultValueBeingReturned(environmentVariable, value, logNonDefaultValue);
                return value;
            }

            if (environmentVariable.IsPrivate())
            {
                throw new ConfigurationErrorsException(
                    $"{Path.GetFileName(filePath)}({memberName}: a private environment variable was not set and had no 'default value'"
                );
            }
            else
            {
                throw new ConfigurationErrorsException($"Environment variable {environmentVariable} was not set and had no 'default value'");
            }
        }

        /// <summary>
        /// Returns true if an environment variable value <see cref="IsSet"/> or has
        /// a <see cref="DefaultValueAttribute"/> value or false if neither apply.
        /// when true, the 'out' parameter contains the value as a <see cref="string"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(
            this TonicEnvironmentVariable environmentVariable,
            [NotNullWhen(true)] out string? value,
            bool logNonDefaultValue = false)
        {
            value = Environment.GetEnvironmentVariable(environmentVariable.ToString());

            if (!string.IsNullOrEmpty(value))
            {
                LogNonDefaultValueBeingReturned(environmentVariable, value, logNonDefaultValue);
                return true;
            }

            if (environmentVariable.TryGetDefaultValue(out value))
            {
                LogDefaultValueBeingReturned(environmentVariable, value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if an environment variable value <see cref="IsSet"/> or has
        /// a <see cref="DefaultValueAttribute"/> value and that value can be parsed as
        /// <typeparamref name="T"/>.  Returns false if no value meets these criteria.
        /// when true, the 'out' parameter contains the value as a <typeparamref name="T"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<T>(
            this TonicEnvironmentVariable environmentVariable,
            [NotNullWhen(true)] out T? value,
            bool logNonDefaultValue = false) where T : IConvertible
        {
            var typeOfT = typeof(T);

            var valueRaw = Environment.GetEnvironmentVariable(environmentVariable.ToString());
            if (valueRaw != null)
            {
                if (typeOfT == typeof(int) && valueRaw.Contains("E+", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(valueRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueConverted))
                    value = (T) Convert.ChangeType(valueConverted.ToString(CultureInfo.InvariantCulture), typeOfT, CultureInfo.InvariantCulture);
                else
                    value = (T) Convert.ChangeType(valueRaw, typeOfT, CultureInfo.InvariantCulture);

                LogNonDefaultValueBeingReturned(environmentVariable, value, logNonDefaultValue);

                return true;
            }

            if (environmentVariable.TryGetDefaultValue(out value))
            {
                LogDefaultValueBeingReturned(environmentVariable, value);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns true if an environment variable value <see cref="IsSet"/> or has
        /// a <see cref="DefaultValueAttribute"/> value and that value can be parsed as
        /// <typeparamref name="T"/>.  Returns false if no value meets these criteria.
        /// when true, the 'out' parameter contains the value as a <typeparamref name="T"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<T>(
            this TonicEnvironmentVariable environmentVariable,
            TryParser<T> tryParse,
            [NotNullWhen(true)] out T? value,
            bool logNonDefaultValue = false) where T : IConvertible
        {
            var valueRaw = Environment.GetEnvironmentVariable(environmentVariable.ToString());
            if (valueRaw != null && tryParse(valueRaw, out value))
            {
                LogNonDefaultValueBeingReturned(environmentVariable, value, logNonDefaultValue);
                return true;
            }

            if (environmentVariable.TryGetDefaultValue(out value))
            {
                LogDefaultValueBeingReturned(environmentVariable, value);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns true if an environment variable value <see cref="IsSet"/> or has
        /// a <see cref="DefaultValueAttribute"/> value and that value can be parsed as
        /// <typeparamref name="T"/>.  Returns false if no value meets these criteria.
        /// when true, the 'out' parameter contains the value as a <typeparamref name="T"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetEnum<T>(
            this TonicEnvironmentVariable environmentVariable,
            bool ignoreCase,
            out T value,
            bool logNonDefaultValue = false) where T : struct, Enum
        {
            var valueRaw = Environment.GetEnvironmentVariable(environmentVariable.ToString());
            if (valueRaw != null)
            {
                var parsed = Enum.TryParse(valueRaw, ignoreCase, out value);
                LogNonDefaultValueBeingReturned(environmentVariable, value, logNonDefaultValue);
                return parsed;
            }

            if (environmentVariable.TryGetDefaultValue(out value))
            {
                LogDefaultValueBeingReturned(environmentVariable, value);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns true if an environment variable value has
        /// a <see cref="DefaultValueAttribute"/> value and that value can be parsed as
        /// <typeparamref name="T"/>.  Returns false if there is no default value.
        /// when true, the 'out' parameter contains the default value as a <typeparamref name="T"/>
        /// </summary>
        public static bool TryGetDefaultValue<T>(this TonicEnvironmentVariable environmentVariable, [NotNullWhen(true)] out T? defaultValue)
        {
            if (environmentVariable.TryGetAttribute<DefaultValueAttribute>(out var defaultValueAttribute))
            {
                var typeOfT = typeof(T);

                //the order of conditions here is important
                if (defaultValueAttribute.Value is T t)
                {
                    defaultValue = t;
                    return true;
                }
                if (typeOfT == typeof(string) && defaultValueAttribute.Value is IConvertible convertible)
                {
                    //this is a special case because any IConvertible 'default value'
                    //is guaranteed to convert to typeof(string)
                    defaultValue = (T) Convert.ChangeType(convertible, typeOfT, CultureInfo.InvariantCulture);
                    return true;
                }
                if (typeOfT is { IsValueType: true, IsEnum: false })
                {
                    defaultValue = default!;
                    return true;
                }
            }

            defaultValue = default;
            return false;
        }

        /// <summary>
        /// Returns the <see cref="DefaultValueAttribute"/> value of an environment variable as a <typeparamref name="T"/> if applicable
        /// </summary>
        public static T? GetDefaultValue<T>(this TonicEnvironmentVariable environmentVariable)
        {
            if (environmentVariable.TryGetDefaultValue(out T? defaultValue))
            {
                return defaultValue;
            }

            return default;
        }

        /// <summary>
        /// Returns true if an environment variable has a value defined / is set, otherwise returns false
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSet(this TonicEnvironmentVariable environmentVariable)
        {
            //bypasses default values normally by 'Get' and checks environment directly
            var valueRaw = Environment.GetEnvironmentVariable(environmentVariable.ToString());
            return valueRaw != null;
        }

        /// <summary>
        /// Returns the value of an environment variable as a <see cref="string"/>.
        /// Bypasses any <see cref="DefaultValueAttribute"/> value if applicable
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? GetRawValue(this TonicEnvironmentVariable environmentVariable)
        {
            //bypasses default values normally by 'Get' and checks environment directly
            var valueRaw = Environment.GetEnvironmentVariable(environmentVariable.ToString());

            //Handle numeric values which are >6 digits in length and not entered in quotes: https://helm.sh/docs/chart_best_practices/values/#make-types-clear
            if (!string.IsNullOrEmpty(valueRaw) && valueRaw.Contains("E+", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(valueRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueAsInt))
                return valueAsInt.ToString(CultureInfo.InvariantCulture);
            return valueRaw;
        }

        /// <summary>
        /// Returns true if an environment variable is annotated with <see cref="PrivateAttribute"/> otherwise returns false
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrivate(this TonicEnvironmentVariable environmentVariable) => environmentVariable.GetAttribute<PrivateAttribute>() != null;

        /// <summary>
        /// Returns true if an environment variable is annotated with <see cref="SecretValueAttribute"/> otherwise returns false
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSecret(this TonicEnvironmentVariable environmentVariable) => environmentVariable.GetAttribute<SecretValueAttribute>() != null;

        /// <summary>
        /// <inheritdoc cref="ConfigurationBinder.GetValue{T}(IConfiguration,string)"/>
        /// </summary>
        /// <remarks>
        /// Overload to simplify usage of <see cref="IConfiguration"/> with well-known variable strings.
        /// This preserves the 'override' value in config if a config file overrides an environment
        /// variable when both are set
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? GetValue<T>(this IConfiguration configuration, TonicEnvironmentVariable environmentVariable) =>
            configuration.GetValue(environmentVariable.ToString(), environmentVariable.GetDefaultValue<T>());

        /// <summary>
        /// <inheritdoc cref="ConfigurationBinder.GetValue{T}(IConfiguration,string)"/>
        /// </summary>
        /// <remarks>
        /// Overload to simplify usage of <see cref="IConfiguration"/> with well-known variable strings.
        /// This preserves the 'override' value in config if a config file overrides an environment
        /// variable when both are set
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValue<T>(this IConfiguration configuration, TonicEnvironmentVariable environmentVariable, T defaultValue) =>
            configuration.GetValue(environmentVariable.ToString(), defaultValue) ?? defaultValue;

        private static void LogDefaultValueBeingReturned(TonicEnvironmentVariable environmentVariable, object value)
        {
            if (ShouldLog(environmentVariable))
            {
                Log.Information("Environment variable {Variable} is not set. Defaulting to {DefaultValue}", environmentVariable, value);
            }
        }

        private static void LogNonDefaultValueBeingReturned(TonicEnvironmentVariable environmentVariable, object value, bool logNonDefaultValue)
        {
            if (ShouldLog(environmentVariable) && logNonDefaultValue)
                Log.Information("Using {EnvironmentVariable} = {Value}",
                    environmentVariable.ToString(),
                    value);
        }

        private static bool ShouldLog(TonicEnvironmentVariable environmentVariable)
        {
            if (environmentVariable.IsPrivate())
            {
                return false;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable($"{environmentVariable}_LOGGED", EnvironmentVariableTarget.Process)))
            {
                return false;
            }

            Environment.SetEnvironmentVariable($"{environmentVariable}_LOGGED", true.ToString(CultureInfo.InvariantCulture), EnvironmentVariableTarget.Process);

            return true;
        }
    }

    /// <summary>
    /// Delegate in the shape of the various 'TryParse' methods present on <see cref="IConvertible"/> types.
    /// Used to provide custom parsing or parsing of non-convertible types.
    /// </summary>
    public delegate bool TryParser<T>(string rawValue, out T value);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class TonicVariableCategories
    {
        public const string Common = nameof(Common);
        public const string Remapper = nameof(Remapper);
    }
}