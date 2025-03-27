using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Tonic.Common.OracleHelper.Models
{
    /// <summary>
    /// Oracle command-line tools called during generation
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OracleTool
    {
        /// <summary>
        /// Used to import schema dumps into Oracle
        /// </summary>
        [EnumMember(Value = "impdp")]
        DataPumpImport,
        /// <summary>
        /// Used to export schema dumps from Oracle
        /// </summary>
        [EnumMember(Value = "expdp")]
        DataPumpExport,
        /// <summary>
        /// Used to load data exports into Oracle
        /// </summary>
        [EnumMember(Value = "sqlldr")]
        SqlLoader,

            [EnumMember(Value = "db2cmd")]
        db2cmd
    }
}