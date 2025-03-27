using System;
using System.Runtime.CompilerServices;

namespace Tonic.Common.Enums
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SecretValueAttribute : Attribute
    {
        public const string HiddenValue = "<secret value>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? Hide(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            return HiddenValue;
        }
    }
}