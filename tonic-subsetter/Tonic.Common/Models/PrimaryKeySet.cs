using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Tonic.Common.Models;

public sealed record PrimaryKeySet
{
    public Table Table { get; }
    public ImmutableHashSet<MultiStringKey> SubsidiaryMultiStringKeys { get; }
    public ImmutableArray<Table> PrincipalKeyTables { get; }
    public ImmutableArray<ForeignKey> PrincipalForeignKeys { get; }
    public ImmutableHashSet<ForeignKey> SubsidiaryForeignKeys { get; }
    public bool IsMultiTablePrincipal { get; }
    public ImmutableArray<Column> Columns;

    public override string ToString()
    {
        var principalKeys = string.Join("\n\t", PrincipalForeignKeys);
        var subsidiaryKeys = string.Join("\n\t", SubsidiaryMultiStringKeys);
        return $"{Table} Primary Key Set:\nPrincipal Keys: {principalKeys}\nSubsidiary Key Sets:{subsidiaryKeys}";
    }

    public PrimaryKeySet(Table table, ImmutableArray<Column> columns, IList<ForeignKey> foreignKeys)
    {
        Table = table;
        Columns = columns;
        var principalForeignKeys = foreignKeys.Where(fk => table.IsForeignKeyOf(fk) && fk.IsPrincipal).ToList();
        IsMultiTablePrincipal = principalForeignKeys.Any() && principalForeignKeys.All(fk => fk.MultiTableIndexColumnIndices.Any());

        if (IsMultiTablePrincipal)
        {
            PrincipalForeignKeys = principalForeignKeys
                .OrderBy(fk => fk.MultiTableIndexColumnIndices.First())
                .ToImmutableArray();
        }
        else
        {
            PrincipalForeignKeys = principalForeignKeys.ToImmutableArray();
        }

        PrincipalKeyTables = PrincipalForeignKeys
            .Select(fk => new Table(fk.PrimaryKeyHostCategory, fk.PrimaryKeyTable))
            .Distinct()
            .ToImmutableArray();

        SubsidiaryForeignKeys = foreignKeys
            .Where(table.IsPrimaryKeyOf)
            .ToImmutableHashSet();

        SubsidiaryMultiStringKeys = SubsidiaryForeignKeys.Select(fk => fk.PrimaryKeyColumns.OrderBy(c => c))
            .Select(parts => new MultiStringKey(table, parts)).ToImmutableHashSet();

        if (PrincipalForeignKeys.Length > 1 && !IsMultiTablePrincipal)
        {
            throw new InvalidDataException(
                $"Error computing key sets for {table}: tables with multiple principal foreign keys must specify multi-table indexes.\n" +
                $"Principal keys found: {string.Join("\n\t", PrincipalForeignKeys)}");
        }

        if (IsMultiTablePrincipal && PrincipalForeignKeys.Any(fk => fk.MultiTableIndexColumnIndices.Length > 1))
        {
            throw new InvalidDataException(
                $"Error computing key sets for {table}: Multi-table principal keys must specify exactly one column per principal");
        }
    }
}