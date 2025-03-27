using System;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Subsetter.Actions;

namespace Tonic.Subsetter.Utils;

public class SubsetTraversal
{
    private readonly HashSet<TableRelationship> _tableRelationships;
    private readonly Dictionary<Table, int> _downstreamTopologicalOrdering;
    private readonly Dictionary<Table, int> _upstreamTopologicalOrdering;
    private readonly ISet<Table> _directTargets;
    private readonly ISet<Table> _additionalUpstreamStarts;
    private readonly HashSet<Table> _upstreamProcessedTables;
    private readonly HashSet<Table> _downstreamProcessedTables;
    // When upstreaming we may encounter dependencies that have not been processed yet
    // as a downstream table. We track these tables separately so as to avoid duplicating their
    // preprocessing, then process them again during the normal downstream subsetting
    private readonly HashSet<Table> _preprocessedDownstreamTables;

    public SubsetTraversal(IEnumerable<ForeignKey> foreignKeys, ISet<Table> directTargets,
        ISet<Table> additionalUpstreamStarts)
    {
        _directTargets = directTargets;
        _additionalUpstreamStarts = additionalUpstreamStarts;
        _upstreamProcessedTables = new HashSet<Table>();
        _downstreamProcessedTables = new HashSet<Table>();
        _preprocessedDownstreamTables = new HashSet<Table>();
        _tableRelationships = new HashSet<TableRelationship>();
        foreach (var fk in foreignKeys)
        {
            var fkTable = new Table(fk.ForeignKeyHostCategory, fk.ForeignKeyTable, fk.Grouped);
            var pkTable = new Table(fk.PrimaryKeyHostCategory, fk.PrimaryKeyTable, fk.Grouped);
            _tableRelationships.Add(new TableRelationship(fkTable, pkTable, fk.IsPrincipal, fk.ForeignKeyColumns,
                fk.PrimaryKeyColumns, fk.MultiTableIndexColumnIndices.Any()));
        }

        _downstreamTopologicalOrdering = new TopologicalOrdering(_tableRelationships).Generate();
        _upstreamTopologicalOrdering = TopologicalOrdering.ReverseOrdering(_downstreamTopologicalOrdering);
    }

    public IEnumerable<SubsetAction> GetTraversalOrder()
    {
        var upstreamHeap = new TraversalHeap();
        var downstreamHeap = new TraversalHeap();
        var traversalOrder = new List<SubsetAction>();

        // Direct Tables
        var orderedDirectTargets = _directTargets.OrderBy(dt => _upstreamTopologicalOrdering[dt]);
        foreach (var directTarget in orderedDirectTargets)
        {
            traversalOrder.Add(new SubsetAction(directTarget, SubsetActionType.Direct));
            _preprocessedDownstreamTables.Add(directTarget);
        }

        // Additional Upstream Starts Downstream Traversal
        var downstreamForAdditionalUpstreamStarts =
            TraverseDownstreamForAdditionalUpstreamStarts();
        traversalOrder.AddRange(downstreamForAdditionalUpstreamStarts);

        var allTargets = _directTargets.Concat(_additionalUpstreamStarts).ToHashSet();
        foreach (var target in allTargets)
        {
            var upstreamTablesToAdd = _tableRelationships.Where(relationship =>
                relationship.IsPrincipal &&
                relationship.PrimaryKeyTable.Equals(target) &&
                !upstreamHeap.Contains(relationship.ForeignKeyTable));

            foreach (var relationship in upstreamTablesToAdd)
            {
                upstreamHeap.Push(relationship.ForeignKeyTable,
                    _upstreamTopologicalOrdering[relationship.ForeignKeyTable]);
            }
        }

        // Upstream Traversal
        var traverseUpstream = TraverseUpstream(upstreamHeap);
        traversalOrder.AddRange(traverseUpstream);

        // Downstream Traversal
        DownstreamFurthestUpstreamTables(downstreamHeap, _directTargets.Union(_upstreamProcessedTables));
        traversalOrder.AddRange(TraverseDownstream(downstreamHeap));

        return traversalOrder;
    }

    private IEnumerable<SubsetAction> TraverseDownstreamForAdditionalUpstreamStarts()
    {
        var traversalOrder = new List<SubsetAction>();
        var firstDownstreamPassHeap = new TraversalHeap();

        var firstTablesToAdd = _tableRelationships
            .Where(rel => _directTargets.Contains(rel.ForeignKeyTable))
            .Select(rel => rel.PrimaryKeyTable).ToHashSet();

        foreach (var table in firstTablesToAdd)
        {
            firstDownstreamPassHeap.Push(table, _downstreamTopologicalOrdering[table]);
        }
        
        var mostDownstreamAdditionalStarts = new HashSet<Table>();
        if (_additionalUpstreamStarts.Count > 0)
        {
            var mostDownstreamAdditionalStartLevel =
                _additionalUpstreamStarts.Max(table => _downstreamTopologicalOrdering[table]);
            mostDownstreamAdditionalStarts = _additionalUpstreamStarts.Where(table =>
                _downstreamTopologicalOrdering[table] == mostDownstreamAdditionalStartLevel).ToHashSet();
        }

        while (firstDownstreamPassHeap.Any())
        {
            var currentTable = firstDownstreamPassHeap.Pop();
            traversalOrder.Add(new SubsetAction(currentTable, SubsetActionType.Downstream));
            _preprocessedDownstreamTables.Add(currentTable);

            var downstreamTablesToAdd = _tableRelationships.Where(rel =>
                !mostDownstreamAdditionalStarts.Contains(rel.ForeignKeyTable) &&
                rel.ForeignKeyTable.Equals(currentTable) &&
                !firstDownstreamPassHeap.Contains(rel.PrimaryKeyTable));

            foreach (var relationship in downstreamTablesToAdd)
            {
                firstDownstreamPassHeap.Push(relationship.PrimaryKeyTable,
                    _downstreamTopologicalOrdering[relationship.PrimaryKeyTable]);
            }
        }


        return traversalOrder;
    }

    private IEnumerable<SubsetAction> TraverseMissingDownstreams(
        IEnumerable<TableRelationship> missingDownstreamsRelationships)
    {
        var upstreamPassHeap = new TraversalHeap();
        var missingDownstreamHeap = new TraversalHeap();

        var missingDownstreamTables = missingDownstreamsRelationships.Select(rel => rel.PrimaryKeyTable).Distinct();
        foreach (var table in missingDownstreamTables)
        {
            missingDownstreamHeap.Push(table, _downstreamTopologicalOrdering[table]);
            upstreamPassHeap.Push(table, _upstreamTopologicalOrdering[table]);
        }

        while (missingDownstreamHeap.Any())
        {
            var currentDownstream = missingDownstreamHeap.Pop();
            var childMissingDownstreams = _tableRelationships.Where(rel =>
                    Equals(currentDownstream, rel.ForeignKeyTable) &&
                    !upstreamPassHeap.Contains(rel.PrimaryKeyTable) &&
                    !_downstreamProcessedTables.Contains(rel.PrimaryKeyTable) &&
                    !_preprocessedDownstreamTables.Contains(rel.PrimaryKeyTable) &&
                    !missingDownstreamHeap.Contains(rel.PrimaryKeyTable))
                .Select(rel => rel.PrimaryKeyTable).ToList();
            foreach (var missingDownstream in childMissingDownstreams)
            {
                missingDownstreamHeap.Push(missingDownstream, _downstreamTopologicalOrdering[missingDownstream]);
                upstreamPassHeap.Push(missingDownstream, _upstreamTopologicalOrdering[missingDownstream]);
            }
        }

        var subsetActions = new List<SubsetAction>();
        while (upstreamPassHeap.Any())
        {
            var table = upstreamPassHeap.Pop();
            subsetActions.Add(new SubsetAction(table, SubsetActionType.Upstream));
            _preprocessedDownstreamTables.Add(table);
        }

        return subsetActions;
    }

    private List<SubsetAction> TraverseUpstream(TraversalHeap upstreamHeap)
    {
        var traversalOrder = new List<SubsetAction>();
        while (upstreamHeap.Any())
        {
            var currentTable = upstreamHeap.Pop();
            var multiTableKeyMissingDownstreams = _tableRelationships.Where(rel =>
                rel.ForeignKeyTable.Equals(currentTable) && rel.IsMultiTable &&
                !_downstreamProcessedTables.Contains(rel.PrimaryKeyTable) &&
                !_preprocessedDownstreamTables.Contains(rel.PrimaryKeyTable)).ToList();
            if (multiTableKeyMissingDownstreams.Any())
            {
                var missingDownStreams = TraverseMissingDownstreams(multiTableKeyMissingDownstreams);
                traversalOrder.AddRange(missingDownStreams);
            }

            _upstreamProcessedTables.Add(currentTable);
            traversalOrder.Add(new SubsetAction(currentTable, SubsetActionType.Upstream));

            var furtherUpstreamTablesToAdd =
                _tableRelationships.Where(rel =>
                    rel.IsPrincipal &&
                    rel.PrimaryKeyTable.Equals(currentTable) &&
                    !_upstreamProcessedTables.Contains(rel.ForeignKeyTable) &&
                    !upstreamHeap.Contains(rel.ForeignKeyTable));

            foreach (var relationship in furtherUpstreamTablesToAdd)
            {
                upstreamHeap.Push(relationship.ForeignKeyTable,
                    _upstreamTopologicalOrdering[relationship.ForeignKeyTable]);
            }
        }

        return traversalOrder;
    }

    private IEnumerable<SubsetAction> TraverseDownstream(TraversalHeap downstreamHeap)
    {
        var traversalOrder = new List<SubsetAction>();

        while (downstreamHeap.Any())
        {
            var currentTable = downstreamHeap.Pop();
            _downstreamProcessedTables.Add(currentTable);
            traversalOrder.Add(new SubsetAction(currentTable, SubsetActionType.Downstream));

            var unprocessedUpstreamTables = _tableRelationships.Where(rel =>
                rel.IsPrincipal &&
                rel.PrimaryKeyTable.Equals(currentTable) &&
                !_directTargets.Contains(rel.ForeignKeyTable) &&
                !_upstreamProcessedTables.Contains(rel.ForeignKeyTable) &&
                !_downstreamProcessedTables.Contains(rel.ForeignKeyTable) &&
                !downstreamHeap.Contains(rel.ForeignKeyTable)).ToHashSet();

            if (unprocessedUpstreamTables.Any())
            {
                var unprocessedUpstreamHeap = new TraversalHeap();
                foreach (var relationship in unprocessedUpstreamTables)
                {
                    unprocessedUpstreamHeap.Push(relationship.ForeignKeyTable,
                        _upstreamTopologicalOrdering[relationship.ForeignKeyTable]);
                }

                var processedParentOrder = TraverseUpstream(unprocessedUpstreamHeap);
                traversalOrder.AddRange(processedParentOrder);

                // Add new children of the updated set of root tables to the downstream heap.
                DownstreamFurthestUpstreamTables(downstreamHeap,
                    processedParentOrder.Select(action => action.Table));

                // Add the current table back in for downstream processing if it hasn't already been added
                // This is necessary because more data will need to be pulled after the current table's newly upstreamed
                // parents have completed pulling data, so we will downstream it again later.
                if (!downstreamHeap.Contains(currentTable))
                {
                    downstreamHeap.Push(currentTable, _downstreamTopologicalOrdering[currentTable]);
                }
            }
            else
            {
                var downstreamTablesToAdd = _tableRelationships.Where(rel =>
                    rel.ForeignKeyTable.Equals(currentTable) &&
                    !downstreamHeap.Contains(rel.PrimaryKeyTable));
                foreach (var relationship in downstreamTablesToAdd)
                {
                    downstreamHeap.Push(relationship.PrimaryKeyTable,
                        _downstreamTopologicalOrdering[relationship.PrimaryKeyTable]);
                }
            }
        }

        return traversalOrder;
    }

    /// <summary>
    /// Adds the first level of tables that are children, i.e. primary key tables, of a list of already processed tables.
    /// </summary>
    /// <param name="downstreamHeap">The downstream heap to which the primary key tables are added to</param>
    /// <param name="processedTables">The list of already processed tables</param>
    private void DownstreamFurthestUpstreamTables(TraversalHeap downstreamHeap, IEnumerable<Table> processedTables)
    {
        var childTables = _tableRelationships.Select(rel => rel.PrimaryKeyTable).ToHashSet();
        var processedRootTables = processedTables.Except(childTables).ToHashSet();
        var downstreamTablesToAdd = _tableRelationships.Where(rel =>
            processedRootTables.Contains(rel.ForeignKeyTable) && !downstreamHeap.Contains(rel.PrimaryKeyTable));

        foreach (var relationship in downstreamTablesToAdd)
        {
            downstreamHeap.Push(relationship.PrimaryKeyTable,
                _downstreamTopologicalOrdering[relationship.PrimaryKeyTable]);
        }
    }
}

public class TraversalHeap
{
    private readonly SimplePriorityQueue<Table, int> _tableHeap;
    private readonly HashSet<Table> _tables;

    public TraversalHeap()
    {
        _tableHeap = new SimplePriorityQueue<Table, int>();
        _tables = new HashSet<Table>();
    }

    public bool Any() => _tableHeap.Any();

    public void Push(Table table, int priority)
    {
        if (_tables.Contains(table))
        {
            throw new ArgumentException($"Traversal Heap already contains table {table}");
        }

        _tableHeap.Enqueue(table, priority);
        _tables.Add(table);
    }

    public Table Pop()
    {
        var value = _tableHeap.Dequeue();
        _tables.Remove(value);
        return value;
    }

    public bool Contains(Table t) => _tables.Contains(t);
}