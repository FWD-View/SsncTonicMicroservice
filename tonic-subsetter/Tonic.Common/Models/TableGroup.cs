using System;

namespace Tonic.Common.Models;

public class TableGroup
{
    public readonly string HostCategory;
    public readonly string TableNamePattern;
    public readonly int GroupSize;

    public TableGroup(string hostCategory, string tableName, int groupSize)
    {
        HostCategory = hostCategory;
        TableNamePattern = tableName;
        GroupSize = groupSize;
    }

    public override bool Equals(object? obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}