using Azure;
using Azure.Data.Tables;

namespace StokvelSync.Api.Data;

public class MemberEntity : ITableEntity
{
    // PartitionKey = "StokvelGroup" or "General"
    public string PartitionKey { get; set; } = default!;
    // RowKey = Member's Email address
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom Properties
    public string ActiveTiers { get; set; } = string.Empty; // Store as comma-separated string
    public decimal TotalContribution { get; set; }
    public decimal PenaltyBalance { get; set; }
    public bool HasPaidCurrentMonth { get; set; }
}