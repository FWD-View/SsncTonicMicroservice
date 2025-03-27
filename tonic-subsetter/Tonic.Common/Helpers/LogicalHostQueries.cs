//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Data;
//using System.Data.Common;
//using System.Linq;
//using Oracle.ManagedDataAccess.Client;
//using Serilog;
//using Tonic.Common.OracleHelper;

//namespace Tonic.Common.Helpers;

//public partial class Host
//{
//    private OracleCommand CreateParameterizedOracleCommand(IDictionary<string, string?> paramsDict,
//        OracleConnection connection,
//        string replacedQuery)
//    {
//        var command = connection.CreateCommand();
//        if (paramsDict.Any())
//            command.Parameters.AddRange(paramsDict.Select(kvp =>
//                {
//                    if (kvp.Value == null)
//                    {
//                        var param = new OracleParameter(kvp.Key, OracleDbType.Date);
//                        param.Value = DBNull.Value;
//                        return param;
//                    }
//                    else
//                    {
//                        return new OracleParameter(kvp.Key, kvp.Value);
//                    }
//                })
//                .ToArray());
//        command.SetCommandText(replacedQuery, this);
//        return command;
//    }

//    public (string, string?) ReplaceSchemaTokens(string query, ImmutableArray<SchemaOverride>? schemaOverrides = null)
//    {
//        query = query.Replace(Constants.TonicSchemaToken, Configuration.Schema);
//        var schemaOverride = SchemaOverride.OverrideForHost(this, schemaOverrides ?? SchemaOverrides);
//        if (schemaOverride == null) return (SchemaOverride.ReplaceToken(query, Configuration.Schema), null);
//        var overriddenTables = SchemaOverride.ParseOverrideTokens(query);
//        foreach (var (token, _) in overriddenTables)
//        {
//            query = query.Replace(token, schemaOverride.Schema);
//        }

//        return (query, schemaOverride.Sid);
//    }

//    public void ExecuteNonQuery(string query)
//    {
//        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);
//        using var connection = CreateOpenConnection(sid);
//        using var command = connection.CreateCommand();
//        command.SetCommandText(replacedQuery, this);
//        try
//        {
//            command.ExecuteNonQuery();
//        }
//        catch (OracleException)
//        {
//            Log.Error(
//                "Could not run non query command as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid} {Query}",
//                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port, Configuration.Sid,
//                command.CommandText);
//            throw;
//        }
//    }


//    public void ExecuteNonQueryWithConnection(string query, OracleConnection connection)
//    {
//        using var command = connection.CreateCommand();
//        command.SetCommandText(query, this);
//        try
//        {
//            command.ExecuteNonQuery();
//        }
//        catch (OracleException)
//        {
//            Log.Error(
//                "Could not run non query command as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid} {Query}",
//                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port, Configuration.Sid,
//                command.CommandText);
//            throw;
//        }
//    }

//    public void ExecuteParameterizedNonQuery(string query, IDictionary<string, string> paramsDict)
//    {
//        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);

//        var connection = CreateOpenConnection(sid);
//        using var command = CreateParameterizedOracleCommand(paramsDict, connection, replacedQuery);
//        try
//        {
//            command.ExecuteNonQuery();
//        }
//        catch (OracleException)
//        {
//            var paramsStr = paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]";
//            Log.Fatal(
//                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
//                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
//                Configuration.Sid, command.CommandText, paramsStr);
//            throw;
//        }
//    }

//    public DbDataReader ExecuteQuery(string query)
//    {
//        return ExecuteFinalizedQuery(query, new Dictionary<string, string>());
//    }

//    public DbDataReader ExecuteParameterizedQuery(string query, Dictionary<string, string> paramsDict)
//    {
//        return ExecuteFinalizedQuery(query, paramsDict);
//    }


//    public DbDataReader ExecuteQueryWithConnection(string query, OracleConnection connection)
//    {
//        return ExecuteParameterizedQueryWithConnection(query, new Dictionary<string, string>(), connection);
//    }

//    private DbDataReader ExecuteParameterizedQueryWithConnection(string query, Dictionary<string, string> paramsDict,
//        OracleConnection connection)
//    {
//        var (replacedQuery, _) = ReplaceSchemaTokens(query, SchemaOverrides);

//        using var command = CreateParameterizedOracleCommand(paramsDict, connection, replacedQuery);
//        try
//        {
//            return command.ExecuteReader(CommandBehavior.Default);
//        }
//        catch (OracleException)
//        {
//            Log.Fatal(
//                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
//                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
//                Configuration.Sid, command.CommandText,
//                paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]");
//            throw;
//        }
//    }

//    private DbDataReader ExecuteFinalizedQuery(string query, Dictionary<string, string> paramsDict)
//    {
//        var (replacedQuery, sid) = ReplaceSchemaTokens(query, SchemaOverrides);

//        var connection = CreateOpenConnection(sid); // closed by reader
//        using var command = CreateParameterizedOracleCommand(paramsDict, connection, replacedQuery);
//        try
//        {
//            return command.ExecuteReader(CommandBehavior.CloseConnection);
//        }
//        catch (OracleException)
//        {
//            Log.Fatal(
//                "Could not run non query as User: {User} Schema: {Schema} Host: {Host}:{Port}/{Sid}\n\t{Query}\nParams:\n\t{Params}",
//                Configuration.User, Configuration.Schema, Configuration.Host, Configuration.Port,
//                Configuration.Sid, command.CommandText,
//                paramsDict.Any() ? $"[{string.Join(",", paramsDict.Values)}]" : "[]");
//            throw;
//        }
//    }
//}