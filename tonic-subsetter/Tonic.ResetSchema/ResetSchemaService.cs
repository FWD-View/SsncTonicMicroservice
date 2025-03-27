using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using Tonic.Common;
using Tonic.Common.Configs;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;

namespace Tonic.ResetSchema;

public class ResetSchemaService
{
    private readonly OracleCommandRunner _oracleCommandRunner;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ResetSchemaService(OracleCommandRunner oracleCommandRunner, CancellationTokenSource cancellationTokenSource)
    {
        _oracleCommandRunner = oracleCommandRunner;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public void Run(ResetSchemaConfig config)
    {
        var tables = config.TableSource == TableSourceEnum.Config
            ? config.AllReferencedTables
            : GetTablesFromSource(config.SourceHosts);

        if (config.TruncateOnly)
        {
            Log.Information("Truncate only mode detected: existing tables will be retained & truncated");
            TruncateTables(config, tables);
            return;
        }

        Log.Information("Could not detect `TruncateOnly` config item; resetting schema's");

        DestroyDestinations(config);
        DestroyDbLinks(config.SourceHosts, config.DestinationHosts, config.DbaUser, config.DbaPassword);

        SetupDestinationSchemas(config.SourceHosts, config.DestinationHosts, tables, config.KeepPrimaryKeys,
            config.MergePartitions, config.DbaUser, config.DbaPassword, config.DebugImport);
        GrantCrossTableSelects(tables, config.DestinationHosts, config.DbaUser, config.DbaPassword);
        GrantTableCreation(config.DestinationHosts, config.DbaUser, config.DbaPassword);
        Log.Information("Running post reset schema reset scripts... ");
        RunPostResetSchemaScripts(config.DbaUser, config.DbaPassword, config.DestinationHosts,
            config.PostResetSchemaScripts);
        Log.Information("Finished altering destination databases");
    }

    private static void TruncateTables(ResetSchemaConfig config, IList<Table> tables) =>
        Parallel.ForEach(config.DestinationHosts, (host) =>
        {
            var adminHost = CreateAdminHost(host, config.DbaUser, config.DbaPassword);
            var truncationTables = tables.Where(t => t.HostCategory == host.Name);
            foreach (var table in truncationTables)
            {
                try
                {
                    adminHost.ExecuteNonQuery(
                        $"TRUNCATE TABLE {host.Configuration.Schema}.{table.TableName}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not truncate {Table}", table);
                }
            }
        });

    public static void GrantCrossTableSelects(IList<Table> destinationTables,
        IList<IHost> configDestinationHosts,
        string dbaUser, string dbaPassword)
    {
        Log.Information("Granting cross table query permissions for destination users");

        var hostTableGroupings = destinationTables
            .GroupBy(t => t.HostCategory, (lhc, tables) => (lhc, tables.ToList())).ToList();
        var hostCategoryLookup =
            configDestinationHosts.ToDictionary(h => h.Configuration.HostCategory);
        var grantStatements = hostCategoryLookup.Select(kvp =>
        {
            var (hostCategory, destinationHost) = kvp;
            var otherTables = hostTableGroupings
                .Where(p => p.Item1 != hostCategory)
                .SelectMany(p =>
                    p.Item2.Select(s => $"{hostCategoryLookup[p.Item1].Configuration.Schema}.{s.TableName}"))
                .ToArray();
            return (destinationHost, otherTables);
        });

        try
        {
            foreach (var (host, otherTables) in grantStatements)
            {
                var adminHost = CreateAdminHost(host, dbaUser, dbaPassword);
                foreach (var table in otherTables)
                {
                    var user = host.Configuration.User;
                    Log.Debug("Granting select privileges to {User} for {Table}",
                        user, table);
                    adminHost.ExecuteNonQuery($"GRANT SELECT ON {table} TO {user}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error while granting select privileges to users; exiting");
        }

        Log.Information("Finished granting cross table query permissions for destination users");
    }

    public static void RunPostResetSchemaScripts(string dbaUser, string dbaPassword,
        IList<IHost> destinationHosts, ImmutableDictionary<string, List<string>> postResetSchemaScripts) =>
        Parallel.ForEach(postResetSchemaScripts.Keys, (hostCategory) =>
        {
            var host = destinationHosts.FirstOrDefault(dst => dst.Name == hostCategory);
            if (host == null) return;
            var adminHost = CreateAdminHost(host, dbaUser, dbaPassword);
            foreach (var script in postResetSchemaScripts[hostCategory])
            {
                try
                {
                    var sqlScript = script.Replace(hostCategory, host.Configuration.Schema);
                    adminHost.ExecuteNonQuery(sqlScript);
                }
                catch (Exception ex)
                {
                    Log.Information("Error running post reset schema script on {Host}: {Message}",
                        host.Name, ex.Message);
                    throw;
                }
            }
        });

    private static IList<Table> GetTablesFromSource(IList<IHost> sourceHosts)
    {
        var tables = new ConcurrentBag<Table>();
        Parallel.ForEach(sourceHosts, host =>
        {
            var columns = OracleUtilities.GetColumns(host);
            foreach (var col in columns)
            {
                tables.Add(new Table(col.HostCategoryName, col.TableName));
            }
        });
        return tables.Distinct().ToImmutableArray();
    }

    public static void DestroyDestinations(ResetSchemaConfig config)
    {
        Log.Information("Cleaning destination DB... ");
        Parallel.ForEach(config.DestinationHosts, (host) =>
        {
            var adminHost = CreateAdminHost(host, config.DbaUser, config.DbaPassword);
            var retry = new List<(string, Exception)>();
            do
            {
                retry.Clear();
                var dropCommands = CreateDropCommands(adminHost);

                foreach (var dropCommand in dropCommands)
                {
                    try
                    {
                        adminHost.ExecuteNonQuery(dropCommand);
                    }
                    catch (Exception e)
                    {
                        retry.Add((dropCommand, e));
                    }
                }

                if (!retry.Any() || retry.Count != dropCommands.Count) continue;
                var msg =
                    $"Could not execute drop commands, here is one command that didn't work:\n{retry[0].Item1}";
                throw new InvalidOperationException(msg, retry[0].Item2);
            } while (retry.Any());
        });
        Log.Information("Done");
    }
    
    public static void DestroyDbLinks(
        IList<IHost> sourceHosts,
        IEnumerable<IHost> destinationHosts,
        string dbaUser,
        string dbaPassword)
    {
        foreach (var host in destinationHosts)
        {
            var adminHost = CreateAdminHost(host, dbaUser, dbaPassword);
            var category = host.Configuration.HostCategory;

            var linkNames = sourceHosts
                .Where(lh => lh.Configuration.HostCategory == category && lh.Configuration.CanDbLink)
                .Select(lh => lh.Configuration.CreateLinkName())
                .Distinct();

            foreach (var linkName in linkNames)
            {
                //
                // Close & Drop DB Link
                //
                try
                {
                    adminHost.DropDbLink(linkName);
                }
                catch (OracleException e) when(e.Number == 2024)
                {
                    /* not worried about failures */
                    Log.Warning("Not dropping {LinkName} because it has already been dropped", linkName);
                }
            }
        }
    }

    private void SetupDestinationSchemas(IList<IHost> sourceHosts,
        IEnumerable<IHost> destinationHosts,
        IList<Table> tables,
        bool keepPrimaryKeys,
        bool mergePartitions,
        string dbaUser,
        string dbaPassword, bool debugImport)
    {
        var impdpTasksByTzVersion = new Dictionary<int, List<Task>>();
        var sourceHostsByTzVersion = GetTimezoneVersions(sourceHosts);

        var destHosts = destinationHosts as IHost[] ?? destinationHosts.ToArray();
        foreach (var host in destHosts)
        {
            var adminHost = CreateAdminHost(host, dbaUser, dbaPassword);
            if (debugImport) DebugImpdpJobs(adminHost);
            var category = host.Configuration.HostCategory;
            var lhTables = tables.Where(t => t.HostCategory == category).ToList();
            if (!lhTables.Any())
            {
                Log.Warning("No tables were found for host: {Category}, skipping reset schema", category);
                continue;
            }

            var allLinks =
                sourceHosts.Where(lh =>
                    lh.Configuration.HostCategory == category && lh.Configuration.CanDbLink);

            foreach (var source in allLinks)
            {
                var sourceConfig = source.Configuration;
                var sourceTzVersion = sourceHostsByTzVersion[source];
                var lhConfig = host.Configuration;
                
                //
                // Create database link ahead of time to avoid race condition
                //
                var linkName = adminHost.CreateDbLink(sourceConfig);
                
                var task = new Task(() =>
                {
                    //
                    // Create User if needed
                    //
                    CreateUser(adminHost, lhConfig);

                    //
                    // Execute Remap Schema Impdp Job
                    //
                    var impdpParams = SqlLoaderUtils.CreateRemapSchemaParameters(adminHost.Configuration,
                        lhTables, sourceConfig,
                        mergePartitions,
                        debugImport, linkName, keepPrimaryKeys);
                    _oracleCommandRunner.RunCommand(impdpParams, adminHost.Configuration, _cancellationTokenSource.Token)
                        .Wait();
                });
                if (impdpTasksByTzVersion.TryGetValue(sourceTzVersion, out var value))
                {
                    value.Add(task);
                }
                else
                {
                    var tzTaskList = new List<Task> { task };
                    impdpTasksByTzVersion[sourceTzVersion] = tzTaskList;
                }
            }
        }

        foreach (var (version, tasks) in impdpTasksByTzVersion)
        {
            Log.Information("Setting up destination schemas with TZ version {TzVersion}", version);
            foreach (var task in tasks)
            {
                task.Start();
                Thread.Sleep(2000);
            }

            Task.WaitAll(tasks.ToArray());
        }
        
        DestroyDbLinks(sourceHosts, destHosts, dbaUser, dbaPassword);
    }

    private static void GrantTableCreation(IList<IHost> configDestinationHosts, string dbaUser,
        string dbaPassword)
    {
        Log.Information("Granting TABLE CREATE permissions for destination host users");
        foreach (var host in configDestinationHosts)
        {
            var adminHost = CreateAdminHost(host, dbaUser, dbaPassword);
            adminHost.ExecuteNonQuery($"GRANT CREATE TABLE TO {host.Configuration.User}");
        }

        Log.Information("Finished granting TABLE CREATE permissions for destination host users");
    }

    private static void CreateUser(IHost adminHost, HostConfig lhConfig)
    {
        using var countReader =
            adminHost.ExecuteQuery(
                $"SELECT count(*) FROM dba_users WHERE username = '{lhConfig.Schema}'");
        countReader.Read();
        var count = countReader.GetDecimal(0);
        switch (count)
        {
            case 0:
            {
                using var tableSpaceReader =
                    adminHost.ExecuteQuery(
                        $"SELECT default_tablespace FROM dba_users WHERE username = USER");
                tableSpaceReader.Read();
                var defaultTableSpace = tableSpaceReader.GetString(0);
                adminHost.ExecuteNonQuery(
                    $"CREATE USER {lhConfig.Schema} IDENTIFIED BY {lhConfig.Password} DEFAULT TABLESPACE {defaultTableSpace}");
                adminHost.ExecuteNonQuery(
                    $"ALTER USER {lhConfig.Schema} QUOTA UNLIMITED ON {defaultTableSpace}");
                adminHost.ExecuteNonQuery(
                    $"GRANT CREATE SESSION, RESOURCE, IMP_FULL_DATABASE, EXP_FULL_DATABASE TO {lhConfig.Schema}");
                break;
            }
            case > 1:
                throw new InvalidOperationException(
                    $"Detected {count} users with the name {lhConfig.Schema}! Impossible!");
        }
    }

    private static void DebugImpdpJobs(IHost adminHost)
    {
        var jobs = new List<DataPumpJob>();
        using var reader = adminHost.ExecuteQuery(
            "SELECT OWNER_NAME, JOB_NAME, OPERATION, JOB_MODE, STATE, ATTACHED_SESSIONS FROM DBA_DATAPUMP_JOBS  WHERE job_name NOT LIKE 'BIN$%'");

        while (reader.Read())
        {
            var job = DataPumpJob.CreateFromReader(reader);
            jobs.Add(job);
        }

        var groupedJobs = jobs
            .GroupBy(j => j.State, (state, jobsGrp) => (state, jobsGrp.ToArray()))
            .ToDictionary(tuple => tuple.Item1, grp => grp.Item2);

        foreach (var (state, stateJobs) in groupedJobs)
        {
            Log.Information("Detected {Count} jobs in {State}:\n\t{Out}", stateJobs.Length, state,
                string.Join("\t", stateJobs.Select(j => j.ToString())));
        }
    }

    private static IHost CreateAdminHost(IHost standardDestination, string dbaUser,
        string dbaPassword) => standardDestination.CreateAdminHost(dbaUser, dbaPassword);


    private static List<string> CreateDropCommands(IHost host)
    {
        var dropCommands = new List<string>();

        var allTablesQuery = $@"SELECT owner, table_name
                           FROM all_tables
                           WHERE owner = '{host.Configuration.Schema}'";
        using (var reader = host.ExecuteQuery(allTablesQuery))
        {
            while (reader.Read())
            {
                var owner = reader.GetString(0).ToUpperInvariant();
                var tableName = reader.GetString(1).ToUpperInvariant();
                var isTemp = tableName.StartsWith("TMP") || tableName.StartsWith("tmp");
                var tableIdentifier = isTemp ? tableName : $"{owner}.{tableName}";
                if (isTemp) Log.Logger.Warning("Found tmp table {Table}", tableIdentifier);
                dropCommands.Add($"DROP TABLE {tableIdentifier} CASCADE CONSTRAINTS PURGE");
            }
        }

        var allObjectsQuery = $@"SELECT object_type, owner, object_name
                           FROM all_objects
                           WHERE object_type NOT IN ('TABLE','INDEX','PACKAGE BODY','TRIGGER','LOB')
                                 AND object_type NOT LIKE '%LINK%'
                                 AND object_type NOT LIKE '%PARTITION%'
                                 AND owner = '{host.Configuration.Schema}'
                           ORDER BY object_type";
        using (var reader = host.ExecuteQuery(allObjectsQuery))
        {
            while (reader.Read())
            {
                var objectType = reader.GetString(0).ToUpperInvariant();
                var owner = reader.GetString(1).ToUpperInvariant();
                var objectName = reader.GetString(2).ToUpperInvariant();
                dropCommands.Add($"DROP {objectType} {owner}.{objectName}");
            }
        }

        return dropCommands;
    }

    private static ImmutableDictionary<IHost, int> GetTimezoneVersions(IEnumerable<IHost> sourceHosts)
    {
        var tzDict = new ConcurrentDictionary<IHost, int>();
        Parallel.ForEach(sourceHosts,
            (sourceHost) => { tzDict.TryAdd(sourceHost, OracleUtilities.GetTimezoneVersion(sourceHost)); });

        return tzDict.ToImmutableDictionary();
    }
}