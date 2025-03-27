using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tonic.Common.Models;
using Tonic.Common.OracleHelper;

namespace Tonic.Common.Helpers;

public interface ITsvReader
{
    public Task<List<string[]>> ReadBatch(IEnumerable<Column> columns);
    public IAsyncEnumerable<string[]> ReadAllRows();
}

public class TsvReader : ITsvReader
{
    private static string Extension => ".tsv";
    public Func<string, Stream> OpenStream { get; init; } = OpenFileStream;
    private IEnumerable<string> Files { get; }


    public TsvReader(string file)
    {
        Files = new[] { file };
    }

    public TsvReader(IEnumerable<string> files)
    {
        Files = files;
    }

    public async Task<List<string[]>> ReadBatch(IEnumerable<Column> columns)
    {
        var batch = new List<string[]>();
        await foreach (var row in ReadAllRows())
        {
            batch.Add(row);
        }

        return batch;
    }

    public async IAsyncEnumerable<string[]> ReadAllRows()
    {
        var bufferLength = 500;
        const int recordLengthNumDigits = OracleUtilities.RecordLengthNumDigits;
        foreach (var file in Files)
        {
            await using var fs = OpenStream(file);
            var rowBuffer = new byte[bufferLength];
            var rowLengthBuffer = new byte[recordLengthNumDigits];

            while (true)
            {
                Array.Clear(rowLengthBuffer, 0, recordLengthNumDigits);
                Array.Clear(rowBuffer, 0, bufferLength);

                var rowLengthBytesRead = await fs.ReadAsync(rowLengthBuffer.AsMemory(0, recordLengthNumDigits));
                if (rowLengthBytesRead <= 0) break;

                var rowLengthString = Encoding.UTF8.GetString(rowLengthBuffer);
                var validRowLength = int.TryParse(rowLengthString, out var rowLength);
                if (!validRowLength)
                    throw new InvalidDataException($"Error: invalid row length in {file}");

                if (rowLength > bufferLength)
                {
                    rowBuffer = new byte[rowLength];
                    bufferLength = rowLength;
                }

                var rowBufferReadBytes = await fs.ReadAsync(rowBuffer.AsMemory(0, rowLength));
                if (rowBufferReadBytes <= 0)
                {
                    throw new InvalidDataException(
                        $"Error: {file} invalid, should not end with valid character field");
                }

                var rowContents = Encoding.UTF8.GetString(rowBuffer.Take(rowLength).ToArray());
                yield return rowContents.Split(OracleUtilities.TsvColSeparatingChar);
            }
        }
    }


    private static FileStream OpenFileStream(string file)
    {
        return new FileStream(file + Extension, FileMode.Open, FileAccess.Read);
    }
}