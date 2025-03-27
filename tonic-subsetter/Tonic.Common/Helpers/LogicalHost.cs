using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq;
using IBM.Data.Db2;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using Tonic.Common.Configs;
using Tonic.Common.Enums;
using Tonic.Common.OracleHelper;

namespace Tonic.Common.Helpers;

public interface IHost
{
    string Name { get; }
    HostConfig Configuration { get; }
    public IEnumerable<(int, IHost)> GetAllShardedHosts();
    IHost CreateAdminHost(string dbaUser, string dbaPassword, HostConfig? config = null);
    string CreateDbLink(HostConfig other);
    //OracleConnection CreateOpenConnection(string? sid = null);
    //DB2Connection CreateDB2OpenConnection(string? sid = null);

    void DropDbLink(string linkName);
    IHost GetShardedDestination(int destinationIndex);
    (string, string?) ReplaceSchemaTokens(string query, ImmutableArray<SchemaOverride>? schemaOverrides = null);
    // queries
    void ExecuteNonQuery(string query);
    void ExecuteNonQueryWithConnection(string query, IDbConnection connection);
    DbDataReader ExecuteQuery(string query);

    DbDataReader ExecuteQueryWithConnection(string query, IDbConnection connection);
    DbDataReader ExecuteParameterizedQuery(string query, Dictionary<string, string> paramsDict);
}

public partial class Host : IHost
{
    public Host()
    {
        Configuration = new HostConfig();
        Name = "";
        SchemaOverrides = ImmutableArray<SchemaOverride>.Empty;
    }

    public Host(string name, HostConfig config,
        ImmutableArray<SchemaOverride>? schemaOverrides = null)
    {
        Name = name;
        Configuration = config;
        SchemaOverrides = schemaOverrides ?? ImmutableArray<SchemaOverride>.Empty;
    }

    public ImmutableArray<SchemaOverride> SchemaOverrides { get; }
    public string Name { get; }
    public HostConfig Configuration { get; }

    public IHost CreateAdminHost(string dbaUser, string dbaPassword, HostConfig? config = null) =>
        new Host(Name + "_admin", (config ?? Configuration).WithUserAndPassword(dbaUser, dbaPassword),
            ImmutableArray<SchemaOverride>.Empty);

    private string? GetDatabaseLink(string linkName)
    {
        using var reader = ExecuteQuery($"SELECT HOST FROM DBA_DB_LINKS WHERE DB_LINK='{linkName}'");
        if (reader.Read() && reader.HasRows) return reader.GetString(0);
        return null;
    }

    public string CreateDbLink(HostConfig other)
    {
        var linkName = other.CreateLinkName();
        Log.Information("Creating DB Link on Host {Src} to Host {Dest}", ToString(), other.ToString());
        var testMode = TonicEnvironmentVariable.TONIC_TEST_MODE.Get<bool>();
        // If we are in test mode and the host is localhost, then use host.docker.internal
        var host = testMode && other.Host == "localhost" ? "host.docker.internal" : other.Host;
        var hostInfo =
            $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={other.Port}))(CONNECT_DATA=(SERVICE_NAME={other.Sid})))";
        var dblinkInfo = GetDatabaseLink(linkName);

        if (string.Equals(dblinkInfo, hostInfo, StringComparison.InvariantCultureIgnoreCase)) return linkName;
        if (dblinkInfo != null)
            throw new InvalidOperationException(
                $"Database Link {linkName} already exists, but has a conflicting configuration{Environment.NewLine}" +
                $"{linkName} config: {dblinkInfo}{Environment.NewLine}" +
                $"Expected config: {hostInfo}");

        ExecuteNonQuery(
            $"CREATE DATABASE LINK {linkName} CONNECT TO {other.User} IDENTIFIED BY {other.Password} " +
            $"USING '{hostInfo}'"
        );
        return linkName;
    }

    public void DropDbLink(string linkName)
    {
        Log.Debug("Dropping DB Link {Name}", linkName);
        ExecuteNonQuery($"DROP DATABASE LINK {linkName}");
    }

    public IHost GetShardedDestination(int destinationIndex)
    {
        var destConfig = Configuration.GetShardedDestination(destinationIndex);
        return new Host(Name, destConfig);
    }

    public OracleConnection CreateOpenConnection(string? sid = null)
    {
        OracleConnection connection;
        try
        {
            connection = Configuration.CreateConnection(sid);
            connection.Open();
        }
        catch (OracleException)
        {
            Log.Fatal(
                "Cannot open connection to database. User: {User} Host: {Host}:{Port}/{Sid}",
                Configuration.User, Configuration.Host, Configuration.Port, Configuration.Sid);
            throw;
        }

        return connection;
    }

    public IEnumerable<(int, IHost)> GetAllShardedHosts() => Configuration
        .GetAllHostConfigs()
        .Select<(int, HostConfig), (int, IHost)>(pair =>
            (pair.Item1, new Host(Name, pair.Item2, SchemaOverrides)));

    public override string ToString() => $"{Name}: {Configuration.Host}/{Configuration.Sid}/{Configuration.Schema}";

    public (string, string?) ReplaceSchemaTokens(string query, ImmutableArray<SchemaOverride>? schemaOverrides = null)
    {
        query = query.Replace(Constants.TonicSchemaToken, Configuration.Schema);
        var schemaOverride = SchemaOverride.OverrideForHost(this, schemaOverrides ?? SchemaOverrides);
        if (schemaOverride == null) return (SchemaOverride.ReplaceToken(query, Configuration.Schema), null);
        var overriddenTables = SchemaOverride.ParseOverrideTokens(query);
        foreach (var (token, _) in overriddenTables)
        {
            query = query.Replace(token, schemaOverride.Schema);
        }

        return (query, schemaOverride.Sid);
    }

    public void ExecuteNonQuery(string query)
    {
        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);
        using var connection = CreateOpenConnection(sid);
        using var command = connection.CreateCommand();
        command.SetCommandText(replacedQuery, this);
        try
        {
            command.ExecuteNonQuery();
        }
        catch (OracleException)
        {
            Log.Error(
                "Could not run non query command as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid} {Query}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port, Configuration.Sid,
                command.CommandText);
            throw;
        }
    }


    public void ExecuteNonQueryWithConnection(string query, IDbConnection oracleConnection)
    {
        OracleConnection connection = oracleConnection as OracleConnection;
        using var command = connection.CreateCommand();
        command.SetCommandText(query, this);
        try
        {
            command.ExecuteNonQuery();
        }
        catch (OracleException)
        {
            Log.Error(
                "Could not run non query command as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid} {Query}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port, Configuration.Sid,
                command.CommandText);
            throw;
        }
    }

    public void ExecuteParameterizedNonQuery(string query, IDictionary<string, string> paramsDict)
    {
        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);

        var connection = CreateOpenConnection(sid);
        using var command = CreateParameterizedOracleCommand(paramsDict, connection, replacedQuery);
        try
        {
            command.ExecuteNonQuery();
        }
        catch (OracleException)
        {
            var paramsStr = paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]";
            Log.Fatal(
                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
                Configuration.Sid, command.CommandText, paramsStr);
            throw;
        }
    }

    public DbDataReader ExecuteQuery(string query)
    {
        return ExecuteFinalizedQuery(query, new Dictionary<string, string>());
    }


    public DbDataReader ExecuteParameterizedQuery(string query, Dictionary<string, string> paramsDict)
    {
        return ExecuteFinalizedQuery(query, paramsDict);
    }



    public DbDataReader ExecuteQueryWithConnection(string query, IDbConnection oracleConnection)
    {
        OracleConnection connection = oracleConnection as OracleConnection;
        return ExecuteParameterizedQueryWithConnection(query, new Dictionary<string, string>(), connection);
    }

    private DbDataReader ExecuteParameterizedQueryWithConnection(string query, Dictionary<string, string> paramsDict,
        OracleConnection connection)
    {
        var (replacedQuery, _) = ReplaceSchemaTokens(query, SchemaOverrides);

        using var command = CreateParameterizedOracleCommand(paramsDict, connection, replacedQuery);
        try
        {
            return command.ExecuteReader(CommandBehavior.Default);
        }
        catch (OracleException)
        {
            Log.Fatal(
                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
                Configuration.Sid, command.CommandText,
                paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]");
            throw;
        }
    }
    private DbDataReader ExecuteFinalizedQuery(string query, Dictionary<string, string> paramsDict)
    {
        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);

        var connection = CreateOpenConnection(sid); // closed by reader
        using var command = CreateParameterizedOracleCommand(paramsDict, connection, replacedQuery);
        try
        {
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }
        catch (OracleException)
        {
            Log.Fatal(
                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
                Configuration.Sid, command.CommandText,
                paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]");
            throw;
        }
    }

    private OracleCommand CreateParameterizedOracleCommand(IDictionary<string, string?> paramsDict,
        OracleConnection connection,
        string replacedQuery)
    {
        var command = connection.CreateCommand();
        if (paramsDict.Any())
            command.Parameters.AddRange(paramsDict.Select(kvp =>
            {
                if (kvp.Value == null)
                {
                    var param = new OracleParameter(kvp.Key, OracleDbType.Date);
                    param.Value = DBNull.Value;
                    return param;
                }
                else
                {
                    return new OracleParameter(kvp.Key, kvp.Value);
                }
            })
                .ToArray());
        command.SetCommandText(replacedQuery, this);
        return command;
    }
}

public partial class DB2Host : IHost
{
    public DB2Host()
    {
        Configuration = new HostConfig();
        Name = "";
        SchemaOverrides = ImmutableArray<SchemaOverride>.Empty;
    }

    public DB2Host(string name, HostConfig config,
        ImmutableArray<SchemaOverride>? schemaOverrides = null)
    {
        Name = name;
        Configuration = config;
        SchemaOverrides = schemaOverrides ?? ImmutableArray<SchemaOverride>.Empty;
    }

    public ImmutableArray<SchemaOverride> SchemaOverrides { get; }
    public string Name { get; }
    public HostConfig Configuration { get; }

    public IHost CreateAdminHost(string dbaUser, string dbaPassword, HostConfig? config = null) =>
        new Host(Name + "_admin", (config ?? Configuration).WithUserAndPassword(dbaUser, dbaPassword),
            ImmutableArray<SchemaOverride>.Empty);

    private string? GetDatabaseLink(string linkName)
    {
        using var reader = ExecuteQuery($"SELECT HOST FROM DBA_DB_LINKS WHERE DB_LINK='{linkName}'");
        if (reader.Read() && reader.HasRows) return reader.GetString(0);
        return null;
    }

    public string CreateDbLink(HostConfig other)
    {
        var linkName = other.CreateLinkName();
        Log.Information("Creating DB Link on Host {Src} to Host {Dest}", ToString(), other.ToString());
        var testMode = TonicEnvironmentVariable.TONIC_TEST_MODE.Get<bool>();
        // If we are in test mode and the host is localhost, then use host.docker.internal
        var host = testMode && other.Host == "localhost" ? "host.docker.internal" : other.Host;
        var hostInfo =
            $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={other.Port}))(CONNECT_DATA=(SERVICE_NAME={other.Sid})))";
        var dblinkInfo = GetDatabaseLink(linkName);

        if (string.Equals(dblinkInfo, hostInfo, StringComparison.InvariantCultureIgnoreCase)) return linkName;
        if (dblinkInfo != null)
            throw new InvalidOperationException(
                $"Database Link {linkName} already exists, but has a conflicting configuration{Environment.NewLine}" +
                $"{linkName} config: {dblinkInfo}{Environment.NewLine}" +
                $"Expected config: {hostInfo}");

        ExecuteNonQuery(
            $"CREATE DATABASE LINK {linkName} CONNECT TO {other.User} IDENTIFIED BY {other.Password} " +
            $"USING '{hostInfo}'"
        );
        return linkName;
    }

    public void DropDbLink(string linkName)
    {
        Log.Debug("Dropping DB Link {Name}", linkName);
        ExecuteNonQuery($"DROP DATABASE LINK {linkName}");
    }

    public IHost GetShardedDestination(int destinationIndex)
    {
        var destConfig = Configuration.GetShardedDestination(destinationIndex);
        return new Host(Name, destConfig);
    }



    public DB2Connection CreateOpenConnection(string? sid = null)
    {
        DB2Connection connection;
        try
        {
            connection = Configuration.CreateDB2Connection(sid);
            connection.Open();
        }
        catch (DB2Exception ex)
        {
            Log.Fatal(
                "Cannot open connection to database. User: {User} Host: {Host}:{Port}/{Sid}",ex,
                Configuration.User, Configuration.Host, Configuration.Port, Configuration.Sid);
            throw;
        }

        return connection;
    }

    public IEnumerable<(int, IHost)> GetAllShardedHosts() => Configuration
        .GetAllHostConfigs()
        .Select<(int, HostConfig), (int, IHost)>(pair =>
            (pair.Item1, new Host(Name, pair.Item2, SchemaOverrides)));

    public override string ToString() => $"{Name}: {Configuration.Host}/{Configuration.Sid}/{Configuration.Schema}";

    private DB2Command CreateParameterizedDB2Command(IDictionary<string, string?> paramsDict,
        DB2Connection connection,
        string replacedQuery)
    {
        var command = connection.CreateCommand();
        if (paramsDict.Any())
            command.Parameters.AddRange(paramsDict.Select(kvp =>
            {
                if (kvp.Value == null)
                {
                    var param = new DB2Parameter(kvp.Key, DB2Type.Date);
                    param.Value = DBNull.Value;
                    return param;
                }
                else
                {
                    return new DB2Parameter(kvp.Key, kvp.Value);
                }
            })
                .ToArray());
        command.CommandText = replacedQuery;
        return command;
    }

    public (string, string?) ReplaceSchemaTokens(string query, ImmutableArray<SchemaOverride>? schemaOverrides = null)
    {
        query = query.Replace(Constants.TonicSchemaToken, Configuration.Schema);
        var schemaOverride = SchemaOverride.OverrideForHost(this, schemaOverrides ?? SchemaOverrides);
        if (schemaOverride == null) return (SchemaOverride.ReplaceToken(query, Configuration.Schema), null);
        var overriddenTables = SchemaOverride.ParseOverrideTokens(query);
        foreach (var (token, _) in overriddenTables)
        {
            query = query.Replace(token, schemaOverride.Schema);
        }

        return (query, schemaOverride.Sid);
    }


    public void ExecuteNonQuery(string query)
    {
        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);
        using var connection = CreateOpenConnection(sid);
        using var command = connection.CreateCommand();
        command.CommandText = replacedQuery;
        try
        {
            command.ExecuteNonQuery();
        }
        catch (DB2Exception)
        {
            Log.Error(
                "Could not run non query command as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid} {Query}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port, Configuration.Sid,
                command.CommandText);
            throw;
        }
    }


    public void ExecuteNonQueryWithConnection(string query, IDbConnection db2Connection)
    {
        DB2Connection connection = db2Connection as DB2Connection;

        using var command = connection.CreateCommand();
        command.CommandText = query;
        try
        {
            command.ExecuteNonQuery();
        }
        catch (DB2Exception)
        {
            Log.Error(
                "Could not run non query command as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid} {Query}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port, Configuration.Sid,
                command.CommandText);
            throw;
        }
    }

    public void ExecuteParameterizedNonQuery(string query, IDictionary<string, string> paramsDict)
    {
        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);

        var connection = CreateOpenConnection(sid);
        using var command = CreateParameterizedDB2Command(paramsDict, connection, replacedQuery);
        try
        {
            command.ExecuteNonQuery();
        }
        catch (DB2Exception)
        {
            var paramsStr = paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]";
            Log.Fatal(
                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
                Configuration.Sid, command.CommandText, paramsStr);
            throw;
        }
    }

    public DbDataReader ExecuteQuery(string query)
    {
        return ExecuteFinalizedQuery(query, new Dictionary<string, string>());
    }


    public DbDataReader ExecuteParameterizedQuery(string query, Dictionary<string, string> paramsDict)
    {
        return ExecuteFinalizedQuery(query, paramsDict);
    }

    public DbDataReader ExecuteQueryWithConnection(string query, IDbConnection db2Connection)
    {
        DB2Connection connection = db2Connection as DB2Connection;
        return ExecuteParameterizedQueryWithConnection(query, new Dictionary<string, string>(), connection);
    }

    private DbDataReader ExecuteParameterizedQueryWithConnection(string query, Dictionary<string, string> paramsDict,
        DB2Connection connection)
    {
        var (replacedQuery, _) = ReplaceSchemaTokens(query, SchemaOverrides);

        using var command = CreateParameterizedDB2Command(paramsDict, connection, replacedQuery);
        try
        {
            return command.ExecuteReader(CommandBehavior.Default);
        }
        catch (DB2Exception)
        {
            Log.Fatal(
                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
                Configuration.Sid, command.CommandText,
                paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]");
            throw;
        }
    }

    private DbDataReader ExecuteFinalizedQuery(string query, Dictionary<string, string> paramsDict)
    {
        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);

        var connection = CreateOpenConnection(sid); // closed by reader
        using var command = CreateParameterizedDB2Command(paramsDict, connection, replacedQuery);
        try
        {
            var abc= command.ExecuteReader(CommandBehavior.CloseConnection);
            return abc;
        }
        catch (DB2Exception ex)
        {
            Log.Fatal(
                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
                Configuration.Sid, command.CommandText,
                paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]");
            throw;
        }
    }

}
