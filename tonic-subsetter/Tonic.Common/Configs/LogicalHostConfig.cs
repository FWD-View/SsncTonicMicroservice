using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Oracle.ManagedDataAccess.Client;
using IBM.Data.Db2;
namespace Tonic.Common.Configs;

public class HostConfig
{
    public string HostCategory { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = -1;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Sid { get; init; } = string.Empty;
    public string Schema { get; init; } = string.Empty;
    public bool CanDbLink { get; init; }
    public int ShardedIndex { get; init; } = -1;
    public string DbType { get; init; } = string.Empty;



    public HostConfig WithUserAndPassword(string user, string password) =>
        new()
        {
            HostCategory = HostCategory,
            Host = Host,
            Port = Port,
            User = user,
            Password = password,
            Sid = Sid,
            Schema = Schema,
            CanDbLink = CanDbLink,
            DbType= DbType
        };

    public HostConfig AsTempUpsertConfig(string tempUpsertSchemaName) =>
        new()
        {
            HostCategory = HostCategory,
            Host = Host,
            Port = Port,
            User = User,
            Password = Password,
            Sid = Sid,
            Schema = tempUpsertSchemaName,
            CanDbLink = CanDbLink,
            DbType= DbType
        };

    public ImmutableDictionary<int, HostConfig> ShardedDestinations { get; set; } =
        ImmutableDictionary<int, HostConfig>.Empty;

    public HostConfig GetShardedDestination(int destinationIndex)
    {
        if (ShardedIndex == destinationIndex)
            return this;
        if (!ShardedDestinations.ContainsKey(destinationIndex))
            throw new InvalidDataException(
                $"Invalid sharded destination: No host specified for index {destinationIndex}");
        return ShardedDestinations[destinationIndex];
    }

    public IEnumerable<(int, HostConfig)> GetAllHostConfigs()
    {
        yield return (ShardedIndex, this);
        foreach (var (index, config) in ShardedDestinations)
        {
            yield return (index, config);
        }
    }

    public OracleConnection CreateConnection(string? sid = null)
    {
        var connectionStringBuilder = new OracleConnectionStringBuilder
        {
            DataSource =
                $"(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={Host})(PORT={Port})))(CONNECT_DATA=(SERVICE_NAME={sid ?? Sid})))",
            UserID = User,
            Password = Password,
            ConnectionTimeout = 120, // increase time to wait for a new connection
            ConnectionLifeTime = 120, // prevent connections from running out of time against prod
        };
        var oracleConnection = new OracleConnection(connectionStringBuilder.ToString());
            oracleConnection.KeepAlive = true;
        return oracleConnection;
    }

    public DB2Connection CreateDB2Connection(string? sid = null)
    {

        var connStringBld = new DB2ConnectionStringBuilder()
        {
            Database = Sid,
            UserID = User,
            Password = Password,
            Server = $"{Host}:{Port}",
            HostVarParameters= true
        };
        Console.WriteLine(connStringBld.ToString());

        DB2Connection MyDb2Connection = new DB2Connection(connStringBld.ToString());
        return MyDb2Connection;
    }
    public string CreateLinkName() => Sid.Split('_')[0];

    public override string ToString() => $"{User}@{Host}:{Port}/{Sid}";
}
