using System.Diagnostics.CodeAnalysis;

namespace Tonic.Common.OracleHelper.Models.Enums;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DisplayCursorFormat
{
    /// <summary>
    /// displays default information
    /// </summary>
    TYPICAL = 0,
    /// <summary>
    /// * Displays the final plan, or the current plan if the execution has not completed. This section includes notes about runtime optimizations that affect the plan, such as switching from a Nested Loops join to a Hash join.
    /// * Plan lineage. This section shows the plans that were run previously due to automatic reoptimization. It also shows the default plan, if the plan changed due to dynamic plans.
    /// * Recommended plan. In reporting mode, the plan is chosen based on execution statistics displayed. Note that displaying the recommended plan for automatic reoptimization requires re-compiling the query with the optimizer adjustments collected in the child cursor. Displaying the recommended plan for a dynamic plan does not require this.
    /// * Dynamic plans. This summarizes the portions of the plan that differ from the default plan chosen by the optimizer.
    /// </summary>
    ADAPTIVE,
    /// <summary>
    /// Similar to <see cref="ALL"/>, but also include the Outline information (the set of hints that will reproduce the plan) and the peeked bind variables used to optimize the query
    /// </summary>
    ADVANCED,
    /// <summary>
    /// If relevant, shows the "Query Block Name / Object Alias" section
    /// </summary>
    ALIAS,
    /// <summary>
    /// Shows the Query block/Object Alias section, Predicate information, and Column Projections following the plan
    /// </summary>
    ALL,
    /// <summary>
    /// A shortcut for 'IOSTATS MEMSTATS'
    /// </summary>
    ALLSTATS,
    /// <summary>
    /// If relevant, shows the number of bytes estimated by the optimizer
    /// </summary>
    BYTES,
    /// <summary>
    /// If relevant, shows optimizer cost information
    /// </summary>
    COST,
    /// <summary>
    /// Assuming that basic plan statistics are collected when SQL statements are executed (either by using the gather_plan_statistics hint or by setting the parameter statistics_level to ALL), this format will show IO statistics for ALL (or only for the LAST as shown below) executions of the cursor
    /// </summary>
    IOSTATS,
    /// <summary>
    /// By default, plan statistics are shown for all executions of the cursor. The keyword LAST can be specified to see only the statistics for the last execution
    /// </summary>
    LAST,
    /// <summary>
    /// Assuming that PGA memory management is enabled (that is, pga_aggregate_target parameter is set to a non 0 value), this format allows to display memory management statistics (for example, execution mode of the operator, how much memory was used, number of bytes spilled to disk, and so on). These statistics only apply to memory intensive operations like hash-joins, sort or some bitmap operators
    /// </summary>
    MEMSTATS,
    /// <summary>
    /// If relevant, shows the note section of the explain plan
    /// </summary>
    NOTE,
    /// <summary>
    /// Shows only Outline and Predicate information after the basic plan
    /// </summary>
    OUTLINE,
    /// <summary>
    /// If relevant, shows PX information (distribution method and table queue information)
    /// </summary>
    PARALLEL,
    /// <summary>
    /// If relevant, shows partition pruning information
    /// </summary>
    PARTITION,
    /// <summary>
    /// If relevant, shows the predicate section
    /// </summary>
    PREDICATE,
    /// <summary>
    /// If relevant, shows the projection section
    /// </summary>
    PROJECTION,
    /// <summary>
    /// If relevant, shows the information for distributed query (for example, remote from serial distribution and remote SQL)
    /// </summary>
    REMOTE,
    /// <summary>
    /// If relevant, shows the number of rows estimated by the optimizer
    /// </summary>
    ROWS,
    /// <summary>
    /// Same as IOSTATS LAST: displays the runtime stat for the last execution of the cursor
    /// </summary>
    RUNSTATS_LAST,
    /// <summary>
    /// Same as IOSTATS: displays IO statistics for all executions of the specified cursor
    /// </summary>
    RUNSTATS_TOT,
}