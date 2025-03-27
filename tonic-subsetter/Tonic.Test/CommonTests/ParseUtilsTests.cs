using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nett;
using Tonic.Common;
using Tonic.Common.Exceptions;
using Tonic.Common.Extensions;
using Tonic.Common.Helpers;
using Tonic.Common.Models;
using Tonic.Common.Utils;
using Xunit;

namespace Tonic.Test.CommonTests;

public class ParseUtilsTests
{
    [Fact]
    public void TestCompressedColumnParse()
    {
        var toml = Toml.ReadString(@"[CompressedColumns]
A = [""0""]
B = [""0"", ""1""]
C = [""0"", ""1"", ""2""]");

        var compressedColumns = toml.ParseCompressedColumns();
        for (var i = 0; i < 3; i++)
        {
            var tableName = (Convert.ToChar(i + 65)).ToString().ToUpper();
            for (var j = 0; j <= i; j++)
            {
                var columnName = j.ToString();
                var column = new Column("Test", tableName, columnName, "VARCHAR", false);
                Assert.Contains(column.ColumnSelector, compressedColumns);
            }
        }
    }

    [Fact]
    public void TestHostSchemaTableRestrictionsParse()
    {
        var schemaTablePairs = new[]
        {
            ("USER_10", "TABLE_00"),
            ("USER_11", "TABLE_01"),
            ("USER_12", "TABLE_02"),
            ("USER_13", "TABLE_03"),
            ("USER_14", "TABLE_04"),
            ("USER_15", "TABLE_05"),
            ("USER_16", "TABLE_06"),
            ("USER_17", "TABLE_07"),
            ("USER_18", "TABLE_08"),
            ("USER_19", "TABLE_09"),
            ("USER_20", "TABLE_10"),
            ("USER_21", "TABLE_11")
        };

        var config = Toml.ReadString($@"
                [SourceHostSchemaTableRestrictions]
                {string.Join("\n", schemaTablePairs.Select(tup => $"{tup.Item1} = [\"{tup.Item2}\"]"))}");

        var schemaRestrictions = config.ParseSchemaRestrictions();

        foreach (var (schema, table) in schemaTablePairs)
        {
            Assert.True(schemaRestrictions.TryGetValue(table, out var actualSchema));
            Assert.Equal(schema, actualSchema);
        }
    }

    [Fact]
    public void TestHostSchemaOverrideParse()
    {
        var config = Toml.ReadString(@"
[[HostSchemaOverride]]
HostCategory = ""TestHost""
Sid = ""TestSid""
Schema = ""TEST_USER""
Host = ""localhost""
Table = ""TEST_TABLE""");
        var overrides = SchemaOverride.ParseHostSchemaOverrides(config);
        Assert.Single(overrides);
        Assert.Equal("TestHost", overrides.Single().HostCategory);
        Assert.Equal("TestSid", overrides.Single().Sid);
        Assert.Equal("TEST_USER", overrides.Single().Schema);
        Assert.Equal("TEST_TABLE", overrides.Single().TableName);
    }

    [Fact]
    public void TestParseUsersTable()
    {
        var config = Toml.ReadString(@"
[UsersTable]
HostCategory = ""TestHost""
TableName = ""TEST_TABLE""
");
        var actual = config.ParseUsersTable();
        var expected = new Table("TestHost", "TEST_TABLE");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestParseDuplicateImportsAllowedTables()
    {
        var config = Toml.ReadString(@"
[[DuplicateImportsAllowedTables]]
HostCategory = ""TestHost""
TableName = ""TEST_TABLE""
");
        var actual = config.ParseDuplicateImportsAllowedTables().Single();
        var expected = new Table("TestHost", "TEST_TABLE");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestDestinationShards()
    {
        var config = Toml.ReadString(@"
[[DestinationHost]]
HostCategory = ""TestHost""
Host = ""subdomain.host1.com""
Port = 1521
User = ""user""
Password = ""password""
Sid = ""sid""
Schema = ""schema""
ShardIndex = 0
    [[DestinationHost.DestinationShard]]
    Host = ""subdomain.host2.com""
    Port = 1522
    User = ""user1""
    Password = ""password1""
    Sid = ""sid1""
    ShardedIndex = 1
    [[DestinationHost.DestinationShard]]
    Host = ""subdomain.host3.com""
    User = ""user2""
    Password = ""password2""
    Sid = ""sid2""
    Schema = ""schema2""
    ShardedIndex = 2
");

        var hostConfig = config.Get<TomlTableArray>(Constants.DestinationHost)[0];

        var actual = HostConfigToml.Parse(hostConfig);
        for (var i = 0; i < 3; i++)
        {
            var host = actual.GetShardedDestination(i);
            switch (i)
            {
                case 0:
                    Assert.Equal("subdomain.host1.com", host.Host);
                    Assert.Equal(1521, host.Port);
                    Assert.Equal("SCHEMA", host.Schema);
                    break;
                case 1:
                    Assert.Equal("subdomain.host2.com", host.Host);
                    Assert.Equal(1522, host.Port);
                    Assert.Equal("SCHEMA", host.Schema);
                    break;
                case 2:
                    Assert.Equal("subdomain.host3.com", host.Host);
                    Assert.Equal(1521, host.Port);
                    Assert.Equal("SCHEMA2", host.Schema);
                    break;
            }
        }
    }

    [Fact]
    public void TestParseShardedTableMappings()
    {
        var config = Toml.ReadString(@"
         [[DestinationShardedTableMap]]
         HostCategory = ""TestHost""
         ShardIndex = 0
         Table10 = 11
         Table2 = 22
         [[DestinationShardedTableMap]]
         HostCategory = ""TestHost""
         ShardIndex = 1
         Table10 = 0
         Table2 = 99");
        var lookup = config.ParseShardedTableMappings();
        var tables = new[] { new Table("TestHost", "Table10"), new Table("TestHost", "Table2") };
        foreach (var table in tables)
        {
            var tableName = Utilities.GetShardedTableName(table, 0, lookup);
            switch (table.TableName)
            {
                case "Table10":
                {
                    Assert.Equal("Table11", tableName);
                    break;
                }
                case "Table2":
                {
                    Assert.Equal("Table22", tableName);
                    break;
                }
            }

            var otherTableName = Utilities.GetShardedTableName(table, 1, lookup);
            switch (table.TableName)
            {
                case "Table10":
                {
                    Assert.Equal("Table00", otherTableName);
                    break;
                }
                case "Table2":
                {
                    Assert.Equal("Table99", otherTableName);
                    break;
                }
            }
        }
    }

    [Fact]
    public void TestParseUpsertTables()
    {
        var config = Toml.ReadString(@"
         [[UpsertTable]]
         HostCategory = ""TestHost""
         Table = ""TEST_TABLE_1""
         PrimaryOrUniqueKeyColumns = [""A"", ""B""]
         CompositeKeyColumns = [""D"", ""E""]
         TempSubQueryClause = ""temp.E IS NOT NULL""
         UpdateWhereClause = ""orig.A IS NOT NULL""
         InsertWhereClause = ""temp.B IS NOT NULL""
         ConditionAndClause = ""orig.C IS NOT NULL""
        ");
        var tables = config.ParseUpsertTables();
        Assert.Single(tables);
        var table1 = new Table("TestHost", "TEST_TABLE_1");
        Assert.True(tables.ContainsKey(table1));
    }

    [Fact]
    public void TestParseWorkspaces()
    {
        var tomlConfig = Toml.ReadString(@"
[TableCount.CountOptions]
IncludedWorkspaces='''
alias_a,user_a,workspace a
alias_b,user_b,workspace b'''

[HostCategoryToWorkspace]
user_a = ""workspace a (DR1)""
user_b = ""workspace (008) b ""
user_c = ""workspace c""
[SomeOtherConfig]
test = ""test""
");
        var hostCategoryToWorkspace = tomlConfig.ParseHostToWorkspace();
        var includedWorkspaces = tomlConfig.ParseIncludedWorkspaces(hostCategoryToWorkspace);
        var expected = new HashSet<string> { "user_a", "user_b" }.ToImmutableHashSet();
        Assert.True(expected.IsSubsetOf(includedWorkspaces) && includedWorkspaces.IsSubsetOf(expected));
    }

    [Fact]
    public void TestParseWorkspacesThrowsIfInclusionMissingCategory()
    {
        var tomlConfig = Toml.ReadString(@"
[TableCount.CountOptions]
IncludedWorkspaces='''
alias_a,user_a,workspace a
alias_b,user_b,workspace b
alias_c,user_c,workspace c'''

[HostCategoryToWorkspace]
user_a = ""workspace a (DR1)""
user_b = ""workspace b (008)""
");
        var hostCategoryToWorkspace = tomlConfig.ParseHostToWorkspace();
        var exception = Assert.Throws<InvalidConfigurationException>(() =>
            tomlConfig.ParseIncludedWorkspaces(hostCategoryToWorkspace));
        Assert.Equal(
            "Invalid Configuration: [HostCategoryToWorkspace] missing included workspaces: [workspace c]",
            exception.Message);
    }
}