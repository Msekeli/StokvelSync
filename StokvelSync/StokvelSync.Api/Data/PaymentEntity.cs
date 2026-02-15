using Azure;
using Azure.Data.Tables;

namespace StokvelSync.Api.Data;

public class PaymentEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!; // Member Email
    public string RowKey { get; set; } = default!; // Tier_Month (e.g. "100_03")
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public int TierBase { get; set; }
    public int MonthNumber { get; set; }
    public decimal AmountExpected { get; set; }
    public string Status { get; set; } = "Pending";
    public string BlobUrl { get; set; } = string.Empty;
}