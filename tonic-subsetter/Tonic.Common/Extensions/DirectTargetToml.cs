using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nett;
using Serilog;
using Tonic.Common.Models;
using Tonic.Common.Utils;

namespace Tonic.Common.Extensions;

public static class DirectTargetToml
{
    /// <summary>
    /// Parses the direct targets from the TOML config.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="expandGroup">
    /// When set to true, grouped targets will be expanded by interpolating the digit at the end of the target table's name (useful for operations for reading the source DB, e.g. Subsetter).
    /// When set to false, only a single target will be produced for a group, in particular the one explicitly stated in the config (useful for operations on the destination DB, e.g. Reset Schema).
    /// </param>
    /// <returns></returns>
    public static IList<DirectSubsetTarget> ParseDirectTargets(this TomlTable config, bool expandGroup)
    {
        if (!config.ContainsKey("DirectTarget"))
        {
            Log.Information("Could not find any [[DirectTarget]]; returning empty");
            return ImmutableList<DirectSubsetTarget>.Empty;
        }

        var importIdsDict = new Dictionary<string, HashSet<long>>();
        if (config.ContainsKey("ImportIds"))
        {
            foreach (var importIdsConfig in config.Get<TomlTableArray>("ImportIds").Items)
            {
                foreach (var key in importIdsConfig.Keys)
                {
                    var ids = importIdsConfig.Get<long[]>(key).ToHashSet();
                    var added = importIdsDict.TryAdd(key, ids);
                    if (!added) importIdsDict[key].UnionWith(ids);
                }
            }
        }

        var targets = new List<DirectSubsetTarget>();
        var directTargets = (TomlTableArray)config["DirectTarget"];
        for (var i = 0; i < directTargets.Count; ++i)
        {
            var dt = directTargets[i];
            if (expandGroup && dt.ContainsKey("TableGroupSize"))
            {
                var count = dt.Get<int>("TableGroupSize");
                for (var groupIdx = 0; groupIdx < count; ++groupIdx)
                {
                    targets.Add(ParseDirectTarget(dt, importIdsDict, groupIdx));
                }
            }
            else
            {
                targets.Add(ParseDirectTarget(dt, importIdsDict));
            }
        }

        return targets;
    }


    private static DirectSubsetTarget ParseDirectTarget(TomlTable dtConfig,
        IReadOnlyDictionary<string, HashSet<long>> importIdsDict, int? idx = null)
    {
        var tablePattern = dtConfig.Get<string>("Table");
        var tableName = idx.HasValue && tablePattern.Contains('0')
            ? Utilities.InterpolateTableName(tablePattern, idx.Value)
            : tablePattern;


        HashSet<long>? idKeys = null;
        if (dtConfig.ContainsKey("IdKeyTomlName"))
        {
            var key = dtConfig.Get<string>("IdKeyTomlName");
            idKeys = importIdsDict.ContainsKey(key) ? importIdsDict[key].ToHashSet() : new HashSet<long>();
        }

        var grouped = idx.HasValue;
        var directSubsetTarget = new DirectSubsetTarget()
        {
            Table = new Table(dtConfig.Get<string>("HostCategory"), tableName, grouped),
            Clause = dtConfig.ContainsKey("Clause") ? dtConfig.Get<string>("Clause") : null,
            Percent = dtConfig.ContainsKey("Percent") ? Convert.ToDecimal(dtConfig.Get("Percent").ToString()) : null,
            IdColumn = dtConfig.ContainsKey("IdColumn") ? dtConfig.Get<string>("IdColumn") : null,
            IdKeys = idKeys,
            Grouped = grouped
        };
        directSubsetTarget.VerifyCorrect();
        return directSubsetTarget;
    }
}