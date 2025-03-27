using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace Tonic.Common.Helpers;

public interface IForeignKeyProcessor
{
    public static IForeignKeyProcessor FromString(string val)
    {
        return val switch
        {
            "UserItemPropertyKeyProcessor" => UserItemPropertyKeyProcessor.Instance,
            "ItemAttrVinKeyProcessor" => ItemAttrVinKeyProcessor.Instance,
            _ => throw new ArgumentException($"unknown ForeignKeyProcessor {val}")
        };
    }

    public static readonly IForeignKeyProcessor Default = DefaultForeignKeyProcessor.Instance;

    IEnumerable<string[]> ExtractKeys(string[] rowOfKeys);
        
    [UsedImplicitly]
    public string ToString();
}

public class DefaultForeignKeyProcessor : IForeignKeyProcessor
{
    public static readonly DefaultForeignKeyProcessor Instance = new();
    private DefaultForeignKeyProcessor() {}
    public IEnumerable<string[]> ExtractKeys(string[] rowOfKeys)
    {
        return ImmutableList.Create<string[]>(rowOfKeys);
    }

    public override string ToString() => "DefaultForeignKeyProcessor";
}

public class UserItemPropertyKeyProcessor : IForeignKeyProcessor
{
    public static readonly UserItemPropertyKeyProcessor Instance = new();
    private UserItemPropertyKeyProcessor() {}

    public IEnumerable<string[]> ExtractKeys(string[] rowOfKeys)
    {
        if (rowOfKeys.Length != 1) throw new ArgumentException("Must have exactly one key");

        return rowOfKeys[0].Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(key => new[] { key.Split("=").Last() });
    }

    public override string ToString() => "UserItemPropertyKeyProcessor";
}

public class ItemAttrVinKeyProcessor : IForeignKeyProcessor
{
    public static readonly ItemAttrVinKeyProcessor Instance = new();
    private ItemAttrVinKeyProcessor() {}

    public IEnumerable<string[]> ExtractKeys(string[] rowOfKeys)
    {
        if (rowOfKeys.Length != 1) throw new ArgumentException("Must have exactly one key");
        return AttrInfoTokenizer.ExtractToken("4154",rowOfKeys[0]).Select(vin => new[] { vin });
    }

    public override string ToString() => "ItemAttrVinKeyProcessor";
}