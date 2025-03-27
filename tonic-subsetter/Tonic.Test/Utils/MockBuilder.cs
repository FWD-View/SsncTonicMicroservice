#nullable  enable
using System;
using System.Collections.Generic;
using System.Data.Common;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Test.Utils.MockBuilders;

namespace Tonic.Test.Utils;

// ReSharper disable once ClassNeverInstantiated.Global
public record MockBuilder
{
    public static HostBuilder Host() => HostBuilder.CreateInstance();

    public static HostManagerBuilder HostManager(IEnumerable<IHost> hosts) =>
        HostManagerBuilder.WithHosts(hosts);

    // ReSharper disable once ClassNeverInstantiated.Global
    public record DbReader
    {
        public static DbDataReaderBuilder CreateInstance => DbDataReaderBuilder.CreateInstance();
        public static Func<string, DbDataReader> ForQuery(Func<string, string> parseQuery)
        {
            var builder = DbDataReaderBuilder.CreateInstance();
            return builder.ForQuery(parseQuery);
        }

        public static Func<string, Dictionary<string, string>, DbDataReader>
            ForParamQuery(Func<string, Dictionary<string, string>, string> parseQuery)
        {
            var builder = DbDataReaderBuilder.CreateInstance();
            return builder.ForParamQuery(parseQuery);
        }
    }
}