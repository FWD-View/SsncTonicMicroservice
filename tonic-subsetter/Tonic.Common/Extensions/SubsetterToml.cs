using System.Collections.Generic;
using Nett;
using Tonic.Common.Configs;
using Tonic.Common.Models;

namespace Tonic.Common.Extensions;

public static class SubsetterToml
{
    public static List<HostConfig> ParseHostConfigs(this TomlTable tomlConfig, string key)
    {
        var configs = new List<HostConfig>();
        var hosts = (TomlTableArray)tomlConfig[key];
        for (var i = 0; i < hosts.Count; ++i)
        {
            var host = hosts[i];
            configs.Add(HostConfigToml.Parse(host));
        }
    
        return configs;
    }


    public static List<Table> ParseAdditionalUpstreamStarts(this TomlTable config)
    {
        if (!config.ContainsKey("AdditionalUpstreamStart")) return new List<Table>();

        var additionalUpstreamStarts = new List<Table>();
        var directTargets = (TomlTableArray)config["AdditionalUpstreamStart"];
        for (var i = 0; i < directTargets.Count; ++i)
        {
            var aus = directTargets[i];
            var lhc = aus.Get<string>("HostCategory");
            var table = aus.Get<string>("Table");
            additionalUpstreamStarts.Add(new Table(lhc, table));
        }

        return additionalUpstreamStarts;
    }
}