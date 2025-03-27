using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Serilog;
using Tonic.Common.Models;

namespace Tonic.Subsetter.Utils;

public interface IPrimaryKeyCache
{
    AddKeyContext PrepareAddKeys(SQLiteConnection connection, bool rollback, IList<string>? columns = null,
        string? tableId = null);
    void AddKey(string?[] key, AddKeyContext context);
    bool[] ContainsKeys(IEnumerable<string?[]> keys, MultiStringKey multiStringKey);
    IEnumerable<string[]> GetKeys(ImmutableArray<string> multiStringKey);
    int CountKeys();
}

public class PrimaryKeyCache : IPrimaryKeyCache
{
    private readonly string _id;
    private readonly ImmutableArray<string> _primaryKeys;
    private readonly PrimaryKeyCaches _parentCacheCollection;

    public PrimaryKeyCache(MultiStringKey multiStringKey, PrimaryKeyCaches parentCacheCollection)
    {
        var hostCategory = multiStringKey[0];
        _primaryKeys = multiStringKey.From(2).Parts;
        if (!_primaryKeys.Any())
        {
            throw new InvalidOperationException(
                $"Error: attempting to create a key cache for invalid multi string key: {multiStringKey}");
        }

        _id = $"{hostCategory}_{multiStringKey[1]}_{string.Join("_", _primaryKeys)}";
        _parentCacheCollection = parentCacheCollection;
        var columnDefs = string.Join(", ", _primaryKeys.Select(c => $"{c} blob"));
        var columnNames = string.Join(", ", _primaryKeys);
        var createCacheStatement = $"CREATE TABLE IF NOT EXISTS \"{_id}\" ({columnDefs}, PRIMARY KEY({columnNames}));";
        var createScratchCacheStatement = $"CREATE TABLE IF NOT EXISTS \"{ScratchName(_id)}\" ({columnDefs});";

        using var connection = _parentCacheCollection.OpenConnection();
        RunCommand(createCacheStatement, connection);
        Log.Debug("Created Primary Key Cache for {KeySet}", multiStringKey);
        foreach (var column in _primaryKeys)
        {
            RunCommand($"CREATE INDEX IF NOT EXISTS \"{_id}_{column}\" ON \"{_id}\" ({column});", connection);
        }

        RunCommand(createScratchCacheStatement, connection);
    }

    public AddKeyContext PrepareAddKeys(SQLiteConnection connection, bool rollback, IList<string>? columns = null,
        string? tableId = null)
    {
        columns ??= _primaryKeys;
        var cmd = connection.CreateCommand();

        var columnNames = string.Join(", ", columns);
        var parameterNames = Enumerable.Range(0, columns.Count).Select(i => $"@ID{i}").ToImmutableArray();
        cmd.CommandText =
            $"INSERT INTO \"{tableId ?? _id}\" ({columnNames}) VALUES ({string.Join(", ", parameterNames)}) ON CONFLICT DO NOTHING;";
        foreach (var pn in parameterNames)
        {
            cmd.Parameters.Add(pn, DbType.String);
        }

        return new AddKeyContext(cmd, parameterNames, rollback);
    }

    public void AddKey(string?[] key, AddKeyContext context)
    {
        if (key.Length != context.ParameterNames.Count)
        {
            throw new InvalidOperationException("Row doesn't have the right number of values.");
        }

        foreach (var (first, second) in key.Zip(context.ParameterNames))
        {
            context.Command.Parameters[second].Value = first == null ? DBNull.Value : first;
        }

        try
        {
            context.Command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            // try catch in place to address CheckMarx flag of an unhandled exception
            Log.Logger.Debug(ex, "Caught exception while adding keys to cache");
            throw;
        }
    }

    private int AddKeys(IEnumerable<string?[]> keys, AddKeyContext context)
    {
        var keyCount = 0;
        foreach (var key in keys)
        {
            AddKey(key, context);
            keyCount += 1;
        }

        return keyCount;
    }

    public bool[] ContainsKeys(IEnumerable<string?[]> keys, MultiStringKey multiStringKey) =>
        ContainsKeys(keys, multiStringKey.From(2).Parts);

    private bool[] ContainsKeys(IEnumerable<string?[]> keys, ImmutableArray<string> pkColumns)
    {
        var selectExpr = string.Join(" OR ", pkColumns.Select(i => $"r.{i} IS NOT NULL"));
        var columnEquality = string.Join(" AND ", pkColumns.Select(i => $"r.{i} = l.{i}"));

        using var connection = _parentCacheCollection.OpenConnection();
        using var addKeyContext =
            PrepareAddKeys(connection, true, pkColumns, ScratchName(_id));
        var keysAdded = AddKeys(keys, addKeyContext);

        var foundKeys = new bool[keysAdded];
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"SELECT {selectExpr} FROM \"{ScratchName(_id)}\" AS l LEFT JOIN \"{_id}\" AS r ON {columnEquality};";

        using var reader = cmd.ExecuteReader();
        var idx = 0;
        while (reader.Read())
        {
            if (idx >= keysAdded)
            {
                throw new InvalidOperationException(
                    $"Error selecting cached keys for {pkColumns} cache, created with {selectExpr}");
            }

            foundKeys[idx++] = reader.GetBoolean(0);
        }

        if (idx != keysAdded)
        {
            throw new InvalidOperationException("Not enough results coming from key check.");
        }

        return foundKeys;
    }

    public IEnumerable<string[]> GetKeys(ImmutableArray<string> pkColumns)
    {
        using var connection = _parentCacheCollection.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT {string.Join(",", pkColumns)} FROM \"{_id}\"";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (var i = 0; i < row.Length; ++i) row[i] = reader.GetString(i);
            yield return row;
        }
    }

    public int CountKeys()
    {
        using var connection = _parentCacheCollection.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM \"{_id}\"";
        using var reader = cmd.ExecuteReader();
        return !reader.Read() ? 0 : reader.GetInt32(0);
    }

    public static void RunCommand(string command, SQLiteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = command;
        cmd.ExecuteNonQuery();
    }

    private static string ScratchName(string original)
    {
        return original + "_scratch";
    }
}

public sealed class AddKeyContext : IDisposable
{
    public readonly SQLiteCommand Command;
    public readonly IList<string> ParameterNames;
    private readonly SQLiteTransaction? _transaction;
    private readonly bool _rollback;

    public AddKeyContext(SQLiteCommand command, IList<string> parameterNames, bool rollback)
    {
        Command = command;
        ParameterNames = parameterNames;
        _rollback = rollback;
        _transaction = _rollback ? Command.Connection.BeginTransaction() : null;
    }

    public void Dispose()
    {
        Command.Dispose();

        if (!_rollback || _transaction == null) return;
        _transaction.Rollback();
        _transaction.Dispose();
    }
}