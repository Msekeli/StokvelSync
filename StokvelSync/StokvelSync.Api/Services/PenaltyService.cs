using StokvelSync.Shared;

namespace StokvelSync.Api.Services;

public class PenaltyService
{
    public void CheckAndApplyPenalties(Member member, DateTime currentDate)
    {
        // Rule 1: The 7th Rule (R50 Fine)
        if (currentDate.Day > 7 && !member.HasPaidCurrentMonth)
        {
            member.PenaltyBalance += 50;
        }

        // Rule 2: Missed Month (R100 Fine)
        // This would typically be checked at the very end of the month
        if (currentDate.Day == DateTime.DaysInMonth(currentDate.Year, currentDate.Month) 
            && !member.HasPaidCurrentMonth)
        {
            member.PenaltyBalance += 100;
        }
    }
}