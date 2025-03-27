using System;
using System.Runtime.CompilerServices;

namespace Tonic.Common.Extensions;

public static class ValueTypeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsAny<T>(this T valueType, params T[]? values)
        where T : struct, IConvertible
    {
        if (values == null || values.Length < 1)
        {
            return false;
        }
        for (int i = 0; i < values.Length; i++)
        {
            if (valueType.Equals(values[i]))
            {
                return true;
            }
        }
        return false;
    }
}