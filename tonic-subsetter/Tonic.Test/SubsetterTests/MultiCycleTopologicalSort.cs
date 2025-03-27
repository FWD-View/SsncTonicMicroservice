using System.Collections.Generic;
using System.Linq;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Xunit;

namespace Tonic.Test;

public class TestMultiCycleTopologicalSort
{
    [Fact]
    public void TestSimpleCycle()
    {
        var nodes = SimpleNodes;
        var edges = SimpleEdges(nodes).ToList();
        var sorter = new TopologicalSorter<Table> {Nodes = nodes.Values.ToHashSet()};
        var breaks = MultiCycleTopologicalSort.CalculateDependencyBreaks(sorter, edges).ToList();
        Assert.Single(breaks);
        Assert.Equal(TableDependencyBreak.FromTables(nodes["HostATableA"], nodes["HostATableB"]), breaks[0]);
    }

    [Fact]
    public void TestTwoCycle()
    {
        var nodes = SimpleNodes;
        var edges = SimpleEdges(nodes).ToList();
        edges.Add(new Edge<Table>(nodes["HostBTableA"], nodes["HostATableC"]));
        var sorter = new TopologicalSorter<Table> {Nodes = nodes.Values.ToHashSet()};
        var breaks = MultiCycleTopologicalSort.CalculateDependencyBreaks(sorter, edges).ToList();
        Assert.Single(breaks);
        Assert.Equal(TableDependencyBreak.FromTables(nodes["HostATableC"], nodes["HostATableA"]), breaks[0]);
    }

    [Fact]
    public void TestAutoCycle()
    {
        var nodes = SimpleNodes;
        var edges = SimpleEdges(nodes).ToList();
        edges.Add(new Edge<Table>(nodes["HostATableC"], nodes["HostATableC"]));
        var sorter = new TopologicalSorter<Table> {Nodes = nodes.Values.ToHashSet()};
        var breaks = MultiCycleTopologicalSort.CalculateDependencyBreaks(sorter, edges).ToList();
        var correctBreaks = new HashSet<TableDependencyBreak>
        {
            TableDependencyBreak.FromTables(nodes["HostATableC"], nodes["HostATableC"]),
            TableDependencyBreak.FromTables(nodes["HostATableA"], nodes["HostATableB"]),
        };
        Assert.Equal(correctBreaks.Count, breaks.Count);
        Assert.Equal(breaks.Count, breaks.Intersect(correctBreaks).Count());
    }

    [Fact]
    public void TestManyCycle()
    {
        var nodes = SimpleNodes;
        var edges = SimpleEdges(nodes).Concat(new HashSet<Edge<Table>>
        {
            new(nodes["HostBTableA"], nodes["HostATableC"]),
            new(nodes["HostATableA"], nodes["HostATableC"]),
            new(nodes["HostBTableA"], nodes["HostBTableB"]),
            new(nodes["HostBTableB"], nodes["HostBTableA"]),
        }).ToList();
        var sorter = new TopologicalSorter<Table>{Nodes = nodes.Values.ToHashSet()};
        var breaks = MultiCycleTopologicalSort.CalculateDependencyBreaks(sorter, edges).ToList();
        var correctBreaks = new HashSet<TableDependencyBreak>
        {
            TableDependencyBreak.FromTables(nodes["HostATableC"], nodes["HostATableA"]),
            TableDependencyBreak.FromTables(nodes["HostBTableA"], nodes["HostBTableB"]),
        };
        Assert.Equal(correctBreaks.Count, breaks.Count);
        Assert.Equal(breaks.Count, breaks.Intersect(correctBreaks).Count());
    }

    [Fact]
    public void TestSortFromForeignKeys()
    {
        var nodes = SimpleNodes;
        var edges = SimpleEdges(nodes).ToList();
        var foreignKeys = edges.Select(e => new ForeignKey
        {
            PrimaryKeyHostCategory = e.Child.HostCategory,
            PrimaryKeyTable = e.Child.TableName,
            ForeignKeyHostCategory = e.Parent.HostCategory,
            ForeignKeyTable = e.Parent.TableName,
        });
        var (sorted, breaks) = MultiCycleTopologicalSort.FromTables(foreignKeys);
        var actual = sorted.Select(t => $"{t.HostCategory}{t.TableName}").ToArray();
        Assert.Equal(new[] {"HostATableB", "HostATableC", "HostATableA", "HostBTableA"}, actual);
        Assert.Single(breaks);
        Assert.Equal(breaks[0], new Table("HostA", "TableA"));
    }

    private static readonly Dictionary<string, Table> SimpleNodes = new()
    {
        ["HostATableA"] = new Table("HostA", "TableA"),
        ["HostATableB"] = new Table("HostA", "TableB"),
        ["HostATableC"] = new Table("HostA", "TableC"),
        ["HostBTableA"] = new Table("HostB", "TableA"),
        ["HostBTableB"] = new Table("HostB", "TableB"),
    };

    private static HashSet<Edge<Table>> SimpleEdges(IReadOnlyDictionary<string, Table> nodes) =>
        new()
        {
            new Edge<Table>(nodes["HostATableA"], nodes["HostATableB"]),
            new Edge<Table>(nodes["HostATableB"], nodes["HostATableC"]),
            new Edge<Table>(nodes["HostATableC"], nodes["HostATableA"]),
            new Edge<Table>(nodes["HostATableA"], nodes["HostBTableA"]),
        };
}