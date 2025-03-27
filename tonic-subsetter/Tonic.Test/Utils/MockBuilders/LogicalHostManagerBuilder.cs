using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Subsetter.Utils;

namespace Tonic.Test.Utils.MockBuilders;

public class HostManagerBuilder
{
    private Mock<IHostsService> MockHostManager { get; }
    private Dictionary<string, List<IHost>> MockHosts { get; }
    public IHostsService Object => MockHostManager.Object;

    private HostManagerBuilder(Dictionary<string, List<IHost>> mockHosts)
    {
        MockHostManager = new Mock<IHostsService>();
        MockHosts = mockHosts;
    }

    public static HostManagerBuilder WithHosts(IEnumerable<IHost> hosts)
    {
        var hostLookup = new Dictionary<string, List<IHost>>();
        foreach (var host in hosts)
        {
            if (!hostLookup.ContainsKey(host.Configuration.HostCategory))
                hostLookup[host.Configuration.HostCategory] = new List<IHost>();
            hostLookup[host.Configuration.HostCategory].Add(host);
        }

        return new HostManagerBuilder(hostLookup);
    }

    public HostManagerBuilder WithHostsEnumerator()
    {
        MockHostManager
            .Setup(x => x.GetEnumerator())
            .Returns(() => MockHosts.Select(kvp => (kvp.Key, kvp.Value.First())).GetEnumerator());
        return this;
    }

    public HostManagerBuilder WithRunOnCategory()
    {
        MockHostManager
            .Setup(dlh => dlh.RunOnCategory(It.IsAny<string>(), It.IsAny<Action<IHost>>()))
            .Returns((string category, Action<IHost> action) =>
            {
                foreach (var host in MockHosts[category])
                {
                    action(host);
                }

                return new[] { Task.CompletedTask, };
            });
        return this;
    }

    public HostManagerBuilder WithRunOnMultiplexedCategories()
    {
        MockHostManager
            .Setup(dlh => dlh.RunOnCategoryWithMultiplexedQueue(It.IsAny<string>(),
                It.IsAny<BlockingCollection<It.IsAnyType>>(),
                It.IsAny<Action<IHost,
                    BlockingCollection<It.IsAnyType>>>()))
            .Returns((string category, BlockingCollection<(string, Dictionary<string, string>)> queriesQueue,
                Action<IHost, BlockingCollection<(string, Dictionary<string, string>)>> action) =>
            {
                foreach (var host in MockHosts[category])
                {
                    action(host, queriesQueue);
                }

                return new[] { Task.CompletedTask, };
            });
        return this;
    }
}