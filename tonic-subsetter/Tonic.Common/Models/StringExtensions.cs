using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tonic.Common.Models;

public static partial class StringExtensions
{
    public static string RemoveExtraWhitespaces(this string s)
    {
        var sb = new StringBuilder(s.Length);
        using (var reader = new StringReader(s))
        {
            char c;
            var prevChar = '0';
            for (var i = 0; i < s.Length; i++)
            {
                c = (char) reader.Read();

                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
                else if (!char.IsWhiteSpace(prevChar)) sb.Append(c);
                prevChar = c;
            }
        }

        return sb.ToString();
    }

    public static bool TryParseHexValue(this string val, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (!val.StartsWith("0x", StringComparison.Ordinal))
            return false;

        var hexSpan = val.AsSpan(2);
        var length = hexSpan.Length;
        bytes = new byte[(length + 1) / 2];

        for (var i = length - 1; i >= 0; i -= 2)
        {
            var byteStart = Math.Max(0, i - 1);
            var byteSpan = hexSpan[byteStart..(i + 1)];
            if (!byte.TryParse(byteSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i / 2]))
                return false;
        }
        return true;
    }

    public static string EscapePostgresColumnName(this string columnName) => columnName.Replace("\"", "\"\"");

    public static string? ExtractDomainFromEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        if (!MailAddress.TryCreate(email, out var mailAddress))
        {
            return email;
        }

        return mailAddress.Host;
    }

    /// <summary>
    /// Replaces the first occurence of oldValue in the string with newValue
    /// </summary>
    public static string ReplaceFirst(this string s, string oldValue, string newValue)
    {
        int startIndex = s.IndexOf(oldValue, StringComparison.Ordinal);
        if (startIndex == -1)
        {
            return s;
        }

        return s.Remove(startIndex, oldValue.Length).Insert(startIndex, newValue);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsAny(this string s, StringComparison comparisonType, params string[] values)
    {
        if (string.IsNullOrEmpty(s) || values == null || values.Length <= 0)
        {
            return false;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (string.Equals(s, values[i], comparisonType))
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsAny(this string s, StringComparison comparisonType, params string[] values)
    {
        if (string.IsNullOrEmpty(s) || values == null || values.Length <= 0)
        {
            return false;
        }

        return values.Any(substring => s.Contains(substring, comparisonType));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithAny(this string s, StringComparison comparisonType, params string[] values)
    {
        if (string.IsNullOrEmpty(s) || values == null || values.Length <= 0)
        {
            return false;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (s.StartsWith(values[i], comparisonType))
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CapitalizeFirstLetter(this string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        if (word.Length == 1)
        {
            return word.ToUpperInvariant();
        }

        return char.ToUpperInvariant(word[0]) + word.Substring(1);
    }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimBeforeLastIndexOf(this string s, char c)
    {
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }

        int index = s.LastIndexOf(c);
        if (index > -1)
        {
            return s.Substring(index);
        }

        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimAfterFirstIndexOf(this string s, string s2)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(s2))
        {
            return s;
        }

        int index = s.IndexOf(s2, StringComparison.CurrentCulture);
        if (index > -1)
        {
            return s.Substring(0, index);
        }

        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimAfterLastIndexOf(this string s, string s2)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(s2))
        {
            return s;
        }

        int index = s.LastIndexOf(s2, StringComparison.CurrentCulture);
        if (index > -1)
        {
            return s.Substring(0, index);
        }

        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimBeforeFirstIndexOf(this string s, string s2)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(s2))
        {
            return s;
        }

        int index = s.IndexOf(s2, StringComparison.CurrentCulture);
        if (index > -1)
        {
            return s.Substring(index);
        }

        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimBeforeLastIndexOf(this string s, string s2)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(s2))
        {
            return s;
        }

        int index = s.LastIndexOf(s2, StringComparison.CurrentCulture);
        if (index > -1)
        {
            return s.Substring(index);
        }

        return s;
    }
}