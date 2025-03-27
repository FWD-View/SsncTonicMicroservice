using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Subsetter.Actions;
using Tonic.Subsetter.Utils;
using Xunit;

namespace Tonic.Test;

public class TopologicalOrderingTests
{
    [Fact]
    public void TestReverseTopologicalSort()
    {
        const string dummyHost = "DUMMY_HOST";
        var topologicalOrdering = new Dictionary<Table, int>();
        var currentLevel = 0;
        for (var i = 0; i < 15; i += 3)
        {
            topologicalOrdering.Add(new Table(dummyHost, $"table_{i}"), currentLevel);
            topologicalOrdering.Add(new Table(dummyHost, $"table_{i + 1}"), currentLevel);
            topologicalOrdering.Add(new Table(dummyHost, $"table_{i + 2}"), currentLevel);
            currentLevel += 1;
        }

        var reverseOrdering = TopologicalOrdering.ReverseOrdering(topologicalOrdering);

        Assert.Equal(0, reverseOrdering[new Table(dummyHost, "table_14")]);

        var newLevel = 0;
        var index = 0;
        foreach (var keyValuePair in topologicalOrdering.OrderByDescending(kvp => kvp.Value))
        {
            Assert.Equal(newLevel, reverseOrdering[keyValuePair.Key]);
            index += 1;
            if (index % 3 == 0)
            {
                newLevel += 1;
            }
        }
    }

    [Fact]
    public void TestMultiTableDownstreamTraversal()
    {
        var direct = new Table("1", "I");
        var additionalUpstreamStart = new Table("1", "U");
        var foreignKeys = new Dictionary<string, ForeignKey>
        {
            ["S => U"] = new()
            {
                ForeignKeyHostCategory = "1", 
                ForeignKeyTable = "S", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "U", 
                MultiTableIndexColumnIndices = new[] { 0 }.ToImmutableArray(),
            },
            ["S => I"] = new()
            {
                ForeignKeyHostCategory = "1",
                ForeignKeyTable = "S", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "I", 
                MultiTableIndexColumnIndices = new[] { 1 }.ToImmutableArray(),
                IsPrincipal = true,
            },
            ["S => IV"] = new()
            {
                ForeignKeyHostCategory = "1", 
                ForeignKeyTable = "S", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "IV", 
                MultiTableIndexColumnIndices = new[] { 2 }.ToImmutableArray(),
                IsPrincipal = true,
            },
            ["I => U"] = new()
            {
                ForeignKeyHostCategory = "1", 
                ForeignKeyTable = "I", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "U", 
                IsPrincipal = true,
            },
            ["IV => B"] = new()
            {
                ForeignKeyHostCategory = "1", 
                ForeignKeyTable = "IV", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "B", 
            },
            ["IV => C"] = new()
            {
                ForeignKeyHostCategory = "1", 
                ForeignKeyTable = "IV", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "C",
            },
            ["B => I"] = new()
            {
                ForeignKeyHostCategory = "1", 
                ForeignKeyTable = "B", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "I"
            },
            ["C => I"] = new()
            {
                ForeignKeyHostCategory = "1", 
                ForeignKeyTable = "C", 
                PrimaryKeyHostCategory = "1", 
                PrimaryKeyTable = "I",
            },
        };
        var subsetTraversal = new SubsetTraversal(foreignKeys.Values, new HashSet<Table>() { direct },
            new HashSet<Table>() { additionalUpstreamStart });
        var traversalOrder = subsetTraversal.GetTraversalOrder().ToList();
        var correctOrder = new List<(string, SubsetActionType)>
        {
            ("1.I", SubsetActionType.Direct),
            ("1.U", SubsetActionType.Downstream),
            ("1.I", SubsetActionType.Upstream),
            ("1.B", SubsetActionType.Upstream),
            ("1.C", SubsetActionType.Upstream),
            ("1.IV", SubsetActionType.Upstream),
            ("1.S", SubsetActionType.Upstream),
            ("1.IV", SubsetActionType.Downstream),
            ("1.B", SubsetActionType.Downstream),
            ("1.C", SubsetActionType.Downstream),
            ("1.I", SubsetActionType.Downstream),
            ("1.U", SubsetActionType.Downstream),
        };
        Assert.Equal(correctOrder.Count, traversalOrder.Count);
        for (var i = 0; i < correctOrder.Count; i++)
        {
            var (tableName, actionType) = correctOrder[i];
            var action = traversalOrder[i];
            Assert.Equal(tableName, $"{action.Table.HostCategory}.{action.Table.TableName}");
            Assert.Equal(actionType, action.ActionType);
        }
    }
}