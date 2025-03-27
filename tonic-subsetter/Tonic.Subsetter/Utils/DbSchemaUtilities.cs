using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Serilog;
using Tonic.Common;
using Tonic.Common.CommonAbstraction;
using Tonic.Common.Helpers;
using Tonic.Common.Models;

namespace Tonic.Subsetter.Utils;

public record DbSchemaUtilities
{
    private static readonly TimeSpan BackoffDuration = TimeSpan.FromSeconds(3).Duration();

    public static Func<IHost, T?> WithRetries<T>(Func<IHost, T?> func)
    {
        return host =>
        {
            var attempts = 0;
            const int maxRetryAttempts = 3;

            var retval = default(T);
            while (attempts < maxRetryAttempts)
            {
                attempts += 1;
                try
                {
                    retval = func(host);
                    break;
                }
                catch (Exception exception)
                {
                    if (attempts == maxRetryAttempts)
                        throw new InvalidOperationException($"Error executing query on {host.Configuration.Host}",
                            exception);
                    Log.Error(exception, "Error executing query on {Host}, attempting retry #{Number}",
                        host.Configuration.Host, attempts);
                    Thread.Sleep(BackoffDuration);
                }
            }

            return retval;
        };
    }

    public static ImmutableDictionary<Table, ImmutableArray<Column>> CollectSourceSchemaInformation(
        IHostsService connections)
    {
        var retval = new ConcurrentDictionary<Table, List<Column>>();
        connections.RunOnCanDbLinkHostsAndWait((_, host) =>
        {
            var columns = WithRetries(DBAbstractionLayer.GetColumns)(host);
            foreach (var group in columns.GroupBy(c => new Table(c.HostCategoryName, c.TableName)))
            {
                retval.TryAdd(group.Key, group.ToList());
            }
        });
        return retval.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());
    }

    public static ImmutableHashSet<Table> CollectIoTTables(IHostsService connections)
    {
        var allIoTTables = new ConcurrentBag<Table>();
        connections.RunOnCanDbLinkHostsAndWait((_, host) =>
        {
            var iotTables = WithRetries(DBAbstractionLayer.GetIoTTables)(host);
            if (iotTables == null) return;
            foreach (var table in iotTables)
            {
                allIoTTables.Add(table);
            }
        });
        return allIoTTables.ToImmutableHashSet();
    }
}