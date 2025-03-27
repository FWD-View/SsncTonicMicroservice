#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using Moq;

namespace Tonic.Test.Utils.MockBuilders;

public class DbDataReaderBuilder
{
    private Mock<DbDataReader> _mock = new();
    private Func<DbDataReaderBuilder, string, DbDataReaderBuilder> CreateDbReaderFunc { get; set; } = CreateReader;

    public DbDataReader Object => _mock.Object;

    public static DbDataReaderBuilder CreateInstance()
    {
        return new DbDataReaderBuilder
        {
            _mock = new Mock<DbDataReader>()
        };
    }

    public DbDataReaderBuilder WithReader(Func<DbDataReaderBuilder, string, DbDataReaderBuilder> createDbReader)
    {
        CreateDbReaderFunc = createDbReader;
        return this;
    }

    public DbDataReaderBuilder WithRecordsAffected(int records)
    {
        _mock.SetupGet(x => x.RecordsAffected).Returns(records);
        return this;
    }

    public Func<string, DbDataReader> ForQuery(Func<string, string> parseQuery) =>
        (unparsedQuery) =>
        {
            var parsedQuery = parseQuery(unparsedQuery);
            return CreateDbReaderFunc(this, parsedQuery).Object;
        };

    public Func<string, Dictionary<string, string>, DbDataReader> ForParamQuery(
        Func<string, Dictionary<string, string>, string> parseQuery) =>
        (unparsedQuery, paramsDict) =>
        {
            var parsedQuery = parseQuery(unparsedQuery, paramsDict);
            return CreateDbReaderFunc(this, parsedQuery).Object;
        };

    private static DbDataReaderBuilder CreateReader(DbDataReaderBuilder self, string parsedQuery)
    {
        const string selectFrom = @"^SELECT (.*) FROM .*\.(.*)$";
        var regex = new Regex(selectFrom, RegexOptions.Singleline);
        var matches = regex.Match(parsedQuery);
        var captures = matches.Groups.Values.Select(g => g.Value).Skip(1).ToArray();
        var capturedColumns = captures[0]
            .Split(", ")
            .ToImmutableArray();

        var count = -1;

        self._mock.Setup(x => x.Read())
            // Return 'True' while list still has an item
            .Returns(() => count < 9)
            // Go to next position
            .Callback(() => count++);

        self._mock.SetupGet(x => x.FieldCount).Returns(capturedColumns.Count);
        self._mock.Setup(x => x.GetValue(It.IsAny<int>()))
            .Returns((int i) => $"{capturedColumns[i]}_{count}");
        self._mock.Setup(x => x.IsDBNull(It.IsAny<int>()))
            .Returns((int _) => false);
        return self;
    }

    public static DbDataReaderBuilder CreateDeleteReader(DbDataReaderBuilder self, string parsedQuery)
    {
        return self;
    }


    public static DbDataReaderBuilder CreateVinReader(DbDataReaderBuilder self, string parsedQuery)
    {
        var vinItems = new[]
        {
            "|4|-30|54|A|4|-52|0|B|4|-55|0|C|4|-20|2|D|4|4148||1|0||1|-3|E|F|0|4154",
            "|4|-30|54|A|4|-52|0|B|4|-55|0|C|4|-20|2|D|4|4148||1|0||1|-3|E|F|0|4154",
            "|4|-30|54|A|4|-52|0|B|4|-55|0|C|4|-20|2|D|4|4148||1|0||1|-3|E|F|0|4154",
            "|4|-30|54|A|4|-52|0|B|4|-55|0|C|4|-20|2|D|4|4148||1|0||1|-3|E|F|0|4154"
        };

        var count = -1;

        self._mock.Setup(x => x.Read())
            // Return 'True' while list still has an item
            .Returns(() => count < vinItems.Length - 1)
            // Go to next position
            .Callback(() => count++);

        self._mock.SetupGet(x => x.FieldCount).Returns(1);
        self._mock.Setup(x => x.GetValue(It.IsAny<int>()))
            .Returns((int _) => vinItems[count]);
        self._mock.Setup(x => x.IsDBNull(It.IsAny<int>()))
            .Returns((int _) => false);
        return self;
    }

    public DbDataReaderBuilder CreateGenericReaderForRows(string?[][] rows)
    {
        var count = -1;

        _mock.Setup(x => x.Read())
            // Return 'True' while list still has an item
            .Returns(() => count < rows.Length - 1)
            // Go to next position
            .Callback(() => count++);

        _mock.SetupGet(x => x.FieldCount).Returns(rows.First().Length);
        _mock.Setup(x => x.GetString(It.IsAny<int>()))
            .Returns((int index) => rows[count][index]!);
        _mock.Setup(x => x.IsDBNull(It.IsAny<int>()))
            .Returns((int i) => rows[count][i] == null);
        return this;
    }


    public static string ParseGenericDownstreamDestinationQueryFirstSubselect(string query)
    {
        /*language=text*/
        const string joinedSelect = @"SELECT (.*) FROM \(
(.*)
\) l 
LEFT JOIN \(
(.*)
\) r ON (.*)
WHERE (.*) AND (.*)";
        var regex = new Regex(joinedSelect, RegexOptions.Singleline);
        if (!regex.IsMatch(query)) return null;
        var matches = regex.Match(query);
        var captures = matches.Groups.Values.Select(g => g.Value).Skip(1).ToArray();
        var subSelect = captures[1].Trim();
        return subSelect;
    }
}