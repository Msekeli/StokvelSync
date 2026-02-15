using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StokvelSync.Api.Data;
using StokvelSync.Api.Services;
using StokvelSync.Shared;

namespace StokvelSync.Api.Functions;

public class PenaltyRoutine
{
    private readonly MemberRepository _repository;
    private readonly PenaltyService _penaltyService;

    public PenaltyRoutine(MemberRepository repository, PenaltyService penaltyService)
    {
        _repository = repository;
        _penaltyService = penaltyService;
    }

    /// <summary>
    /// Business Rule: R50 fine applied if no contribution by the 7th.
    /// Cron Expression: 0 0 0 8 * * (Runs at midnight on the 8th of every month)
    /// </summary>
  [Function("ApplyPenalties")]
public async Task Run([TimerTrigger("0 0 0 1 * *")] TimerInfo myTimer, FunctionContext context)
{
    var logger = context.GetLogger("PenaltyRoutine");
    var members = await _repository.GetAllMembersAsync();

    foreach (var member in members)
    {
        // 1. Check for Missed Month (Total lack of payment)
        if (!member.HasPaidCurrentMonth && member.TotalContribution == 0)
        {
            member.PenaltyBalance += 100; // Missed month penalty
            logger.LogInformation($"R100 Penalty: {member.Email} missed the month.");
        }
        // 2. Check for Late Payment (Paid after 7th but before end of month)
        else if (!member.HasPaidCurrentMonth)
        {
            member.PenaltyBalance += 50; // Late fee
            logger.LogInformation($"R50 Penalty: {member.Email} was late.");
        }

        // Reset payment status for the new month
        member.HasPaidCurrentMonth = false;
        await _repository.UpsertMemberAsync(member);
    }
}
}