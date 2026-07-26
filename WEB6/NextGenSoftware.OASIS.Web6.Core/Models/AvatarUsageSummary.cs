using System;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    public class AvatarUsageSummary
    {
        public Guid   AvatarId          { get; set; }
        public string PeriodMonth       { get; set; }
        public string PeriodDay         { get; set; }
        public double MonthlySpendUSD   { get; set; }
        public double MonthlyBudgetUSD  { get; set; }
        public int    DailyTokensUsed   { get; set; }
        public int    DailyTokenLimit   { get; set; }
        public double RemainingBudgetUSD => MonthlyBudgetUSD > 0 ? Math.Max(0, MonthlyBudgetUSD - MonthlySpendUSD) : -1;
        public int    RemainingTokensToday => DailyTokenLimit > 0 ? Math.Max(0, DailyTokenLimit - DailyTokensUsed) : -1;
    }
}
