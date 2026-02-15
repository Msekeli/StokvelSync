using Azure;
using Azure.Data.Tables;

namespace StokvelSync.Api.Data;

public class MemberEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string ActiveTiers { get; set; } = string.Empty;
    
    // Ensure these are double to match the (double) cast in the Repository
    public double TotalContribution { get; set; }
    public double PenaltyBalance { get; set; }
    public bool IsAdmin { get; set; }
}