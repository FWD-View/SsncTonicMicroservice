using System.Collections.Generic;
using System.Linq;
using Tonic.Common.Exceptions;
using Tonic.Common.Models;

namespace Tonic.Common.Helpers;

public class TopologicalSorter<T> where T : class
{
    internal HashSet<Edge<T>> Edges = new HashSet<Edge<T>>();
    public HashSet<T> Nodes = new HashSet<T>();

    public void AddEdges(HashSet<Edge<T>> newEdges)
    {
        Edges = newEdges;
    }
        
    public void AddNode(T n)
    {
        Nodes.Add(n);
    }

    public List<T> Sort()
    {
        // Remove any self-cycles, as we'll deal with those separately when processing the table
        var tempEdges = this.Edges.ToList();
        var tempNodes = new List<T>(GetAllNodesWithNoIncoming(this.Nodes, tempEdges));
        var sortedList = new List<T>();

        while (tempNodes.Any())
        {
            var n = tempNodes[0];
            sortedList.Add(n);
            tempNodes.RemoveAt(0);

            RemoveAllEdgesIncomingFrom(n, tempEdges);
            tempNodes = tempNodes
                .Concat(GetAllNodesWithNoIncoming(this.Nodes, tempEdges).Except(tempNodes).Except(sortedList))
                .ToList();
        }

        if (tempEdges.Any())
        {
            throw new CycleFoundExceptionV1<T>(tempEdges);
        }

        return sortedList;
    }

    private static void RemoveAllEdgesIncomingFrom(T parent, List<Edge<T>> e)
    {
        e.RemoveAll(x => EqualityComparer<T>.Default.Equals(x.Parent, parent));
    }

    internal static List<T> GetAllNodesWithNoIncoming(HashSet<T> nodes, List<Edge<T>> e)
    {
        var allChildren = e.Select(x => x.Child).Distinct();
        return nodes.Except(allChildren).ToList();
    }

    public void SortEdges(HashSet<Edge<T>> newEdges)
    {
        this.Edges = newEdges;
        Sort();
    }
}