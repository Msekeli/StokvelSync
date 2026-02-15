using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using StokvelSync.Api.Data;

namespace StokvelSync.Api.Functions;

public class PenaltyRoutine
{
    private readonly ILogger _logger;
    private readonly TableClient _memberTable;
    private readonly TableClient _paymentTable;

    public PenaltyRoutine(ILoggerFactory loggerFactory, TableServiceClient tableService)
    {
        _logger = loggerFactory.CreateLogger<PenaltyRoutine>();
        _memberTable = tableService.GetTableClient("Members");
        _paymentTable = tableService.GetTableClient("Payments");
    }

    [Function("PenaltyRoutine")]
    public async Task Run([TimerTrigger("0 0 0 8 * *")] TimerInfo myTimer)
    {
        var members = _memberTable.Query<MemberEntity>();
        int currentMonth = DateTime.UtcNow.Month;

        foreach (var member in members)
        {
            // Logic: Tiers are stored as "50,100"
            var tiers = member.ActiveTiers.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var tier in tiers)
            {
                string rowKey = $"{tier}_{currentMonth:D2}";
                var payment = _paymentTable.Query<PaymentEntity>(p => 
                    p.PartitionKey == member.RowKey && p.RowKey == rowKey).FirstOrDefault();

                // Logic: Apply R50 late penalty if not "Approved" or "Pending" by the 8th
                if (payment == null || (payment.Status != "Approved" && payment.Status != "Pending"))
                {
                    member.PenaltyBalance += 50;
                    _logger.LogInformation($"Penalty of R50 applied to {member.RowKey} for tier {tier}");
                }
            }
            await _memberTable.UpdateEntityAsync(member, member.ETag);
        }
    }
}