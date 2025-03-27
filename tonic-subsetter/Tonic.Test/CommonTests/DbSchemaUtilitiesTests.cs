using System;
using Tonic.Common;
using Tonic.Common.Helpers;
using Tonic.Subsetter.Utils;
using Xunit;

namespace Tonic.Test.CommonTests;

public class DbSchemaUtilitiesTests
{
    [Fact]
    public void TestWithRetriesSuccess()
    {
        var count = 0;
        var func = new Func<IHost, int>(_ =>
        {
            count += 1;
            if (count == 3) return count;
            throw new Exception("exception");
        });

        var result = DbSchemaUtilities.WithRetries(func)(new Host());
        Assert.Equal(3, result);
    }

    [Fact]
    public void TestWithRetriesFailure()
    {
        var func = new Func<IHost, int>(_ => throw new Exception("exception"));

        Exception exception = null!;
        try
        {
            DbSchemaUtilities.WithRetries(func)(new Host());
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        Assert.NotNull(exception);
    }
}