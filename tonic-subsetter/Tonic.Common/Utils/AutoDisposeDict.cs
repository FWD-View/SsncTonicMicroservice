using System;
using System.Collections.Generic;

namespace Tonic.Common.Utils;

public sealed class AutoDisposeDict<T, TU> : Dictionary<T, TU>, IDisposable where T : notnull where TU : IDisposable
{
    public void Dispose()
    {
        foreach (var value in Values)
        {
            value.Dispose();
        }
    }
}