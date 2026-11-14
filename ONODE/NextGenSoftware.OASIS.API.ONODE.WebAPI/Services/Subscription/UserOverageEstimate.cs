using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class UserOverageEstimateResult
    {
        public long MonthRequests { get; set; }
        public long IncludedLimit { get; set; }
        public decimal RatePerRequestUsd { get; set; }
        /// <summary>Request charges so far this month (requests above the allowance x rate, limited to the cap).</summary>
        public AppOverageEstimateResult SoFar { get; set; }
        /// <summary>Requests expected by month end if the pace so far continues.</summary>
        public long ProjectedMonthRequests { get; set; }
        public AppOverageEstimateResult Projected { get; set; }
        public int DayOfMonth { get; set; }
        public int DaysInMonth { get; set; }
    }

    /// <summary>Pure maths for the Usage and billing estimate. Request charges only: AI cost above the included budget is not estimated.</summary>
    public static class UserOverageEstimate
    {
        public static UserOverageEstimateResult Compute(long monthRequests, long includedLimit, decimal ratePerRequestUsd, decimal monthlyCapUsd, DateTime nowUtc)
        {
            var days = DateTime.DaysInMonth(nowUtc.Year, nowUtc.Month);
            var day = Math.Max(1, nowUtc.Day);
            // Pace so far (requests per elapsed day, counting today as a full day) carried to the end of the month.
            var projected = day >= days ? monthRequests : (long)Math.Ceiling(monthRequests / (double)day * days);
            return new UserOverageEstimateResult
            {
                MonthRequests = monthRequests, IncludedLimit = includedLimit, RatePerRequestUsd = Math.Max(0, ratePerRequestUsd),
                SoFar = AppOverageEstimate.Compute(monthRequests, includedLimit, ratePerRequestUsd, monthlyCapUsd),
                ProjectedMonthRequests = projected,
                Projected = AppOverageEstimate.Compute(projected, includedLimit, ratePerRequestUsd, monthlyCapUsd),
                DayOfMonth = day, DaysInMonth = days
            };
        }
    }
}
