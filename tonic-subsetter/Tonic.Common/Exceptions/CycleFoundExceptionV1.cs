using System;
using System.Collections.Generic;
using System.Linq;
using Tonic.Common.Helpers;
using Tonic.Common.Models;

namespace Tonic.Common.Exceptions;

public class CycleFoundExceptionV1<T> : Exception where T : class
{
    public List<Edge<T>>? Cycle { get; }

    public CycleFoundExceptionV1(List<Edge<T>> cycle)
    {
        var nodes = cycle.SelectMany(c => new List<T> {c.Child, c.Parent}).ToHashSet();
        List<Edge<T>>? nextCycle;
        while (true)
        {
            var noIncomers = TopologicalSorter<T>.GetAllNodesWithNoIncoming(nodes, cycle).ToHashSet();
            nextCycle = cycle.Where(c => !noIncomers.Contains(c.Parent)).ToList();
            if (nextCycle.Count == cycle.Count)
            {
                break;
            }

            cycle = nextCycle;
        }

        this.Cycle = nextCycle;
    }
}