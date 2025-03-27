using System.Collections.Generic;
using System.Collections.Immutable;
using Tonic.Common;
using Tonic.Common.Configs;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Subsetter.Actions;

namespace Tonic.Subsetter;

public interface ISubsetConfig
{
    public IList<DirectSubsetTarget> DirectTargets { get; init; }
    public IList<Table> AdditionalUpstreamStarts { get; init; }
    public IList<UpstreamFilter> UpstreamFilters { get; init; }
    public IList<UpstreamGroupLimit> UpstreamGroupLimits { get; init; }
    public IList<HostConfig> SourceHostConfigs { get; init; }
    public IList<HostConfig> DestinationHostConfigs { get; init; }
    public IList<ForeignKey> ForeignKeys { get; init; }
    public Dictionary<string, string> TableSchemaRestrictions { get; init; }

    public int QueryBatchSize { get; set; }
    public int UploadParallelism { get; init; }
    public bool Debug { get; init; }
    public bool NoDownstreamUnions { get; set; }
    public bool DetailedSubsetInformation { get; set; }
    public ImmutableArray<SchemaOverride> SchemaOverrides { get; init; }
    public ImmutableHashSet<string> CompressedColumns { get; init; }
}

public class SubsetConfig : ISubsetConfig
{
    public IList<DirectSubsetTarget> DirectTargets { get; init; } = null!;
    public IList<Table> AdditionalUpstreamStarts { get; init; } = null!;
    public IList<UpstreamFilter> UpstreamFilters { get; init; } = null!;
    public IList<UpstreamGroupLimit> UpstreamGroupLimits { get; init; } = null!;
    public IList<HostConfig> SourceHostConfigs { get; init; } = null!;
    public IList<HostConfig> DestinationHostConfigs { get; init; } = null!;
    public IList<ForeignKey> ForeignKeys { get; init; } = null!;
    public Dictionary<string, string> TableSchemaRestrictions { get; init; } = null!;

    public int QueryBatchSize { get; set; }
    public int UploadParallelism { get; init; }
    public bool Debug { get; init; }
    public bool NoDownstreamUnions { get; set; }
    public bool DetailedSubsetInformation { get; set; }
    public ImmutableArray<SchemaOverride> SchemaOverrides { get; init; }
    public ImmutableHashSet<string> CompressedColumns { get; init; } = ImmutableHashSet<string>.Empty;
}