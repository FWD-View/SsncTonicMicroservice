#nullable enable
using System;
using System.Collections.Generic;
using System.Data.Common;
using Moq;
using Tonic.Common;
using Tonic.Common.Configs;
using Tonic.Common.Helpers;

namespace Tonic.Test.Utils.MockBuilders;

public class HostBuilder
{
    private Mock<IHost> _mock = new();
    public IHost Object => _mock.Object;

    public static HostBuilder CreateInstance()
    {
        var builder = new HostBuilder()
        {
            _mock = new Mock<IHost>()
        };
        return builder;
    }

    public HostBuilder WithConfig(HostConfig config)
    {
        _mock.SetupGet(h => h.Configuration).Returns(config);
        return this;
    }

    public HostBuilder WithExecuteQuery(Func<string, DbDataReader> dbDataReader)
    {
        _mock.Setup(h => h.ExecuteQuery(It.IsAny<string>()))
            .Returns(dbDataReader);
        return this;
    }

    public HostBuilder WithExecuteNonQuery(Action<string>? action = null)
    {
        _mock.Setup(h => h.ExecuteNonQuery(It.IsAny<string>()))
            .Callback(action ?? delegate { });
        return this;
    }

    public HostBuilder WithExecuteParameterizedQuery(
        Func<string, Dictionary<string, string>, DbDataReader>? action)
    {
        _mock.Setup(h => h.ExecuteParameterizedQuery(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(action);
        return this;
    }

    public HostBuilder WithGetAllShardedHosts(IEnumerable<(int, IHost)> hosts)
    {
        _mock.Setup(h => h.GetAllShardedHosts()).Returns(() => hosts);
        return this;
    }

    public HostBuilder WithCreateAdminHost()
    {
        _mock.Setup(h => h.CreateAdminHost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HostConfig?>()))
            .Returns((string _, string _, HostConfig _) => _mock.Object);
        return this;
    }

    public HostBuilder WithName(string name)
    {
        _mock.SetupGet(h => h.Name).Returns(name);
        return this;
    }
}