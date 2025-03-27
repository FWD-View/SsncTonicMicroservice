using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Nett;
using Serilog;
using Tonic.Common;
using Tonic.Common.Extensions;
using Tonic.Common.Helpers;
using Tonic.Subsetter.Actions;

namespace Tonic.Subsetter;

public static class SubsetConfigTomlExtensions
{
    public static ISubsetConfig ParseSubsetConfig(this Stream input)
    {
        var tomlConfig = Toml.ReadStream(input);
        return tomlConfig.GenerateSubsetConfig().AssertConfigIsCorrect();
    }

    public static ISubsetConfig GenerateSubsetConfig(this TomlTable tomlConfig)
    {
        var subsetterOptions = tomlConfig.Get<TomlTable>("SubsetterOptions");

        return new SubsetConfig
        {
            QueryBatchSize = subsetterOptions.Get<int>("QueryBatchSize"),
            DirectTargets = tomlConfig.ParseDirectTargets(true),
            AdditionalUpstreamStarts = tomlConfig.ParseAdditionalUpstreamStarts(),
            UpstreamFilters = tomlConfig.ParseUpstreamFilters(),
            UpstreamGroupLimits = tomlConfig.ParseUpstreamGroupLimits(),
            SourceHostConfigs = tomlConfig.ParseHostConfigs(Constants.SourceHost),
            DestinationHostConfigs = tomlConfig.ParseHostConfigs(Constants.DestinationHost),
            ForeignKeys = tomlConfig.ParseForeignKeys(),
            SchemaOverrides = SchemaOverride.ParseHostSchemaOverrides(tomlConfig),
            UploadParallelism = subsetterOptions.ContainsKey("UploadParallelism")
                ? subsetterOptions.Get<int>("UploadParallelism")
                : 1,
            Debug = subsetterOptions.ContainsKey("Debug") &&
                    subsetterOptions.Get<bool>("Debug"),
            NoDownstreamUnions = subsetterOptions.ContainsKey("NoDownstreamUnions") &&
                                 subsetterOptions.Get<bool>("NoDownstreamUnions"),
            TableSchemaRestrictions = tomlConfig.ParseSchemaRestrictions(),
            CompressedColumns = tomlConfig.ParseCompressedColumns(),
            DetailedSubsetInformation = subsetterOptions.ContainsKey("DetailedSubsetInformation") &&
                                        subsetterOptions.Get<bool>("DetailedSubsetInformation"),
        };
    }

    private static ISubsetConfig AssertConfigIsCorrect(this ISubsetConfig config)
    {
        if (config.QueryBatchSize >= 1000)
            throw new ArgumentException("QueryBatchSize cannot exceed 999, this is an Oracle limitation.");

        var destinationHosts = config.DestinationHostConfigs;
        foreach (var hostConfig in destinationHosts)
        {
            var hosts = destinationHosts.Where(h => h.Schema == hostConfig.Schema).ToList();
            if (hosts.Count > 1)
            {
                Log.Logger.Debug("Found multiple destination hosts for host category {Category}",
                    hostConfig.HostCategory);
            }
        }
        return config;
    }

    private static IList<UpstreamFilter> ParseUpstreamFilters(this TomlTable config)
    {
        if (!config.ContainsKey("UpstreamFilter")) return ImmutableArray<UpstreamFilter>.Empty;

        var upstreamFilters = new List<UpstreamFilter>();
        var filters = (TomlTableArray)config["UpstreamFilter"];
        for (var i = 0; i < filters.Count; ++i)
        {
            var f = filters[i];
            upstreamFilters.Add(new UpstreamFilter(f));
        }

        return upstreamFilters.ToImmutableArray();
    }

    private static IList<UpstreamGroupLimit> ParseUpstreamGroupLimits(this TomlTable config)
    {
        if (!config.ContainsKey("UpstreamGroupLimit")) return ImmutableArray<UpstreamGroupLimit>.Empty;

        var groupLimits = new List<UpstreamGroupLimit>();
        var filters = (TomlTableArray)config["UpstreamGroupLimit"];
        for (var i = 0; i < filters.Count; ++i)
        {
            var f = filters[i];
            groupLimits.Add(new UpstreamGroupLimit(f));
        }

        return groupLimits.ToImmutableArray();
    }
}