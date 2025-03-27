using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Tonic.Common.Models;

/// <summary>
/// Class for capturing compiler-injected 'source code' information from the callsite
/// </summary>
public sealed record SourceInfo
{
    // ReSharper disable once InconsistentNaming
    private static readonly Type ThisType = typeof(SourceInfo);

    /// <summary>
    /// Namespace with leading parts removed from the front to shorten log entries that emit full paths
    /// </summary>
    internal static readonly string ShortenedNamespacePrefix = ThisType.Namespace!.Substring(
        ThisType.Namespace.IndexOf(".", StringComparison.InvariantCultureIgnoreCase) + 1);

    /// <summary>
    /// The compiler-injected source code file path of the callsite
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The compiler-injected source code file line number of the callsite
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// The compiler-injected source code member name of the callsite
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// ctor for serialization
    /// </summary>
    [JsonConstructor]
    private SourceInfo()
        : this(string.Empty, -1, string.Empty)
    {
    }

    private SourceInfo(string filePath = "", int lineNumber = 0, string memberName = "")
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        MemberName = memberName;
    }


    /// <summary>
    /// Factory to create <see cref="SourceInfo"/> from the callsite
    /// </summary>
    public static SourceInfo FromHere([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => new(filePath, lineNumber, memberName);

    /// <summary>
    /// Factory to create <see cref="SourceInfo"/> from the values provided by Caller*Attribute-annotated parameters
    /// </summary>
    public static SourceInfo FromCallerAttributes(string filePath, int lineNumber, string memberName)
        => new(filePath, lineNumber, memberName);

    public override string ToString() =>
        $"{this.GetFileShortLocation()}({LineNumber}): {MemberName}";
}

public static class SourceInfoExtensions
{
    /// <summary>
    /// Gets a the path to the file with prefixing directory names removed
    /// </summary>
    public static string GetFileShortLocation(this SourceInfo sourceInfo)
    {
        var filePath = sourceInfo.FilePath;
        if (string.IsNullOrEmpty(filePath))
        {
            return string.Empty;
        }

        string location;
        string fileName = Path.GetFileName(filePath);

        DirectoryInfo directoryInfo = sourceInfo.ContainingDirectory();

        if (!string.IsNullOrEmpty(directoryInfo.Name))
        {
            DirectoryInfo? cursor = directoryInfo;
            while (cursor != null)
            {
                if (cursor.Name.StartsWith(SourceInfo.ShortenedNamespacePrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    location = $"{cursor.Name.Substring(SourceInfo.ShortenedNamespacePrefix.Length)}@{fileName}";
                    return location;
                }

                cursor = cursor.Parent;
            }

            location = $"{directoryInfo.Name}@{fileName}";
        }
        else if (Path.DirectorySeparatorChar is var directorySeparatorChar &&
                 filePath.Contains(directorySeparatorChar))
        {
            location = filePath.TrimBeforeLastIndexOf(directorySeparatorChar);
        }
        else
        {
            location = filePath;
        }

        return location;
    }

    /// <remarks>can fail and return `null` in AWS on Linux</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DirectoryInfo ContainingDirectory(this SourceInfo sourceInfo, string pathWhenNull = "") =>
        new(
            Path.GetDirectoryName(sourceInfo.FilePath) /* can fail and return `null` in AWS on Linux */ ??
            pathWhenNull);

    public static bool TryFindPeerOrAncestor(this SourceInfo sourceInfo, string filePattern, out FileInfo? fileInfo)
    {
        var directoryInfo = sourceInfo.ContainingDirectory();
        while (directoryInfo != null)
        {
            fileInfo = directoryInfo.EnumerateFiles(filePattern)
                .FirstOrDefault();
            if (fileInfo != null)
            {
                return true;
            }

            directoryInfo = directoryInfo.Parent;
        }

        fileInfo = null;
        return false;
    }

}