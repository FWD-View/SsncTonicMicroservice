using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using Tonic.Common.Configs;
using Tonic.Common.Helpers;
using Tonic.Common.Models;

namespace Tonic.Common.OracleHelper;

public static class OracleHelperUtils
{
    private const char Quote = '\"';
    private const char Backslash = '\\';
    private const char Comma = ',';
    public const long TwoGigabytes = 2_147_483_684;
    public const int DefaultColumnArrayRows = 5_000;
    public const int CsvBatchSize = 10_000;
    public const int WhereClauseBatchSize = 1_000;
    public const string SharedDirectory = "/tmp";
    public const string ResumableWaitErrorCode = "ORA-39171:";
    public static readonly Regex OracleErrorRegex = new(@".*(ORA-\d{5}:).*", RegexOptions.Compiled);
    public static readonly Regex SqlLoaderErrorRegex = new(@".*(SQL\*Loader-\d*:).*", RegexOptions.Compiled);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ControlFilePath(string fileName) => $"{fileName}.ctl".Replace('\\', '/');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ControlFilePath(string path, string fileName) =>
        Path.Combine(path, $"{fileName}.ctl").Replace('\\', '/');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string LogFilePath(string fileName) =>$"{fileName}-sqlldr.log".Replace('\\', '/');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string LogFilePath(string path, string fileName) =>
        Path.Combine(path, $"{fileName}-sqlldr.log").Replace('\\', '/');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BadFilePath(string fileName) => $"{fileName}.bad".Replace('\\', '/');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BadFilePath(string path, string fileName) =>
        Path.Combine(path, $"{fileName}.bad").Replace('\\', '/');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SingleQuote(string s) => $"'{s}'";

    public static string GetConnectionString(HostConfig oracleDbc)
    {
        var username = oracleDbc.User.Replace("\"", "\\\"");
        var password = EscapePassword(oracleDbc.Password);
        var database = oracleDbc.Sid.Replace("\"", "\\\"");

        return $"\"{username}/{password}@//{oracleDbc.Host}:{oracleDbc.Port}/{database}\"";
    }

    /// <summary>
    /// Escapes an argument for passing to the Oracle CLI tools, following the C# implementation in `System.PasteArguments` with
    ///     some modifications
    ///
    /// Also: https://docs.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
    /// </summary>
    /// <param name="password">The password to escape</param>
    /// <returns></returns>
    private static string EscapePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return string.Empty;

        // If the string has a white space, then we use double quotes, as Oracle seems to parse out a single quote (")
        // if the string doesn't we wrap in single quotes
        var doubleQuote = password.Any(char.IsWhiteSpace);

        var sb = new StringBuilder();

        // Begin wrapping quotes
        // If we are double quoting, we don't need to escape them, but we need to escape single quotes
        if (doubleQuote)
        {
            sb.Append(OracleHelperUtils.Quote, 2);
        }
        else
        {
            sb.Append(OracleHelperUtils.Backslash);
            sb.Append(OracleHelperUtils.Quote);
        }

        var idx = 0;
        while (idx < password.Length)
        {
            var c = password[idx++];
            if (c == OracleHelperUtils.Backslash)
            {
                var numBackslash = 1;

                while (idx < password.Length && password[idx] == OracleHelperUtils.Backslash)
                {
                    idx++;
                    numBackslash++;
                }

                if (idx == password.Length)
                {
                    // There are backslashes up to the end of the argument, and we are wrapping it in quotes, so
                    // there will be a quote after the backslashes, we must double the number of backslashes
                    sb.Append(OracleHelperUtils.Backslash, numBackslash * 2);
                }
                else if (password[idx] == OracleHelperUtils.Quote || password[idx] == OracleHelperUtils.Comma)
                {
                    // The character after the backslash is a quote or a comma, we must double the number of backslashes,
                    //  and escape the quote/comma
                    sb.Append(OracleHelperUtils.Backslash, numBackslash * 2 + 1);
                    sb.Append(password[idx]);
                    idx++;
                }
                else
                {
                    //The backslashes are not followed by a quote, no need to escape them, leave as is
                    sb.Append(OracleHelperUtils.Backslash, numBackslash);
                }

                continue;
            }

            if (c == OracleHelperUtils.Quote)
            {
                // Escape the quote, so it appears as a literal
                sb.Append(OracleHelperUtils.Backslash);
                sb.Append(OracleHelperUtils.Quote);
                continue;
            }

            // Non-quote or backslash character, append it
            sb.Append(c);
        }

        // End wrapping quotes
        // If we are double quoting, we don't need to escape them, but we need to escape single quotes
        if (doubleQuote)
        {
            sb.Append(OracleHelperUtils.Quote, 2);
        }
        else
        {
            sb.Append(OracleHelperUtils.Backslash);
            sb.Append(OracleHelperUtils.Quote);
        }

        return sb.ToString();
    }

    public static bool TryDeserialize<T>(string json, out T value)
    {
        try
        {
            value = TonicJsonSerializer.Deserialize<T>(json) ??
                    throw new InvalidOperationException("Failed to deserialize oracle helper response");
            return true;
        }
        catch (Exception e)
        {
            Log.Debug(e, "Error deserializing");
        }

        value = default!;
        return false;
    }

    public static string GetColumnConfig(Column column)
    {
        var columnName = column.ColumnName;
        var columnType = column.DataType;
        if (columnType == "DATE")
        {
            return $"    \"{columnName}\" DATE 'YYYY-MM-DD HH24:mi:ss' NULLIF \"{columnName}\"=\"\\\\N\"";
        }

        if (columnType.StartsWith("TIMESTAMP"))
        {
            return $"    \"{columnName}\" TIMESTAMP 'YYYY-MM-DD HH24:mi:ss.ff6' NULLIF \"{columnName}\"=\"\\\\N\"";
        }

        var specifySize = columnType switch
        {
            "CLOB" => "char(1000000)",
            "NCLOB" => "char(1000000)",
            "BLOB" => "char(1000000)",
            "LONG" => "char(1000000)",
            "VARCHAR2" => "char(4000)",
            "NVARCHAR2" => "char(4000)",
            "CHAR" => "char(2000)",
            "NCHAR" => "char(2000)",
            _ => ""
        };
        var blobEnclosure = OracleUtilities.BlobTypes.Contains(columnType)
            ? " ENCLOSED BY '<blob-438f58ee-f378-41d4-901d-6d7846784e38>'"
            : "";
        return @$"    ""{columnName}"" {specifySize}{blobEnclosure} NULLIF ""{columnName}""=""\\N""";
    }
}