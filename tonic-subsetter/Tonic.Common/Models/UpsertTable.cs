using System.Collections.Generic;
using System.Collections.Immutable;
using Nett;
using Tonic.Common.Exceptions;

namespace Tonic.Common.Models;

public class UpsertTable : Table
{
    public bool PreserveOnError { get; }
    public IList<string> PrimaryOrUniqueKeyColumns { get; }
    public IList<string> CompositeKeyColumns { get; }
    public string TempSubQueryClause { get; }
    public string UpdateWhereClause { get; }
    public string InsertWhereClause { get; }
    public string ConditionAndClause { get; }
    
    public UpsertTable(
        string hostCategory,
        string tableName,
        IList<string> primaryOrUniqueKeyColumns,
        IList<string> compositeKeyColumns,
        string conditionAndClause,
        string tempSubQueryClause,
        string updateWhereClause,
        string insertWhereClause,
        bool preserveOnError = false,
        bool grouped = false) : base(hostCategory, tableName, grouped)
    {
        PrimaryOrUniqueKeyColumns = primaryOrUniqueKeyColumns;
        CompositeKeyColumns = compositeKeyColumns;
        ConditionAndClause = conditionAndClause;
        TempSubQueryClause = tempSubQueryClause;
        UpdateWhereClause = updateWhereClause;
        InsertWhereClause = insertWhereClause;
        PreserveOnError = preserveOnError;
    }
    
    public new static UpsertTable FromConfig(TomlTable toml)
    {
        var primaryOrUniqueKeyColumns = toml.ContainsKey("PrimaryOrUniqueKeyColumns") 
            ? toml.Get<TomlArray>("PrimaryOrUniqueKeyColumns").To<string>().ToImmutableArray()
            : ImmutableArray<string>.Empty;
        var compositeKeyColumns = toml.ContainsKey("CompositeKeyColumns")
            ? toml.Get<TomlArray>("CompositeKeyColumns").To<string>().ToImmutableArray()
            : ImmutableArray<string>.Empty;
        if (primaryOrUniqueKeyColumns.IsEmpty && compositeKeyColumns.IsEmpty)
            throw new InvalidConfigurationException(
                "Error: [[UpsertTable]] must specify either `PrimaryOrUniqueKeyColumns` or `CompositeKeyColumns`");
        var conditionAndClause = toml.ContainsKey("ConditionAndClause") ? toml.Get<string>("ConditionAndClause") : string.Empty;
        var tempSubQueryClause = toml.ContainsKey("TempSubQueryClause") ? toml.Get<string>("TempSubQueryClause") : string.Empty;
        var updateWhereClause = toml.ContainsKey("UpdateWhereClause") ? toml.Get<string>("UpdateWhereClause") : string.Empty;
        var insertWhereClause = toml.ContainsKey("InsertWhereClause") ? toml.Get<string>("InsertWhereClause") : string.Empty;
        return new UpsertTable(toml.Get<string>("HostCategory"),
            toml.Get<string>("Table"),
            primaryOrUniqueKeyColumns,
            compositeKeyColumns,
            conditionAndClause,
            tempSubQueryClause,
            updateWhereClause,
            insertWhereClause,
            toml.ContainsKey("PreserveOnError") && toml.Get<bool>("PreserveOnError"),
            toml.ContainsKey("Grouped") && toml.Get<bool>("Grouped"));
    }
    
    public Table AsTable => new(HostCategory, TableName, Grouped);
}
