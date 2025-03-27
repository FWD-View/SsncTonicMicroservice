using Nett;

namespace Tonic.Common.Models;

public record RowProcessor
{
    public string TableName { get; init; } = string.Empty;
    public string TargetColumn { get; init; } = string.Empty;
    public string ValueColumn { get; init; } = string.Empty;
    public string NewPrefix { get; init; } = string.Empty;

    public static (Table, RowProcessor) FromToml(TomlTable toml)
    {
        var lhc = toml.Get<string>("HostCategory");
        var tableName = toml.Get<string>("Table");
        var valueColumnName = toml.Get<string>("ValueColumn");
        var targetColumnName = toml.Get<string>("TargetColumn");
        var newPrefix = toml.Get<string>("NewPrefix");
        return (new Table(lhc, tableName), new RowProcessor
        {
            TableName = tableName,
            ValueColumn = valueColumnName,
            TargetColumn = targetColumnName,
            NewPrefix = newPrefix
        });
    }
}