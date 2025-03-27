using System.Collections.Generic;
using System.Collections.Immutable;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;

namespace Tonic.ResetSchema;

public class ResetSchemaConfig
{
    public IList<IHost> SourceHosts { get; init; } = new List<IHost>();
    public IList<IHost> DestinationHosts { get; init; } = new List<IHost>();
    public TableSourceEnum TableSource { get; init; }
    public IList<Table> AllReferencedTables { get; init; } = new List<Table>();
    public bool KeepPrimaryKeys { get; init; }
    public bool MergePartitions { get; init; }
    public string DbaUser { get; init; } = string.Empty;
    public string DbaPassword { get; init; } = string.Empty;
    public bool DebugLogging { get; init; }
    public bool DebugImport { get; init; }
    
    public bool TruncateOnly { get; init; }

    public ImmutableDictionary<string, List<string>> PostResetSchemaScripts { get; init; } =
        ImmutableDictionary<string, List<string>>.Empty;
}