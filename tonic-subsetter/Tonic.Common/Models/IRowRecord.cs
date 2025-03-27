namespace Tonic.Common.Models;

public interface IRowRecord
{
    string HostCategory { get; set; }
    string TableName { get; set; }
    string ColumnName { get; set; }
    string OriginalValue { get; set; }
    string NewValue { get; set; }
    string BatchId { get; set; }
    bool Deleted { get; set; }
    string RowKey { get; set; }
    bool Equals(object? obj);
    int GetHashCode();
}