using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Tonic.Common.Helpers;

public class NoOpDbCommand : DbCommand
{
    public static readonly DbCommand Instance = new NoOpDbCommand();
    private NoOpDbCommand()
    {
    }
    public override void Cancel()
    {
        throw new NotImplementedException();
    }

    public override int ExecuteNonQuery() => 0;

    public override object? ExecuteScalar() => null;

    public override void Prepare()
    {
        //do nothing
    }

    [AllowNull]
    public override string CommandText { get; set; } = null!;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection { get; } = null!;
    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get; set; }

    protected override DbParameter CreateDbParameter() => throw new NotImplementedException();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
}