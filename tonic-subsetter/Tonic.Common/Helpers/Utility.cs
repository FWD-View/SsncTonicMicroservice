using System;
using System.IO;
using NodaTime.Text;
using Tonic.Common.Enums;

namespace Tonic.Common.Helpers
{
    public static partial class Utility
    {
        public static readonly LocalDatePattern LD_PATTERN = LocalDatePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd");

        public static readonly LocalDateTimePattern LDT_PATTERN =
            LocalDateTimePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd HH':'mm':'ss.FFFFFFFFF");

        public static readonly InstantPattern INSTANT_PATTERN = InstantPattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd HH':'mm':'ss.FFFFFF");

        public static readonly OffsetDateTimePattern OFFSET_DATETIME_PATTERN =
            OffsetDateTimePattern.CreateWithInvariantCulture("yyyy'-'MM'-'dd HH':'mm':'sso<+HH>");

        public static readonly OffsetDateTimePattern DATE_TIME_OFFSET_PATTERN =
            OffsetDateTimePattern.CreateWithInvariantCulture(LDT_PATTERN.PatternText + "o<G>");

        public static readonly OffsetTimePattern OFFSET_TIME_PATTERN = OffsetTimePattern.CreateWithInvariantCulture("HH:mm:ss");

        public static readonly LocalTimePattern LOCAL_TIME_PATTERN = LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss");

        public static string GetSharedTempPath()
        {
            var tempFilePath = TonicEnvironmentVariable.TONIC_SHARED_TMP_FILE_PATH.Get();
            ArgumentNullException.ThrowIfNull(tempFilePath);
            Directory.CreateDirectory(tempFilePath);

            if (!Directory.Exists(tempFilePath))
            {
                throw new DirectoryNotFoundException(tempFilePath);
            }

            return Path.GetFullPath(tempFilePath);
        }
    }
}