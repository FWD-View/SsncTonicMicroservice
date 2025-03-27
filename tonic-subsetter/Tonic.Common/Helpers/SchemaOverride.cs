using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Nett;
using Tonic.Common.Models;

namespace Tonic.Common.Helpers;

public record SchemaOverride
{
    public string HostCategory { get; }
    public string Sid { get; }
    public string Host { get; }
    public string Schema { get; }
    public string TableName { get; }

    public const string OverrideSchemaToken = "TONIC_OVERRIDE_SCHEMA_TOKEN";
    private const string TokenPattern = @"{{TONIC_OVERRIDE_SCHEMA_TOKEN!([A-Za-z_]*[0-9]*?)}}";

    public static string CreateOverrideToken(string tableName) =>
        $"{{{{TONIC_OVERRIDE_SCHEMA_TOKEN!{tableName}}}}}";


    public static bool ShouldOverrideTable(Table table, ImmutableArray<SchemaOverride> schemaOverrides) =>
        schemaOverrides.Any(o =>
            o.HostCategory == table.HostCategory && o.TableName == table.TableName);

    public static string SchemaTokenForTable(Table table, ImmutableArray<SchemaOverride> schemaOverrides) =>
        ShouldOverrideTable(table, schemaOverrides)
            ? CreateOverrideToken(table.TableName)
            : Constants.TonicSchemaToken;

    public static SchemaOverride?
        OverrideForHost(IHost lh, ImmutableArray<SchemaOverride>? schemaOverrides)
    {
        if (schemaOverrides == null) return null;
        return schemaOverrides.FirstOrDefault<SchemaOverride>(o =>
            o.HostCategory == lh.Configuration.HostCategory && o.Host == lh.Configuration.Host);
    }

    public static IEnumerable<(string, string)> ParseOverrideTokens(string query)
    {
        var r = new Regex(TokenPattern, RegexOptions.None, new TimeSpan(0, 1, 0));
        var matches = r.Match(query);
        return matches.Groups.Values.Select(g => g.Value).Skip(1).Distinct()
            .Select(t => ($"{{{{TONIC_OVERRIDE_SCHEMA_TOKEN!{t}}}}}", t)).ToArray();
    }

    public static string ReplaceToken(string query, string schema)
    {
        return Regex.Replace(query, TokenPattern, schema);
    }

    public static ImmutableArray<SchemaOverride> ParseHostSchemaOverrides(TomlTable config)
    {
        var retval = new List<SchemaOverride>();
        if (!config.ContainsKey("HostSchemaOverride")) return retval.ToImmutableArray();
        var overrides = (TomlTableArray)config["HostSchemaOverride"];
        for (var i = 0; i < overrides.Count; ++i)
        {
            var schemaOverride = overrides[i];
            retval.Add(new SchemaOverride(schemaOverride));
        }

        var distinctCount = retval.GroupBy(a => (a.HostCategory, a.TableName)).Count();
        if (distinctCount != retval.Count)
            throw new ArgumentException("Cannot specify multiple schema overrides for a single table");
        return retval.ToImmutableArray();
    }

    private SchemaOverride(TomlTable schemaOverride)
    {
        HostCategory = schemaOverride.Get<string>("HostCategory");
        Sid = schemaOverride.Get<string>("Sid");
        Schema = schemaOverride.Get<string>("Schema");
        TableName = schemaOverride.Get<string>("Table");
        Host = schemaOverride.Get<string>("Host");
    }
}