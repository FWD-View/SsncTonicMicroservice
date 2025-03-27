using System;
using Nett;

namespace Tonic.Common.Configs;

public record PartialHostConfig
{
    public string? HostCategory { get; private init; }
    public string? Host { get; private init; }
    public int? Port { get; private init; }
    public string? User { get; init; }
    public string? Password { get; init; }
    public string? Sid { get; private init; }
    public string? Schema { get; init; }
    public int ShardedIndex { get; init; }

    public static PartialHostConfig FromToml(TomlTable tomlConfig) => new()
    {
        HostCategory = tomlConfig.ContainsKey("HostCategory")
            ? tomlConfig.Get<string>("HostCategory")
            : null,
        Host = tomlConfig.ContainsKey("Host") ? tomlConfig.Get<string>("Host") : null,
        Port = tomlConfig.ContainsKey("Port") ? Convert.ToUInt16(tomlConfig["Port"].ToString()) : null,
        User = tomlConfig.ContainsKey("User") ? tomlConfig.Get<string>("User").ToUpperInvariant() : null,
        Password = tomlConfig.ContainsKey("Password") ? tomlConfig.Get<string>("Password") : null,
        Sid = tomlConfig.ContainsKey("Sid") ? tomlConfig.Get<string>("Sid").ToUpperInvariant() : null,
        Schema = tomlConfig.ContainsKey("Schema") ? tomlConfig.Get<string>("Schema").ToUpperInvariant() : null,
        ShardedIndex = tomlConfig.Get<int>("ShardedIndex"),
    };
}