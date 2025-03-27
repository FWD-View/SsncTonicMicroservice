using System.Collections.Generic;
using System.Linq;
using Tonic.Common.Exceptions;
using Tonic.Common.Models;

namespace Tonic.Common.Helpers;

public class TopologicalOrdering
{
    private readonly ISet<TableRelationship> _relationships;
    private readonly ISet<Table> _nodes;

    public TopologicalOrdering(ISet<TableRelationship> relationships)
    {
        _relationships = relationships;
        _nodes = relationships.Select(rel => rel.ForeignKeyTable)
            .Union(relationships.Select(rel => rel.PrimaryKeyTable))
            .ToHashSet();
    }

    public Dictionary<Table, int> Generate()
    {
        var topoOrdering = new Dictionary<Table, int>();
        var remainingNodes = new HashSet<Table>(_nodes);
        var remainingRelationships = new HashSet<TableRelationship>(_relationships);
        var currentRoots = remainingNodes.Except(remainingRelationships.Select(rel => rel.PrimaryKeyTable))
            .ToHashSet();

        var level = 0;

        while (remainingNodes.Any())
        {
            foreach (var node in currentRoots)
            {
                topoOrdering[node] = level;
            }

            level += 1;

            remainingNodes.ExceptWith(currentRoots);
            remainingRelationships.RemoveWhere(relationship => currentRoots.Contains(relationship.ForeignKeyTable));
            currentRoots = remainingNodes.Except(remainingRelationships.Select(rel => rel.PrimaryKeyTable))
                .Except(topoOrdering.Keys).ToHashSet();

            if (remainingNodes.Any() && !currentRoots.Any())
            {
                throw new CycleFoundException(remainingRelationships);
            }
        }

        return topoOrdering;
    }

    public static Dictionary<Table, int> ReverseOrdering(Dictionary<Table, int> topoOrdering)
    {
        var reverseOrdering = new Dictionary<Table, int>();
        var currentLevel = 0;
        foreach (var levelGroup in topoOrdering.GroupBy(kvp => kvp.Value).OrderByDescending(group => group.Key))
        {
            foreach (var kvp in levelGroup)
            {
                reverseOrdering.Add(kvp.Key, currentLevel);
            }

            currentLevel += 1;
        }

        return reverseOrdering;
    }
}