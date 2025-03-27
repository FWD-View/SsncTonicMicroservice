using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Tonic.Common;
using Tonic.Common.CommonAbstraction;
using Tonic.Common.Enums;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Tonic.Subsetter.Utils;

namespace Tonic.Subsetter.Actions;

public record UpstreamAction
{
    private ISubsetConfig Config { get; } = null!;
    private readonly PrimaryKeySet _primaryKeySet;
    private readonly IPrimaryKeyCaches _keyCaches;
    private readonly ImmutableArray<UpstreamFilter> _additionalFilters;
    private readonly UpstreamGroupLimit? _groupLimit;
    private static int MultiKeyTableLimit => 3;

    public UpstreamAction(ISubsetConfig config, IPrimaryKeyCaches keyCaches, PrimaryKeySet primaryKeySet)
    {
        _keyCaches = keyCaches;
        Config = config;
        _primaryKeySet = primaryKeySet;
        _additionalFilters = Config.UpstreamFilters.Where(uf => uf.Table.Equals(primaryKeySet.Table))
            .ToImmutableArray();
        _groupLimit = Config.UpstreamGroupLimits.SingleOrDefault(gl => gl.Table.Equals(primaryKeySet.Table));
    }

    public Task GeneratePrincipalUpstreamQueries(
        BlockingCollection<(string, Dictionary<string, string>)> queriesQueue)
    {
        var table = _primaryKeySet.Table;
        var principalKeyCache = CanUpstreamTable();
        if (principalKeyCache == null)
        {
            Log.Warning("Could not find {Table} to have upstreams queries, returning", table);
            queriesQueue.CompleteAdding();
            return Task.CompletedTask;
        }

        var principalFk = _primaryKeySet.PrincipalForeignKeys.First();
        return Task.Run(() =>
        {
            var count = 0;
            var keyBatch = new List<string[]>(Config.QueryBatchSize);
            foreach (var principalKey in principalKeyCache.GetKeys(principalFk.PrimaryKeyColumns))
            {
                keyBatch.Add(principalKey);
                count += 1;
                if (keyBatch.Count < Config.QueryBatchSize) continue;
                queriesQueue.Add(UpstreamSubsetTableQuery(keyBatch, principalFk));
                keyBatch.Clear();
            }

            if (keyBatch.Count != 0)
            {
                queriesQueue.Add(UpstreamSubsetTableQuery(keyBatch, principalFk));
            }

            queriesQueue.CompleteAdding();
            Log.Debug("Found {Count} upstream principal keys for {Table}", count, table);
        });
    }

    public Task GenerateMultiPrincipalUpstreamQueries(
        BlockingCollection<(string, Dictionary<string, string>)> queriesQueue)
    {
        var table = _primaryKeySet.Table;
        // ReSharper disable once InvertIf
        if (!CanUpstreamMultiTableKey())
        {
            queriesQueue.CompleteAdding();
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            var count = 0;
            foreach (var multiKeyBatch in MultiTableKeyBatch(_primaryKeySet.PrincipalForeignKeys,
                         _primaryKeySet.PrincipalKeyTables))
            {
                var emptyBatch = multiKeyBatch.Where(v => !v.Value.Any()).ToList();
                if (emptyBatch.Any())
                    continue;

                var upstreamSubsetTableQuery = UpstreamSubsetTableQuery(multiKeyBatch);
                queriesQueue.Add(upstreamSubsetTableQuery);
                count += 1;
            }

            queriesQueue.CompleteAdding();
            Log.Debug("Generated {Count} upstream queries for {Table}", count, table);
        });
    }

    // When we need to create an upstream query based on multiple key caches, we  create all query batches by
    // splitting the key caches into groups by the max batch size, then create query by enumerating all possible
    // combinations of these groups
    // ReSharper disable once ParameterTypeCanBeEnumerable.Global
    public IEnumerable<Dictionary<Table, List<string>>> MultiTableKeyBatch(
        ImmutableArray<ForeignKey> principalForeignKeys,
        ImmutableArray<Table> principalKeyTables,
        Dictionary<Table, List<string>>? multiKeyBatch = null)
    {
        if (!principalKeyTables.Any())
        {
            yield return CloneKeyBatch(multiKeyBatch);
            yield break;
        }

        var table = principalKeyTables.First();
        var principalFk = principalForeignKeys.First(table.IsPrimaryKeyOf);
        var principalKeyColumns = principalFk.PrimaryKeyColumns;
        var primaryKeySet = new MultiStringKey(table, principalKeyColumns);
        var principalKeyCache = _keyCaches.GetCache(primaryKeySet);
        var tablesTail = principalKeyTables.Skip(1).ToImmutableArray();
        var foreignKeysTail = principalForeignKeys.Skip(1).ToImmutableArray();


        var multiKeyBatchLookup = multiKeyBatch ?? new Dictionary<Table, List<string>>();
        multiKeyBatchLookup[table] = new List<string>(Config.QueryBatchSize);
        using var principalKeyCacheEnumerable = principalKeyCache.GetKeys(principalKeyColumns).GetEnumerator();
        while (true)
        {
            var hasNext = principalKeyCacheEnumerable.MoveNext();
            while (hasNext && multiKeyBatchLookup[table].Count < Config.QueryBatchSize)
            {
                var principalKey = principalKeyCacheEnumerable.Current;
                multiKeyBatchLookup[table].AddRange(principalKey);
                hasNext = principalKeyCacheEnumerable.MoveNext();
            }

            if (!hasNext && !multiKeyBatchLookup[table].Any())
            {
                break;
            }


            foreach (var batch in MultiTableKeyBatch(foreignKeysTail, tablesTail, multiKeyBatchLookup))
            {
                yield return batch;
            }

            multiKeyBatchLookup[table].Clear();

            if (hasNext) multiKeyBatchLookup[table].AddRange(principalKeyCacheEnumerable.Current);
        }
    }

    private (string, Dictionary<string, string> paramsDict)
        UpstreamSubsetTableQuery(IList<string[]> keyBatch, ForeignKey principalFk)
    {
        var table = _primaryKeySet.Table;
        var columns = _primaryKeySet.Columns;
        var schemaToken = SchemaOverride.SchemaTokenForTable(_primaryKeySet.Table, Config.SchemaOverrides);

        var foreignKeyColumns = principalFk.ForeignKeyColumns
            .Zip(principalFk.PrimaryKeyColumns)
            .OrderBy(tup => tup.Second)
            .Select(tup => tup.First)
            .ToList();

        var filterClauses = _additionalFilters.Any()
            ? " AND " + string.Join(" AND ", _additionalFilters.Select(f => f.Clause))
            : "";

       // var columnsSelectStr = DBAbstractionLayer.ColumnsSelectStr(columns, (int)DatabaseType.Oracle);
        var columnsSelectStr = DBAbstractionLayer.ColumnsSelectStr(columns, schemaToken);
        var (keyClause, paramsDict) = Utilities.KeyClause(foreignKeyColumns, keyBatch);
        if (_groupLimit == null)
        {
            return (
                $"SELECT {columnsSelectStr} FROM {schemaToken}.{table.TableName} WHERE {keyClause} {filterClauses}",
                paramsDict);
        }

        var subSelect =
            $@"SELECT {columnsSelectStr},
                          rank() OVER (PARTITION BY {_groupLimit.GroupBy} ORDER BY {_groupLimit.OrderBy} DESC) AS tonic__subset__rank
                   FROM {schemaToken}.{table.TableName}
                   WHERE {table.TableName}.{keyClause} {filterClauses}";

        var subQueryColumnsSelectStr =
            DBAbstractionLayer.ColumnsSelectStr(columns.Select(c => c.WithTable("TONIC__SUBSELECT")).ToList(), (int)DatabaseType.Oracle);

        return (
            $"SELECT {subQueryColumnsSelectStr} FROM ({subSelect}) TONIC__SUBSELECT WHERE TONIC__SUBSELECT.tonic__subset__rank <= {_groupLimit.Limit}",
            paramsDict);
    }

    private (string, Dictionary<string, string>)
        UpstreamSubsetTableQuery(Dictionary<Table, List<string>> multiKeyBatch)
    {
        var table = _primaryKeySet.Table;
        var keyColumnNames = _primaryKeySet.PrincipalForeignKeys
            .SelectMany(fk => fk.MultiTableIndexColumnIndices.Zip(fk.ForeignKeyColumns))
            .OrderBy(tuple => tuple.First).Select(tuple => tuple.Second)
            .ToList();
        var tableColumnLookup =
            _primaryKeySet.PrincipalForeignKeys.ToDictionary(fk => fk.PrimaryKeyTable,
                fk => fk.MultiTableIndexColumnIndices.First());

        var keyValuesForIndexes = multiKeyBatch.OrderBy(kvp => tableColumnLookup[kvp.Key.TableName])
            .Select(kvp => kvp.Value).ToList();

        var columnsSelectStr = DBAbstractionLayer.ColumnsSelectStr(_primaryKeySet.Columns, (int)DatabaseType.Oracle);

        var (keyClause, paramsDict) = Utilities.KeyClauseByMembershipGroup(keyColumnNames, keyValuesForIndexes);
        var schemaToken = SchemaOverride.SchemaTokenForTable(table, Config.SchemaOverrides);
        return ($"SELECT {columnsSelectStr} FROM {schemaToken}.{table.TableName} WHERE {keyClause}", paramsDict);
    }

    private static Dictionary<Table, List<string>> CloneKeyBatch(Dictionary<Table, List<string>>? multiKeyBatch)
    {
        var clone = new Dictionary<Table, List<string>>();
        if (multiKeyBatch == null) return clone;
        foreach (var (keyTable, keyCacheSubset) in multiKeyBatch)
        {
            clone[keyTable] = new List<string>(keyCacheSubset);
        }

        return clone;
    }

    private bool CanUpstreamMultiTableKey()
    {
        string? message = null;
        var principalKeyTables = _primaryKeySet.PrincipalKeyTables;
        if (principalKeyTables.Length > MultiKeyTableLimit)
            message =
                $"Cannot build multi-table batch for upstream query: principal key table count {principalKeyTables.Length} exceeds limit {MultiKeyTableLimit}";
        if (_primaryKeySet.PrincipalForeignKeys.Any(fk => fk.PrimaryKeyColumns.Length > 1))
            message =
                $"Table {_primaryKeySet.Table} targeted for upstream subsetting with multi-table key must define one [[ForeignKey]] per principal table";

        if (message != null) throw new ConfigurationErrorsException(message);


        // for each principal table, assert cache exists
        var principalMultiStringKeys = principalKeyTables
            .Select(table =>
                new MultiStringKey(table,
                    _primaryKeySet.PrincipalForeignKeys.Single(table.IsPrimaryKeyOf).PrimaryKeyColumns));
        foreach (var principalMultiStringKey in principalMultiStringKeys)
        {
            if (_keyCaches.HasCache(principalMultiStringKey)) continue;
            Log.Error("{Table} is missing principal key cache for {MultiStringKey}, cannot upstream subset",
                _primaryKeySet.Table,
                principalMultiStringKey);
            return false;
        }

        return true;
    }

    private IPrimaryKeyCache? CanUpstreamTable()
    {
        var principalKeyTables = _primaryKeySet.PrincipalKeyTables;
        if (_primaryKeySet.PrincipalForeignKeys.Count(fk => fk.IsPrincipal) > 1)
        {
            Log.Logger.Warning(
                "Table targeted for upstream subsetting does not specify exactly 1 principal foreign key: {Table}, Principals: ({PrincipalKeyTables}), skipping",
                _primaryKeySet.Table, string.Join(", ", principalKeyTables));
            return null;
        }

        if (_primaryKeySet.PrincipalForeignKeys.FirstOrDefault() == null)
        {
            Log.Warning(
                "Table targeted for upstream subsetting missing a principal foreign key: {Table}, skipping",
                _primaryKeySet.Table);
            return null;
        }

        // we need to know that the particular pk permutation `table` uses as its principal cache is present 
        var primaryKeyParts = _primaryKeySet.PrincipalForeignKeys.First().PrimaryKeyColumns.OrderBy(s => s);
        var principalKeySet = new MultiStringKey(principalKeyTables.First(), primaryKeyParts);
        if (_keyCaches.HasCache(principalKeySet)) return _keyCaches[principalKeySet];
        Log.Warning(
            "Table targeted for upstream subsetting missing a principal foreign key cache: {Table}:[{Cache}], skipping",
            _primaryKeySet.Table, _primaryKeySet.PrincipalForeignKeys.First().ForeignKeyColumns);
        return null;
    }
}