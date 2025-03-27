using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nett;
using Serilog;
using Tonic.Common.Exceptions;
using static System.String;

namespace Tonic.Common.Extensions;

public static class TableCountToml
{
    public static ImmutableHashSet<string> ParseIncludedWorkspaces(this TomlTable tomlConfig,
        Dictionary<string, string> workspaceNameMappings)
    {
        var countOptionsToml = GetCountOptions(tomlConfig);
        if (countOptionsToml == null) return ImmutableHashSet<string>.Empty;
        if (!workspaceNameMappings.Any())
        {
            Log.Error(
                "Error: [TableCount.CountOptions] defined but no [HostCategoryToWorkspace] mappings specified");
            return ImmutableHashSet<string>.Empty;
        }

        var inclusions = countOptionsToml
            .Get<string>("IncludedWorkspaces")
            .Split('\n')
            .Select(s => s.TrimEnd())
            .ToArray();
        var invertedTableMappings = CreateInvertedTableMappings(workspaceNameMappings);

        var missingInclusions = inclusions
            .SelectMany(r => r.Split(",").Skip(2))
            .Where(n => !invertedTableMappings.ContainsKey(n))
            .ToArray();
        if (missingInclusions.Any())
        {
            throw new InvalidConfigurationException(
                $"[HostCategoryToWorkspace] missing included workspaces: [{Join(", ", missingInclusions)}]");
        }

        return inclusions
            .SelectMany(r => r.Split(",").Skip(2))
            .Where(n => invertedTableMappings.ContainsKey(n))
            .Select(n => invertedTableMappings[n])
            .ToImmutableHashSet();
    }

    private static Dictionary<string, string> CreateInvertedTableMappings(
        Dictionary<string, string> workspaceNameMappings)
    {
        var invertedTableMappings = new Dictionary<string, string>();
        foreach (var (key, value) in workspaceNameMappings)
        {
            var openParenIndex = value.LastIndexOf('(');
            string mapping;
            if (openParenIndex == -1)
            {
                mapping = value.TrimEnd();
            }
            else
            {
                var closingParenIndex = value.LastIndexOf(')');
                if (closingParenIndex == -1)
                    throw new InvalidConfigurationException(
                        $"Malformed <key>:<value> encountered in [HostToWorkspace] - ({key}:{value})");
                mapping = value.Remove(openParenIndex, closingParenIndex - openParenIndex + 1).TrimEnd();
                mapping = Join(' ', mapping.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

            invertedTableMappings[mapping] = key.TrimEnd();
        }

        return invertedTableMappings;
    }

    private static TomlTable? GetCountOptions(TomlTable tomlConfig)
    {
        if (!tomlConfig.ContainsKey("TableCount"))
        {
            Log.Information("Could not locate `TableCount` Config options, skipping included workspace parsing");
            return null;
        }

        var tableCountToml = tomlConfig.Get<TomlTable>("TableCount");
        if (tableCountToml == null)
        {
            Log.Information("Could not locate [TableCount] Config options, skipping included workspace parsing");
            return null;
        }

        if (!tableCountToml.ContainsKey("CountOptions"))
        {
            Log.Information(
                "Could not find `[TableCount.CountOptions]` config key, skipping included workspace parsing");
            return null;
        }

        return tableCountToml.Get<TomlTable>("CountOptions");
    }
}