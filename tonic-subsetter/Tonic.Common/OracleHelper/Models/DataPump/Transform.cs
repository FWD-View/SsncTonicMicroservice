namespace Tonic.Common.OracleHelper.Models.DataPump;

/// <summary>
/// Enables you to alter object creation DDL for objects being imported.
/// </summary>
/// <remarks>
/// The options and values are defined on <see cref="TransformFactory"/>
///     https://docs.oracle.com/database/121/SUTIL/GUID-64FB67BD-EB67-4F50-A4D2-5D34518E6BDB.htm#SUTIL939
/// </remarks>
/// <param name="Name">The transform_name specifies the name of the transform.</param>
/// <param name="Value">Valid values have this type</param>
/// <param name="ObjectType">Object type</param>
public sealed record Transform(string Name, object? Value, OracleObjectType? ObjectType)
{
    public static Transform? Parse(string s) => TransformFactory.Parse(s);
}