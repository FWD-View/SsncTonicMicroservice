using System.Collections.Concurrent;
using System.Data.SQLite;
using System.IO;
using Tonic.Common.Models;

namespace Tonic.Subsetter.Utils;

public interface IPrimaryKeyCaches
{
    SQLiteConnection OpenConnection();

    IPrimaryKeyCache CreateCache(MultiStringKey multiStringKey);

    IPrimaryKeyCache GetCache(MultiStringKey multiStringKey);

    bool HasCache(MultiStringKey multiStringKey);

    IPrimaryKeyCache this[MultiStringKey multiStringKey] { get; }
}

public class PrimaryKeyCaches : IPrimaryKeyCaches
{
    private readonly ConcurrentDictionary<MultiStringKey, IPrimaryKeyCache> _mskCaches;
    private readonly string _sqlitePath;

    public PrimaryKeyCaches(string runId)
    {
        _mskCaches = new ConcurrentDictionary<MultiStringKey, IPrimaryKeyCache>();
        _sqlitePath = Path.Combine(Path.GetTempPath(), $"tonic-subset-{runId}.db");
        using var connection = OpenConnection();
        PrimaryKeyCache.RunCommand("PRAGMA journal_mode=WAL", connection);
    }

    public SQLiteConnection OpenConnection()
    {
        var retval = new SQLiteConnection($"Data Source={_sqlitePath}");
        retval.Open();
        return retval;
    }

    public IPrimaryKeyCache CreateCache(MultiStringKey multiStringKey)
    {
        if (!_mskCaches.ContainsKey(multiStringKey))
        {
            _mskCaches.TryAdd(multiStringKey, new PrimaryKeyCache(multiStringKey, this));
        }

        return GetCache(multiStringKey);
    }

    public IPrimaryKeyCache GetCache(MultiStringKey multiStringKey) => _mskCaches[multiStringKey];
    public IPrimaryKeyCache this[MultiStringKey multiStringKey] => _mskCaches[multiStringKey];
    public bool HasCache(MultiStringKey multiStringKey) => _mskCaches.ContainsKey(multiStringKey);
}