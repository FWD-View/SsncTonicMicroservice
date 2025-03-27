using Nett;
using Tonic.Common.Models;

namespace Tonic.Subsetter.Actions;

public class UpstreamGroupLimit
{
    public readonly Table Table;
    public readonly string GroupBy;
    public readonly string OrderBy;
    public readonly int Limit;

    public UpstreamGroupLimit(TomlTable config)
    {
        Table = new Table(config.Get<string>("HostCategory"), config.Get<string>("Table"));
        GroupBy = config.Get<string>("GroupBy");
        OrderBy = config.Get<string>("OrderBy");
        Limit = config.Get<int>("Limit");
    }
}