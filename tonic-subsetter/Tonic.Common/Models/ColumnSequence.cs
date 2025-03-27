using Nett;
using Tonic.Common.Configs;
using Tonic.Common.Extensions;

namespace Tonic.Common.Models;

public class ColumnSequence
{
    public readonly string HostCategory;
    public readonly string Table;
    public readonly string Column;
    // This value, when present, is the number of tables in a table group associated with the table for this
    // sequence. For example, if in the ForeignKeys configuration the TableGroupSize is 10 for SUFFIXED_TABLE_N,
    // then the sequence for SUFFIXED_TABLE_0.SUFFIXED_TABLE_ID should have ModulusHack 10. The ModulusHack
    // is to ensure we only get sequence values that allow insertion into the 0 table of the table group.
    // In other words, we skip any sequence value for PRIMARY_TABLE.ID that would put any rows in tables other than
    // SUFFIXED_TABLE_0. (PRIMARY_TABLE.ID, aka SUFFIXED_TABLE_0.SELLER_ID, is used to determine which table
    // that seller's campaign info belongs.)
    public readonly int? ModulusHack;
    public readonly HostConfig? SequenceHost;
    public readonly string? SequenceName;

    public ColumnSequence(TomlTable config)
    {
        HostCategory = config.Get<string>("HostCategory");
        Table = config.Get<string>("Table");
        Column = config.Get<string>("Column");
        ModulusHack = config.ContainsKey("ModulusHack") ? config.Get<int?>("ModulusHack") : null;
        if (!config.ContainsKey("SequenceConfig")) return;
        var seqConfig = config.Get<TomlTable>("SequenceConfig");
        SequenceHost = HostConfigToml.Parse(seqConfig);
        SequenceName = seqConfig.Get<string>("SequenceName");
    }
}