using Nett;
using Serilog;

namespace Tonic.Common.Configs;

public class ReuseRowsDbConfig
{
    public readonly string Host;
    public readonly uint Port;
    public readonly string Database;
    public readonly string User;
    public readonly string Password;
    public readonly string Table;
    public readonly int? MigrationTimeout = null;
    public bool DebugQueries { get; } = true;


    public ReuseRowsDbConfig()
    {
        Host = "";
        Database = "";
        User = "";
        Password = "";
        Table = "";
    }

    public ReuseRowsDbConfig(TomlTable? tomlConfig, string key = "Database")
    {
        if (tomlConfig == null)
        {
            (Host, Port, Database, User, Password, Table) = DevConfig();
            return;
        }

        var dbConfig = tomlConfig.ContainsKey(key)
            ? tomlConfig.Get<TomlTable>(key)
            : null;

        if (dbConfig == null)
        {
            (Host, Port, Database, User, Password, Table) = DevConfig();
            return;
        }

        var host = dbConfig.ContainsKey("Host") ? dbConfig.Get<string>("Host") : null;
        var port = dbConfig.ContainsKey("Port") ? dbConfig.Get<uint>("Port") : (uint?) null;
        var database = dbConfig.ContainsKey("Database") ? dbConfig.Get<string>("Database") : null;
        var user = dbConfig.ContainsKey("User") ? dbConfig.Get<string>("User") : null;
        var password = dbConfig.ContainsKey("Password") ? dbConfig.Get<string>("Password") : null;
        var table = dbConfig.ContainsKey("Table") ? dbConfig.Get<string>("Table") : null;
        var migrationTimeout = dbConfig.ContainsKey("MigrationTimeout") ? dbConfig.Get<int>("MigrationTimeout") : (int?)null;

        if (string.IsNullOrWhiteSpace(host)
            || port == null
            || string.IsNullOrWhiteSpace(database)
            || string.IsNullOrWhiteSpace(user)
            || password == null
            || table == null)
        {
            (Host, Port, Database, User, Password, Table) = DevConfig();
            return;
        }

        Host = host;
        Port = port.Value;
        Database = database;
        User = user;
        Password = password;
        Table = table;
        MigrationTimeout = migrationTimeout;
        DebugQueries = dbConfig.ContainsKey("DebugQueries") && dbConfig.Get<bool>("DebugQueries");
    }

    private static (string, uint, string, string, string, string) DevConfig()
    {
        Log.Information("Missing or incomplete configuration of MySql database, using Dev localhost");
        return (
            "127.0.0.1",
            3306,
            "tonicmerger",
            "root",
            "password",
            "ordered_row_keys"
        );
    }
}