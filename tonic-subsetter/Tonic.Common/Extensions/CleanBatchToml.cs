using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Nett;

namespace Tonic.Common.Extensions;

public static class CleanBatchToml
{
    public static ImmutableArray<string> ParseUserColumns(this TomlTable config)
    {
        if (!config.ContainsKey("CleanUserColumns"))
            throw new InvalidDataException(
                "Error: could not locate Users table, identified by [CleanUserColumns] key");

        var userColumns = (TomlTable)config["CleanUserColumns"];
        var columnsKeys = userColumns.Get<TomlArray>("RowKeys");
        return columnsKeys.To<string>().Select(s => s.ToUpperInvariant()).ToImmutableArray();
    }
}