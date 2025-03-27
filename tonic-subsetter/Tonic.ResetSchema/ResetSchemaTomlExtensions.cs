using System.IO;
using Nett;
using Tonic.Common;
using Tonic.Common.Extensions;

namespace Tonic.ResetSchema;

public static class ResetSchemaTomlExtensions
{
    public static ResetSchemaConfig ParseResetSchemaConfig(this Stream input)
    {
        var tomlConfig = Toml.ReadStream(input);
        return tomlConfig.GenerateResetSchemaConfig();
    }

    public static ResetSchemaConfig GenerateResetSchemaConfig(this TomlTable tomlConfig)
    {
        var resetSchemaOptions = tomlConfig.Get<TomlTable>("ResetSchemaOptions");

        return new ResetSchemaConfig
        {
            SourceHosts = tomlConfig.ParseHosts(Constants.SourceHost),
            DestinationHosts = tomlConfig.ParseHosts(Constants.DestinationHost),
            TableSource = resetSchemaOptions.ParseTableSource(),
            AllReferencedTables = tomlConfig.ParseAllReferencedTables(),
            KeepPrimaryKeys = resetSchemaOptions.ParseKeepPrimaryKeys(),
            MergePartitions = resetSchemaOptions.ContainsKey("MergePartitions") && resetSchemaOptions.Get<bool>("MergePartitions"),
            DbaUser = resetSchemaOptions.ParseDbConfig("User"),
            DbaPassword = resetSchemaOptions.ParseDbConfig("Password"),
            PostResetSchemaScripts = tomlConfig.ParsePostResetSchemaScript(),
            DebugLogging = resetSchemaOptions.ContainsKey("DebugLogging") &&
                           resetSchemaOptions.Get<bool>("DebugLogging"),
            DebugImport = resetSchemaOptions.ContainsKey("DebugImports") &&
                          resetSchemaOptions.Get<bool>("DebugImports"),
            TruncateOnly = resetSchemaOptions.ParseTruncateTablesOnly()
        };
    }

}