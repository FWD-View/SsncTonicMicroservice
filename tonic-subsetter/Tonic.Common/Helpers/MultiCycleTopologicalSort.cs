using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Serilog;
using Tonic.Common.Exceptions;
using Tonic.Common.Models;

namespace Tonic.Common.Helpers;

public static class MultiCycleTopologicalSort
{
    /// <summary>
    /// Returns the tables sorted by their foreign keys. The first table is most upstream (i.e. has most dependencies),
    /// the last is most downstream.
    /// </summary>
    /// <param name="foreignKeys"></param>
    /// <returns>Tables sorted by their foreign keys. The first table is most upstream (i.e. has most dependencies),
    /// the last is most downstream.</returns>
    public static (ImmutableArray<Table>, ImmutableArray<Table>) FromTables(IEnumerable<ForeignKey> foreignKeys)
    {
        var sorter = new TopologicalSorter<Table>();
        var edges = new List<Edge<Table>>();

        foreach (var fk in foreignKeys)
        {
            Log.Information("Processing Foreign key: {Fk}", fk.ToString());
            var fkTable = new Table(fk.ForeignKeyHostCategory, fk.ForeignKeyTable, fk.Grouped);
            var pkTable = new Table(fk.PrimaryKeyHostCategory, fk.PrimaryKeyTable, fk.Grouped);
            sorter.AddNode(fkTable);
            sorter.AddNode(pkTable);
            edges.Add(new Edge<Table>(fkTable, pkTable));
        }

        var breaks = CalculateDependencyBreaks(sorter, edges);
        var breakNames = string.Join("], [", breaks.Select(e => e.ToString()));
        Log.Information("{C} Cycles discovered in dependencies: [[{Cycles}]]", breaks.Count(), breakNames);

        var edgesAfterBreaks =
            edges.Where(e => !breaks.Contains(TableDependencyBreak.FromTables(e.Parent, e.Child))).ToHashSet();
        sorter.AddEdges(edgesAfterBreaks);
        var dependencyBreakPrimaryKeyTables = breaks.Select(b => b.ForeignKeyTable()).ToImmutableArray();
        return (sorter.Sort().ToImmutableArray(), dependencyBreakPrimaryKeyTables);
    }


    public static HashSet<TableDependencyBreak> CalculateDependencyBreaks(TopologicalSorter<Table> sorter,
        List<Edge<Table>> edges)
    {
        var selfCycles = edges.Where(edge => Equals(edge.Parent, edge.Child));
        var dependencyBreaks =
            selfCycles.Select(e => TableDependencyBreak.FromTables(e.Parent, e.Child)).ToHashSet();
        const int maxIterations = 100;
        for (var i = 0; i < maxIterations; ++i)
        {
            try
            {
                var edgesAfterBreaks =
                    edges.Where(e => !dependencyBreaks.Contains(TableDependencyBreak.FromTables(e.Parent, e.Child)))
                        .ToHashSet();
                sorter.SortEdges(edgesAfterBreaks);
                return dependencyBreaks;
            }
            catch (CycleFoundExceptionV1<Table> ex)
            {
                if (ex.Cycle == null) continue;
                dependencyBreaks = AddBreaks(sorter, ex.Cycle, edges, dependencyBreaks);
            }
        }

        var dependencyCycle = sorter.Edges.ToList();
        var cycle = string.Join(", ", dependencyCycle.Select(e => e.ToString()));
        var breaks = string.Join(", ", dependencyBreaks.Select(tdb => tdb.ToString()));
        Log.Fatal("Topological Sort Error: Failed to resolve circular dependencies after {MaxIterations} iterations", maxIterations);
        Log.Fatal("Topological Sort Error: For Cycle: [{Cycle}]", cycle);
        Log.Fatal("Topological Sort Error: Attempted dependency breaks: [{DependencyBreaks}]", breaks);
        throw new CycleFoundExceptionV1<Table>(dependencyCycle);
    }

    private static HashSet<TableDependencyBreak> AddBreaks(TopologicalSorter<Table> sorter,
        IEnumerable<Edge<Table>> cycle, List<Edge<Table>> edges, HashSet<TableDependencyBreak> dependencyBreaks)
    {
        HashSet<TableDependencyBreak>? automaticBreaks = null;
        var minCycleSize = int.MaxValue;
        foreach (var edge in cycle)
        {
            var tableDependencyBreak = TableDependencyBreak.FromTables(edge.Parent, edge.Child);
            var trialBreaks = dependencyBreaks.Append(tableDependencyBreak)
                .ToHashSet();
            try
            {
                var edgesAfterBreaks =
                    edges.Where(e => !trialBreaks.Contains(TableDependencyBreak.FromTables(e.Parent, e.Child)))
                        .ToHashSet();
                sorter.SortEdges(edgesAfterBreaks);
                return trialBreaks;
            }
            catch (CycleFoundExceptionV1<Table> ex)
            {
                if (ex.Cycle == null) continue;
                var trialCycleSize = ex.Cycle.Count;
                if (trialCycleSize >= minCycleSize) continue;
                automaticBreaks = trialBreaks;
                minCycleSize = trialCycleSize;
            }
        }

        if (automaticBreaks == null)
            throw new InvalidOperationException(
                "Couldn't break the cycles in the database because there were too many non-null constraints");
        return automaticBreaks;
    }
}