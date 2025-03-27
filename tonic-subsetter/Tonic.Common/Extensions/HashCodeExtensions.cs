using System;
using NodaTime;
using Tonic.Common.Enums;

namespace Tonic.Common.Extensions
{
    public static class HashCodeExtensions
    {
        public static int GetDeterministicHashCode(this string str)
        {
            unchecked
            {
                var hash1 = (5381 << 16) + 5381;
                var hash2 = hash1;
                for (var i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1) break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + hash2 * 1566083941;
            }
        }
        public static int GetDeterministicHashCode(this int val) => val;
        public static int GetDeterministicHashCode(this long val) => unchecked((int) ((long) val)) ^ (int) (val >> 32);
        public static int GetDeterministicHashCode(this sbyte val) => val;
        public static int GetDeterministicHashCode(this byte val) => val;
        public static int GetDeterministicHashCode(this short val) => val;
        public static int GetDeterministicHashCode(this ushort val) => (int) val;
        public static int GetDeterministicHashCode(this uint val) => unchecked((int) val);
        public static int GetDeterministicHashCode(this ulong val) => ((int) val) ^ (int) (val >> 32);
        public static int GetDeterministicHashCode(this char val) => (int) val | ((int) val << 16);
        public static int GetDeterministicHashCode(this bool val) => (val) ? 1 : 0;
        public static int GetDeterministicHashCode(this double val)
        {
            long bits = BitConverter.DoubleToInt64Bits(val);

            // Optimized check for IsNan() || IsZero()
            if (((bits - 1) & 0x7FFFFFFFFFFFFFFF) >= 0x7FF0000000000000)
            {
                // Ensure that all NaNs and both zeros have the same hash code
                bits &= 0x7FF0000000000000;
            }

            return unchecked((int) bits) ^ ((int) (bits >> 32));
        }
        public static int GetDeterministicHashCode(this float val)
        {
            int bits = BitConverter.SingleToInt32Bits(val);

            // Optimized check for IsNan() || IsZero()
            if (((bits - 1) & 0x7FFFFFFF) >= 0x7F800000)
            {
                // Ensure that all NaNs and both zeros have the same hash code
                bits &= 0x7F800000;
            }
            return bits;
        }
        public static int GetDeterministicHashCode(this Guid val) => val.ToString().GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this DateTime val) => val.Ticks.GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this TimeSpan val) => val.Ticks.GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this DateTimeOffset val)
        {
            var t = val.ToUniversalTime();
            return t.Ticks.GetDeterministicHashCode();
        }
        public static int GetDeterministicHashCode(in this decimal val) => val.ToString().GetDeterministicHashCode();

        public static int GetDeterministicHashCode(this Instant val) => val.ToUnixTimeTicks().GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this LocalDate val) => val.ToDateTimeUnspecified().GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this LocalDateTime val) => val.ToDateTimeUnspecified().GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this LocalTime val) => val.TickOfDay.GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this Period val) => val.Ticks.GetDeterministicHashCode();
        public static int GetDeterministicHashCode(this DateTruncationEnum val) => (int) val;
        public static int GetDeterministicHashCode(this object obj)
        {
            if (obj is string s) return s.GetDeterministicHashCode();
            if (obj is int i) return i.GetDeterministicHashCode();
            if (obj is long l) return l.GetDeterministicHashCode();
            if (obj is short sh) return sh.GetDeterministicHashCode();
            if (obj is ushort us) return us.GetDeterministicHashCode();
            if (obj is uint ui) return ui.GetDeterministicHashCode();
            if (obj is ulong ul) return ul.GetDeterministicHashCode();
            if (obj is char ch) return ch.GetDeterministicHashCode();
            if (obj is bool bo) return bo.GetDeterministicHashCode();
            if (obj is float f) return f.GetDeterministicHashCode();
            if (obj is double d) return d.GetDeterministicHashCode();
            if (obj is decimal de) return de.GetDeterministicHashCode();
            if (obj is Guid g) return g.GetDeterministicHashCode();
            if (obj is TimeSpan ts) return ts.GetDeterministicHashCode();
            if (obj is DateTime dt) return dt.GetDeterministicHashCode();
            if (obj is DateTimeOffset dto) return dto.GetDeterministicHashCode();
            if (obj is Instant ins) return ins.GetDeterministicHashCode();
            if (obj is LocalDate ld) return ld.GetDeterministicHashCode();
            if (obj is LocalTime lt) return lt.GetDeterministicHashCode();
            if (obj is LocalDateTime ldt) return ldt.GetDeterministicHashCode();
            if (obj is Period p) return p.GetDeterministicHashCode();
            if (obj is sbyte sb) return sb.GetDeterministicHashCode();
            if (obj is byte b) return b.GetDeterministicHashCode();
            if (obj is DateTruncationEnum dte) return dte.GetDeterministicHashCode();

            return obj.GetHashCode();
        }
    }
}