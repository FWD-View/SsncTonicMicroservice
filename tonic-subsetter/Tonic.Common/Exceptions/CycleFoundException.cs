using System;
using System.Collections.Generic;
using System.Linq;
using Tonic.Common.Models;

namespace Tonic.Common.Exceptions;

public class CycleFoundException : Exception
{
    public readonly List<TableRelationship> Cycle;

    public CycleFoundException(HashSet<TableRelationship> cycleRelationships)
    {
        var nodes = cycleRelationships.SelectMany(rel => new List<Table> {rel.ForeignKeyTable, rel.PrimaryKeyTable})
            .ToHashSet();
        HashSet<TableRelationship> minCycle;
        while (true)
        {
            var currentRoots = nodes.Except(cycleRelationships.Select(rel => rel.PrimaryKeyTable)).ToHashSet();
            minCycle = cycleRelationships.Where(rel => !currentRoots.Contains(rel.ForeignKeyTable)).ToHashSet();
            if (minCycle.Count == cycleRelationships.Count)
                break;
            cycleRelationships = minCycle;
        }

        if (!cycleRelationships.Any())
        {
            Cycle = new List<TableRelationship>();
            return;
        }

        var cycle = new List<TableRelationship>();
        var nextRels = new HashSet<TableRelationship>();

        while (cycleRelationships.Any())
        {
            if (!nextRels.Any())
            {
                var firstRel = cycleRelationships.First();
                cycleRelationships.Remove(firstRel);
                cycle.Add(firstRel);
                nextRels = cycleRelationships.Where(rel => Equals(rel.ForeignKeyTable, firstRel.PrimaryKeyTable)).ToHashSet();
            }
            cycleRelationships.RemoveWhere(rel => nextRels.Contains(rel));
            cycle.AddRange(nextRels);
            var nextTables = nextRels.Select(rel => rel.PrimaryKeyTable).ToHashSet();
            nextRels = cycleRelationships.Where(rel => nextTables.Contains(rel.ForeignKeyTable)).ToHashSet();
        }

        Cycle = cycle;
    }
}