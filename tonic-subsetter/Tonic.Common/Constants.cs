using System.Diagnostics.CodeAnalysis;

namespace Tonic.Common;

public static class Constants
{
    public const string RedactedLookupHost = "REDACTED";
    //public const string DestinationHost = "REDACTED";
    //public const string SourceHost = "REDACTED";
    public const string DestinationHost = "DestinationHost";
    public const string SourceHost = "SourceHost";

    public const string RedactedLookup = "REDACTED";
    public const string Redacted2LookupCategory = "REDACTED";
    public const string RedactedLookupTable = "REDACTED";
    public const string Redacted3LookupCategory = "REDACTED";
    public const string TonicSchemaToken = "{{TONIC_SCHEMA_TOKEN}}";

    public const string MissingKeyString = "99999999999999";
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public const string RedactedValue = "<redacted>";
}