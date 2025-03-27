using Tonic.Subsetter.Utils;

namespace Tonic.Subsetter;

public static class SubsetterService
{
    public static void Run(ISubsetConfig config)
    {
        var sourceConnections = new HostsService(config.SourceHostConfigs, config.SchemaOverrides);
        var destinationConnections = new HostsService(config.DestinationHostConfigs);

        var subsetter = new Subsetter(config, sourceConnections, destinationConnections);
        subsetter.Subset();
    }
}