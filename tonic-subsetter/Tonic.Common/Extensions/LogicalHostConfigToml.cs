using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Nett;
using Tonic.Common.Configs;

namespace Tonic.Common.Extensions;

public static class HostConfigToml
{
    public static HostConfig Parse(TomlTable tomlConfig)
    {
        if (tomlConfig.ContainsKey("IndexTablespace") || tomlConfig.ContainsKey("DataTablespace"))
        {
            throw new InvalidOperationException("IndexTablespace and DataTablespace are dead config points.");
        }

        var config = new HostConfig
        {
            HostCategory = tomlConfig.Get<string>("HostCategory"),
            Host = tomlConfig.Get<string>("Host"),
            Port = Convert.ToUInt16(tomlConfig["Port"].ToString()),
            User = tomlConfig.Get<string>("User").ToUpperInvariant(),
            Password = tomlConfig.Get<string>("Password"),
            Sid = tomlConfig.Get<string>("Sid").ToUpperInvariant(),
            Schema = tomlConfig.Get<string>("Schema").ToUpperInvariant(),
            CanDbLink = tomlConfig.ContainsKey("CanDbLink") && tomlConfig.Get<bool>("CanDbLink"),
            ShardedIndex = tomlConfig.ContainsKey("ShardedIndex") ? tomlConfig.Get<int>("ShardedIndex") : 0,
            DbType = tomlConfig.Get<string>("DbType"),


        };

        config.ShardedDestinations = config.ParseShardedDestinationHost(tomlConfig);
        return config;
    }

    private static ImmutableDictionary<int, HostConfig> ParseShardedDestinationHost(this HostConfig config, TomlTable tomlConfig)
    {
        var shardLookup = new Dictionary<int, HostConfig>();
        if (!tomlConfig.ContainsKey("DestinationShard")) return shardLookup.ToImmutableDictionary();
        var shardedDestinationTomlArray = (TomlTableArray)tomlConfig["DestinationShard"];
        for (var i = 0; i < shardedDestinationTomlArray.Count; ++i)
        {
            var shardedDestinationToml = shardedDestinationTomlArray[i];
            var partialHostConfig = PartialHostConfig.FromToml(shardedDestinationToml);
            if (partialHostConfig.ShardedIndex == -1)
                throw new InvalidDataException($"Must specify valid sharded index for sharded host {tomlConfig}");
            if (shardLookup.ContainsKey(partialHostConfig.ShardedIndex))
                throw new InvalidDataException($"Duplicate sharded destination found for host {tomlConfig}");
            shardLookup[partialHostConfig.ShardedIndex] = config.Extend(partialHostConfig);
        }

        return shardLookup.ToImmutableDictionary();
    }

    public static HostConfig Extend(this HostConfig config, PartialHostConfig partial) => new()
    {
        HostCategory = partial.HostCategory ?? config.HostCategory,
        Host = partial.Host ?? config.Host,
        Port = partial.Port ?? config.Port,
        User = partial.User ?? config.User,
        Password = partial.Password ?? config.Password,
        Sid = partial.Sid ?? config.Sid,
        Schema = partial.Schema ?? config.Schema,
        ShardedIndex = partial.ShardedIndex,
        CanDbLink = false,
    };

}