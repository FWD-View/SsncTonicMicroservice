using System;
using Tonic.Common.Extensions;

namespace Tonic.Common.OracleHelper.Models;

/// <summary>
/// Built-in 'OBJECT_TYPE' values
/// </summary>
/// <remarks>
///     -- AS OF Oracle 21c
///         SELECT DISTINCT OBJECT_TYPE FROM ALL_OBJECTS
///         ORDER BY OBJECT_TYPE ASC
/// </remarks>
public enum OracleObjectType
{
    [Parameter("CLUSTER")]
    Cluster,
    [Parameter("CONSTRAINT")]
    Constraint,
    [Parameter("CONSUMER GROUP")]
    ConsumerGroup,
    [Parameter("DESTINATION")]
    Destination,
    [Parameter("DIRECTORY")]
    Directory,
    [Parameter("EDITION")]
    Edition,
    [Parameter("EVALUATION CONTEXT")]
    EvaluationContext,
    [Parameter("FUNCTION")]
    Function,
    /// <summary>
    /// The following types of privileges can be granted
    ///     Delete data from a specific table.
    ///     Insert data into a specific table.
    ///     Create a foreign key reference to the named table or to a subset of columns from a table.
    ///     Select data from a table, view, or a subset of columns in a table.
    ///     Create a trigger on a table.
    ///     Update data in a table or in a subset of columns in a table.
    ///     Run a specified function or procedure.
    ///     Use a sequence generator or a user-defined type.
    /// </summary>
    [Parameter("GRANT")]
    Grant,
    [Parameter("INDEX")]
    Index,
    [Parameter("INDEX PARTITION")]
    IndexPartition,
    [Parameter("INDEXTYPE")]
    IndexType,
    [Parameter("JAVA CLASS")]
    JavaClass,
    [Parameter("JAVA DATA")]
    JavaData,
    [Parameter("JAVA RESOURCE")]
    JavaResource,
    [Parameter("JOB")]
    Job,
    [Parameter("JOB CLASS")]
    JobClass,
    [Parameter("LIBRARY")]
    Library,
    [Parameter("LOB")]
    LargeObject,
    [Parameter("LOCKDOWN PROFILE")]
    LockdownProfile,
    [Parameter("MLE LANGUAGE")]
    MleLanguage,
    [Parameter("MATERIALIZED_VIEW")]
    MaterializedView,
    [Parameter("MATERIALIZED_VIEW_LOG")]
    MaterializedViewLog,
    [Parameter("OPERATOR")]
    Operator,
    [Parameter("PACKAGE")]
    Package,
    [Parameter("PACKAGE BODY")]
    PackageBody,
    [Parameter("PROCEDURE")]
    Procedure,
    [Parameter("ROLLBACK_SEGMENT")]
    RollbackSegment,
    [Parameter("REF_CONSTRAINT")]
    RefConstraint,
    [Parameter("RULE SET")]
    RuleSet,
    [Parameter("SCHEDULE")]
    Schedule,
    [Parameter("SCHEDULER GROUP")]
    SchedulerGroup,
    [Parameter("SEQUENCE")]
    Sequence,
    [Parameter("STATISTICS")]
    Statistics,
    [Parameter("SYNONYM")]
    Synonym,
    [Parameter("TABLE")]
    Table,
    [Parameter("TABLE PARTITION")]
    TablePartition,
    [Parameter("TABLESPACE")]
    TableSpace,
    [Parameter("TABLE SUBPARTITION")]
    TableSubpartition,
    [Parameter("TRIGGER")]
    Trigger,
    [Parameter("TYPE")]
    Type,
    [Parameter("TYPE BODY")]
    TypeBody,
    [Parameter("USER")]
    User,
    [Parameter("VIEW")]
    View,
    [Parameter("WINDOW")]
    Window,
    [Parameter("XML SCHEMA")]
    XmlSchema,
}

public static class OracleObjectTypeExtensions
{
    public static string GetParameterName(this OracleObjectType oracleObjectType)
    {
        var attribute = oracleObjectType.GetAttribute<ParameterAttribute>();
        if (attribute == null)
        {
            throw new ArgumentNullException($"{nameof(OracleObjectType)}.{oracleObjectType} must have a {nameof(ParameterAttribute)}");
        }

        return attribute.Name;
    }
}