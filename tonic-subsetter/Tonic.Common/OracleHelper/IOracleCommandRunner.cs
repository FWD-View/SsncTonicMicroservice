using System.Threading;
using System.Threading.Tasks;
using Tonic.Common.Configs;
using Tonic.Common.OracleHelper.Models;

namespace Tonic.Common.OracleHelper
{
    public interface IOracleCommandRunner
    {
        Task RunCommand<T>(T toolParameters, HostConfig lh, CancellationToken cancellationToken)
            where T : ParametersBase;

        string GetOracleConnectionString(HostConfig lh);
    }
}