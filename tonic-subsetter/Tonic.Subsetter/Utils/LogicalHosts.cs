using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Tonic.Common;
using Tonic.Common.Configs;
using Tonic.Common.Enums;
using Tonic.Common.Helpers;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Tonic.Subsetter.Utils;

public interface IHostsService : IEnumerable<(string, IHost)>
{
    List<IHost> this[string category] { get; }

    public Task[] RunOnCategory(string hostCategory, Action<IHost> action);

    public Task[] RunOnCategoryWithMultiplexedQueue<T>(string hostCategory,
        BlockingCollection<T> sourceQueue,
        Action<IHost, BlockingCollection<T>> action);

    public void RunOnCanDbLinkHostsAndWait(Action<string, IHost> action);

    public bool IsDB2(string hostCategory);
    public string FindSid(string hostCategory);
}

public class HostsService : IHostsService
{
    private readonly ImmutableDictionary<string, List<IHost>> _hosts;

    public HostsService(IEnumerable<HostConfig> configs,
        ImmutableArray<SchemaOverride>? schemaOverrides = null)
    {
        var tmpHosts = new Dictionary<string, List<IHost>>();
        foreach (var config in configs)
        {
            tmpHosts.TryAdd(config.HostCategory, new List<IHost>());
            if (config.DbType== DatabaseType.DB2.ToString())
            {
                tmpHosts[config.HostCategory].Add(CreateDB2Connection(config, schemaOverrides));
            }
            else
            {
                tmpHosts[config.HostCategory].Add(CreateConnection(config, schemaOverrides));
            }
        }

        _hosts = tmpHosts.ToImmutableDictionary();
    }


    public List<IHost> this[string category] => _hosts[category];

    public IEnumerator<(string, IHost)> GetEnumerator()
    {
        foreach (var (lhCategory, host) in _hosts)
        {
            yield return (lhCategory, host.First());
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Task[] RunOnCategory(string hostCategory, Action<IHost> action)
    {
        var tasks = new List<Task>();
        foreach (var connection in _hosts[hostCategory])
        {
            tasks.Add(Task.Run(() => action(connection)));
        }

        return tasks.ToArray();
    }

    public Task[] RunOnCategoryWithMultiplexedQueue<T>(string hostCategory,
        BlockingCollection<T> sourceQueue, Action<IHost, BlockingCollection<T>> action)
    {
        var tasks = new List<Task>();
        var multiplexedQueues = new Dictionary<IHost, BlockingCollection<T>>();

        foreach (var connection in _hosts[hostCategory])
        {
            multiplexedQueues[connection] = new BlockingCollection<T>();
        }

        var multiplexer = Task.Run(() =>
        {
            foreach (var item in sourceQueue.GetConsumingEnumerable())
            {
                foreach (var oneQueue in multiplexedQueues.Values) oneQueue.Add(item);
            }

            foreach (var oneQueue in multiplexedQueues.Values) oneQueue.CompleteAdding();
        });
        tasks.Add(multiplexer);

        foreach (var connection in _hosts[hostCategory])
        {
            var queue = multiplexedQueues[connection];
            tasks.Add(Task.Run(() => action(connection, queue)));
        }

        return tasks.ToArray();
    }

    public bool IsDB2(string hostCategory)
    {
       
       return _hosts[hostCategory].Any(x => x.Configuration.DbType== DatabaseType.DB2.ToString());
    }
    public string FindSid(string hostCategory)
    {
        return _hosts[hostCategory].Select(x => x.Configuration.Sid).FirstOrDefault();
    }

    public void RunOnCanDbLinkHostsAndWait(Action<string, IHost> action)
    {
        var tasks = new List<Task>();
        // Bit of a HACK, but basically this says to use the CanDbLink host for Source Connections
        var hostPerCategory = _hosts.Values
            .Select(hosts => hosts.Where(host => host.Configuration.CanDbLink));
        foreach (var connections in hostPerCategory)
        {
            tasks.AddRange(connections.Select(connection =>
                Task.Run(() => action(connection.Configuration.HostCategory, connection))));
        }

        Task.WaitAll(tasks.ToArray());
    }

    private static Host CreateConnection(HostConfig config,
        ImmutableArray<SchemaOverride>? schemaOverrides = null)
    {
        return new Host(config.Schema, config, schemaOverrides ?? ImmutableArray<SchemaOverride>.Empty);
    }

    private static DB2Host CreateDB2Connection(HostConfig config,
        ImmutableArray<SchemaOverride>? schemaOverrides = null)
    {
        return new DB2Host(config.Schema, config, schemaOverrides ?? ImmutableArray<SchemaOverride>.Empty);
    }
}