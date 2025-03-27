using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;

namespace Tonic.Common.Helpers;

public static class TableInfo
{
    public static ImmutableHashSet<Table> CollectIoTTables(IEnumerable<IHost> connections)
    {
        var allIotTables = new ConcurrentBag<Table>();
        Parallel.ForEach(connections, host =>
        {
            var iotTables = OracleUtilities.GetIoTTables(host);
            foreach (var table in iotTables) allIotTables.Add(table);
        });

        return allIotTables.ToImmutableHashSet();
    }

    public static IDictionary<Table, IList<Column>> CollectColumns(ImmutableArray<IHost> sourceHosts)
    {
        var allColumns = new ConcurrentBag<List<Column>>();

        Parallel.ForEach(sourceHosts, lh => { allColumns.Add(OracleUtilities.GetColumns(lh)); });

        return allColumns.SelectMany(columns => columns).GroupBy(c => new Table(c.HostCategoryName, c.TableName))
            .ToImmutableDictionary<IGrouping<Table, Column>, Table, IList<Column>>(group => group.Key,
                ImmutableList.CreateRange);
    }
    
    /// <summary>
    /// This function creates the ReuseRowsTable set from the Primary Keys and Unique Index of Tables in the database.
    /// The configuration will provide some ReuseRowsTables as well, when the database is ambiguous, as well as
    /// NoReuseRowsTables when the table is opting out.
    /// </summary>
    /// <param name="tables">List of source tables for PK remapper</param>
    /// <param name="destinationHosts">List of hosts in destination</param>
    /// <param name="reuseRowTablesAugmentation">List of reuseRowsTables from config</param>
    /// <param name="noReuseRowsTables">List of tables that aren't reused</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static HashSet<TableWithRowKeys> CalculateReuseRowsTables(ImmutableArray<Table> tables,
        IEnumerable<IHost> destinationHosts, IEnumerable<TableWithRowKeys> reuseRowTablesAugmentation,
        IReadOnlyCollection<TableWithRowKeys> noReuseRowsTables)
    {
        var noReuseRowsTablesArray = noReuseRowsTables.Select(x => x.Table).ToArray();
        /*
         * Gets a list of tables with their row keys which id the row.
         * The row keys are identifiers that are entered in the config for when there's no primary key and no single unique value
         * (e.g. if you have more than one unique value then you need to set the row key in the config)
         */
        var tableToRowKeys =
            new ConcurrentDictionary<Table, IList<string>>(
                reuseRowTablesAugmentation.ToDictionary(rrt => rrt.Table, rrt => rrt.RowKeys));
        // List of all tables
        var tablesStr = string.Join(", ", tables.Select(t => $"'{t.TableName}'"));
        
        Parallel.ForEach(destinationHosts, lh =>
        {
            // Gets list of all primary keys
            var pkQuery = @$"SELECT cols.table_name, cols.column_name
                    FROM all_constraints cons, all_cons_columns cols
                    WHERE cols.table_name in ({tablesStr})
                    AND cons.owner = cols.owner
                    AND cons.constraint_type = 'P'
                    AND cons.constraint_name = cols.constraint_name
                    AND cons.owner = '{lh.Configuration.Schema}'";
            Log.Information("Query: {Query}", pkQuery);
            using (var reader = lh.ExecuteQuery(pkQuery))
            {
                var allPrimaryKeys = new List<(string Table, string Column)>();
                while (reader.Read())
                {
                    var tableName = reader.GetString(0);
                    var columnName = reader.GetString(1);
                    allPrimaryKeys.Add((tableName, columnName));
                }
                
                // Group PKs by table and then add them
                foreach (var group in allPrimaryKeys.GroupBy(elem => elem.Table))
                {
                    // Gets table with primary key from grouping and then gets list of primary keys
                    var tableWithPk = new Table(lh.Configuration.HostCategory, group.Key);
                    var primaryKeys = group.Select(e => e.Column).ToImmutableArray();
                    
                    // If the PK isn't already in the list of row keys then we add it
                    if (!tableToRowKeys.ContainsKey(tableWithPk))
                    {
                        tableToRowKeys.TryAdd(tableWithPk, primaryKeys);
                    }
                }
            }
            
            /*
             * BACKUP PLAN, ADD UNIQUE INDEXES WHEN THERE IS JUST ONE
             */

            // This query gets all the columns for tables with exactly one unique index
            var uniqueQuery =
                $@" SELECT TABLE_NAME, COLUMN_NAME
                        FROM ALL_IND_COLUMNS
                        WHERE (INDEX_NAME, 1) in (
	                        SELECT INDEX_NAME, count(*) OVER (PARTITION BY TABLE_NAME)
	                        FROM ALL_INDEXES
	                        WHERE TABLE_OWNER = '{lh.Configuration.Schema}'
	                          AND UNIQUENESS = 'UNIQUE'
	                          AND TABLE_NAME IN ({tablesStr}))
                          AND TABLE_OWNER = '{lh.Configuration.Schema}'";
            using var indexReader = lh.ExecuteQuery(uniqueQuery);
            var uniqueIndexColumns = new List<(string Table, string Column)>();
            while (indexReader.Read())
            {
                var tableName = indexReader.GetString(0);
                var columnName = indexReader.GetString(1);
                uniqueIndexColumns.Add((tableName, columnName));
            }
            
            // Similar to PKs we group by table and then add it if it's not already in the list of row keys
            foreach (var group in uniqueIndexColumns.GroupBy(elem => elem.Table))
            {
                var tableWithUniqueIndex = new Table(lh.Configuration.HostCategory, group.Key);
                var uniqueIndex = group.Select(e => e.Column).ToImmutableArray();

                if (!tableToRowKeys.ContainsKey(tableWithUniqueIndex))
                {
                    tableToRowKeys.TryAdd(tableWithUniqueIndex, uniqueIndex);
                }
            }
        });

        // Filters out table row keys where it's not in noReuseRowsTables and then outputs list of reuseRowsTables
        var reuseRowsTables = tableToRowKeys
            .Where(kvp => !noReuseRowsTablesArray.Contains(kvp.Key))
            .Select(kvp => new TableWithRowKeys(kvp.Key, kvp.Value)).ToHashSet();
        if (reuseRowsTables.Any(rrt => !rrt.RowKeys.Any()))
        {
            throw new InvalidOperationException(
                "After CalculateReuseRowsFromPrimaryKeys there should be no ReuseRowsTable missing row keys.");
        }
        
        // Gets a list of tables without reuse rows set up
        var reuseRowsLookup = reuseRowsTables.ToDictionary(r => r.Table);
        var allAccountedForTables = reuseRowsLookup.Keys.Union(noReuseRowsTablesArray).ToHashSet();
        var unaccountedForTables = tables.RemoveRange(allAccountedForTables);
        if (unaccountedForTables.Any())
        {
            throw new InvalidOperationException(
                $"Every table must be accounted for with reuse rows, and these tables are not: {string.Join(", ", unaccountedForTables)}");
        }

        // Returns hashset of reuserows tables
        return reuseRowsLookup
            .Where(kv => !noReuseRowsTablesArray.Contains(kv.Key))
            .Select(kv => reuseRowsLookup[kv.Key])
            .ToHashSet();
    }

    public static IList<Column> FetchTableColumns(string table, IHost redactedLookupDestinationHost)
    {
        var config = redactedLookupDestinationHost.Configuration;
        using var reader = redactedLookupDestinationHost.ExecuteQuery($@"
                SELECT
                    cols.TABLE_NAME AS ""table_name"",
                    cols.COLUMN_NAME AS ""column_name"",
                    cols.DATA_TYPE AS ""data_type_of_column"",
                    CASE cols.NULLABLE WHEN 'Y' THEN 'YES' ELSE 'NO' END AS ""is_column_nullable""
                FROM all_tab_cols cols
                WHERE cols.TABLE_NAME = '{table}'
                  AND HIDDEN_COLUMN = 'NO'
                  AND OWNER = '{config.Schema}'
                ORDER BY cols.COLUMN_ID");

        var columns = new List<Column>();
        while (reader.Read())
        {
            var lhc = config.HostCategory;
            var tableName = reader.GetString(0);
            var columnName = reader.GetString(1);
            var dataType = reader.GetString(2);
            var isNullable = reader.GetString(3) == "YES";
            var newColumn = new Column(lhc, tableName, columnName, dataType, isNullable);
            columns.Add(newColumn);
        }

        return columns.ToImmutableArray();
    }

    public static IEnumerable<Table> CollectSourceTables(IEnumerable<IHost> connections)
    {
        var tables = new ConcurrentBag<Table>();

        Parallel.ForEach(connections, host =>
        {
            using var reader = host.ExecuteQuery(
                $"SELECT table_name FROM all_tables WHERE owner = '{host.Configuration.Schema}' AND table_name NOT LIKE 'SYS_IOT_OVER_%'");

            while (reader.Read())
            {
                tables.Add(new Table(host.Configuration.HostCategory, reader.GetString(0)));
            }
        });

        return tables.ToImmutableArray();
    }
}