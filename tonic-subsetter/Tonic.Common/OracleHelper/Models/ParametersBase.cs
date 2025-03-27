using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Tonic.Common.OracleHelper.Models;

/// <summary>
/// Parameters available in the command-line mode of Sql*Loader and BOTH Data Pump Import and Export
/// </summary>
public abstract record ParametersBase
{
    internal const string PARFILE = nameof(PARFILE);

    /// <summary>
    /// Optional comments read from / written to the beginning of the file
    /// </summary>
    public string[]? HeaderComments { get; set; }

    /// <summary>
    /// The <see cref="OracleTool"/> to which the parameters apply
    /// </summary>
    [JsonIgnore]
    public abstract OracleTool OracleTool { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter(PARFILE)]
    public abstract string? ParametersFile { get; set; }

    /// <summary>
    /// Gets the <see cref="ParameterAttribute"/> for the property with the specified name
    /// </summary>
    /// <remarks>Uses the cached lookups that exist in <see cref="ParFileSerializer"/></remarks>
    public ParameterAttribute? GetParameterAttribute(string propertyName) => ParFileSerializer.GetParameterAttribute(OracleTool, propertyName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal abstract string CoerceBool(bool b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal abstract bool CoerceBool(string s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string YesNo(bool b) => b ? "YES" : "NO";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string YorN(bool b) => b ? "Y" : "N";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string TrueFalse(bool b) => b ? "TRUE" : "FALSE";
}