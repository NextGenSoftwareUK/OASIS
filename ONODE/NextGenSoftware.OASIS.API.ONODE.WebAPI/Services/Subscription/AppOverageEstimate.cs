using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class AppOverageEstimateResult
    {
        /// <summary>Requests above the pool's included monthly allowance.</summary>
        public long OverRequests { get; set; }
        public decimal RatePerRequestUsd { get; set; }
        /// <summary>Requests x rate, before the owner's monthly cap.</summary>
        public decimal GrossUsd { get; set; }
        /// <summary>What would be billed: the gross amount limited to the owner's monthly cap.</summary>
        public decimal ChargedUsd { get; set; }
        public bool CapReached { get; set; }
        /// <summary>Requests the cap would still pay for before the pool stops (0 when no cap or no rate).</summary>
        public long RequestsLeftUnderCap { get; set; }
    }

    /// <summary>Pure maths for an app pool's pay-as-you-go estimate. Display only: nothing is charged from this.</summary>
    public static class AppOverageEstimate
    {
        public static AppOverageEstimateResult Compute(long monthlyRequests, long includedLimit, decimal ratePerRequestUsd, decimal monthlyCapUsd)
        {
            var result = new AppOverageEstimateResult { RatePerRequestUsd = Math.Max(0, ratePerRequestUsd) };
            if (includedLimit <= 0 || ratePerRequestUsd <= 0) return result; // unlimited plan or no price: nothing can be owed
            result.OverRequests = Math.Max(0, monthlyRequests - includedLimit);
            result.GrossUsd = decimal.Round(result.OverRequests * ratePerRequestUsd, 6);
            var cap = Math.Max(0, monthlyCapUsd);
            result.ChargedUsd = cap > 0 ? Math.Min(result.GrossUsd, cap) : result.GrossUsd;
            result.CapReached = cap > 0 && result.GrossUsd >= cap;
            result.RequestsLeftUnderCap = cap > 0 ? (long)Math.Max(0, Math.Floor((cap - result.ChargedUsd) / ratePerRequestUsd)) : 0;
            return result;
        }
    }
}
