using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Tonic.Common;
using Tonic.Common.Helpers;
using Xunit;

namespace Tonic.Test.CommonTests;

public class TsvWriterTests
{
    [Fact]
    public void TestSimpleWrite()
    {
        List<string> files = new();
        var stringWriter = new StringWriter();
        var tsvWriter = new TsvWriter
        {
            Separator = ',',
            TempFilePath = string.Empty,
            HostAndTable = "TEST.TABLE",
            OpenTsvFile = (filePath, _) =>
            {
                files.Add(filePath);
                return stringWriter;
            }
        };
        foreach (var row in MakeRows(1))
        {
            tsvWriter.WriteRow(row);
        }

        Assert.Equal(2, files.Count);
        var csvFiles = tsvWriter.TakeFiles().ToList();
        Assert.Single(csvFiles);
        Assert.Equal("TEST.TABLE__0__0", csvFiles.Single());
        Assert.Equal(files.First(), csvFiles.Single() + ".tsv");
        const string expected = $"0000000011a_0,b_0,c_0";
        Assert.Equal(expected, stringWriter.ToString());
        var remaining = tsvWriter.TakeAllFiles();
        Assert.Empty(remaining);
    }

    [Fact]
    public void TestMultiHostWrite()
    {
        List<string> files = new();
        var stringWriters = new Dictionary<string, StringWriter>();
        var written = new Dictionary<string, List<string>>();
        var tsvWriter = new TsvWriter
        {
            RowLimit = 3,
            ShardDiscriminant = (_, row, _) =>
            {
                var index = int.Parse(row[0].Split('_')[1]);
                return index % 2;
            },
            Separator = ',',
            TempFilePath = string.Empty,
            HostAndTable = "TEST.TABLE_00",
            DestinationCount = 2,
            OpenTsvFile = (filePath, _) =>
            {
                files.Add(filePath);
                var destinationIndex = Path.GetFileName(filePath).Split(TsvWriter.FieldSeparator)[1];
                written.TryAdd(destinationIndex, new List<string>());
                if (stringWriters.ContainsKey(destinationIndex))
                    written[destinationIndex].Add(stringWriters[destinationIndex].ToString());
                stringWriters[destinationIndex] = new StringWriter();
                return stringWriters[destinationIndex];
            }
        };
        foreach (var row in MakeRows(8))
        {
            tsvWriter.WriteRow(row);
        }

        Assert.Equal(4, files.Count);
        var rowBatch = written.ToDictionary(x => x.Key, x => x.Value.Select(y => y.Split("0000000011")).Skip(1).ToArray());
        foreach (var (index, batch) in rowBatch)
        {
            foreach (var row in batch)
            {
                var val = row[0].Split(',')[0].Split('_')[0];
                var actual = int.Parse(val);
                var expected = int.Parse(index);
                Assert.Equal(expected, actual);
            }
        }

        var remainingFiles = tsvWriter.TakeAllFiles();
        Assert.Equal(2, remainingFiles.Count);
        Assert.True(new []{"TEST.TABLE_00__0__1", "TEST.TABLE_00__1__1"}.SequenceEqual(remainingFiles));
            
        var csvFiles = tsvWriter.TakeFiles().ToImmutableHashSet();
        var expectedFiles = new List<string>
        {
            "TEST.TABLE_00__1__0",
            "TEST.TABLE_00__0__0",
        }.ToImmutableHashSet();
        Assert.True(expectedFiles.SequenceEqual(csvFiles.ToList()));

        foreach (var f in csvFiles)
        {
            var destinationIndex = TsvWriter.GetIndexForBaseName(f);
            Assert.Contains(destinationIndex, new HashSet<int> { 0, 1 });
        }
    }

    private IEnumerable<string[]> MakeRows(int n)
    {
        for (var i = 0; i < n; i++)
        {
            yield return new[] { $"a_{i}", $"b_{i}", $"c_{i}" };
        }
    }
}