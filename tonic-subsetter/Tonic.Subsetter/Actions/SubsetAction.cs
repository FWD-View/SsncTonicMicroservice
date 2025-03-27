using Tonic.Common.Models;

namespace Tonic.Subsetter.Actions;

public enum SubsetActionType
{
    Direct,
    Upstream,
    Downstream
}
public class SubsetAction
{
    public Table Table { get; }
    public SubsetActionType ActionType { get; }

    public SubsetAction(Table table, SubsetActionType actionType)
    {
        Table = table;
        ActionType = actionType;
    }

    public override string ToString()
    {
        return $"{Table.HostCategory}.{Table.TableName}: {ActionType}";
    }
}