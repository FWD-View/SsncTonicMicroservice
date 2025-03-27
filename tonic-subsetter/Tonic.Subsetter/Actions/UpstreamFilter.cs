using Nett;
using Tonic.Common.Models;

namespace Tonic.Subsetter.Actions;

public class UpstreamFilter
{
    public readonly Table Table;
    public readonly string Clause;

    public UpstreamFilter(TomlTable config)
    {
        Table = new Table(config.Get<string>("HostCategory"), config.Get<string>("Table"));
        Clause = config.Get<string>("Clause");
    }
}