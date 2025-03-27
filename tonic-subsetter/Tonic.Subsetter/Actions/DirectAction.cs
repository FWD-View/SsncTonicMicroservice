using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Tonic.Common;
using Tonic.Common.CommonAbstraction;
using Tonic.Common.Enums;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;

namespace Tonic.Subsetter.Actions;

public class DirectAction
{
    private readonly DirectSubsetTarget _directSubsetTarget;
    private readonly IList<Column> _columns;
    private int QueryBatchSize { get; }
    private ISubsetConfig Config { get; }

    public DirectAction(ISubsetConfig config, DirectSubsetTarget directSubsetTarget, IList<Column> columns,
        int queryBatchSize = 10)
    {
        Config = config;
        _directSubsetTarget = directSubsetTarget;
        _columns = columns;
        QueryBatchSize = queryBatchSize;
    }

    public void DirectSubsetTable(BlockingCollection<(string, Dictionary<string, string>)> queriesQueue)
    {
        var table = _directSubsetTarget.Table;
        var schemaToken = SchemaOverride.SchemaTokenForTable(table, Config.SchemaOverrides);

        var columnsSelectStr = DBAbstractionLayer.ColumnsSelectStr(_columns, schemaToken);
        var query = _directSubsetTarget switch
        {
            { Clause: { }, IdColumn: null } dst =>
                $"SELECT {columnsSelectStr} FROM {schemaToken}.{table.TableName} {dst.Clause}",
            { Percent: { } } dst =>
                $"SELECT {columnsSelectStr} FROM {schemaToken}.{table.TableName} SAMPLE({dst.Percent})",
            _ => null
        };
        if (query != null)
        {
            queriesQueue.Add((query, new Dictionary<string, string>()));
        }

        Log.Logger.Debug("Directly querying with {Count} id keys", _directSubsetTarget.IdKeys?.Count ?? 0);
        if (_directSubsetTarget.IdKeys == null || _directSubsetTarget.IdColumn == null) return;
        var columnAsArr = new[] { _directSubsetTarget.IdColumn };
        var batch = new List<string[]>();
        foreach (var id in _directSubsetTarget.IdKeys)
        {
            batch.Add(new[] { id.ToString() });
            if (batch.Count != QueryBatchSize) continue;
            queriesQueue.Add(IdQuery(_directSubsetTarget, columnAsArr, batch, columnsSelectStr, schemaToken));
            batch.Clear();
        }

        if (batch.Any())
        {
            queriesQueue.Add(IdQuery(_directSubsetTarget, columnAsArr, batch, columnsSelectStr, schemaToken));
        }
    }

    private static (string idQuery, Dictionary<string, string> paramDict) IdQuery(
        DirectSubsetTarget directSubsetTarget, IList<string> columnAsArr, IList<string[]> batch,
        string columnsSelectStr, string schemaToken)
    {
        var (keyClause, paramDict) = Utilities.KeyClause(columnAsArr, batch);
        var interpolatedClause = directSubsetTarget.Clause == null
            ? $"WHERE {keyClause}"
            : directSubsetTarget.Clause.Replace("{{TONIC_IDS}}", keyClause);
        var idQuery =
            $"SELECT {columnsSelectStr} FROM {schemaToken}.{directSubsetTarget.Table.TableName} {interpolatedClause}";
        return (idQuery, paramDict);
    }
}