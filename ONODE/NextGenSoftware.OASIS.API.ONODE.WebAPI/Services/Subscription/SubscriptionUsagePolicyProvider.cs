using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public interface ISubscriptionUsagePolicyProvider
    {
        SubscriptionUsagePolicy GetPolicy(string planId, int karma);
    }
    public sealed class SubscriptionUsagePolicyProvider : ISubscriptionUsagePolicyProvider
    {
        public SubscriptionUsagePolicy GetPolicy(string planId, int karma)
        {
            var (monthly, daily, tokens, budget) = planId?.ToLowerInvariant() switch {
                "free" => (1_000, 20, 50_000L, 1m), "bronze" => (10_000, 100, 250_000L, 10m),
                "silver" => (100_000, 500, 1_000_000L, 50m), "gold" => (1_000_000, 2_000, 5_000_000L, 250m),
                "enterprise" => (-1, 0, 0L, 0m), _ => throw new InvalidOperationException("The subscription plan is not recognized by WEB4.")
            };
            if (daily > 0) daily = checked((int)(daily * KarmaMultiplier(karma)));
            return new SubscriptionUsagePolicy { MonthlyRequestLimit = monthly, DailyCallLimit = daily, DailyTokenLimit = tokens, MonthlyBudgetUsd = budget };
        }
        public static decimal KarmaMultiplier(int karma) => karma switch {
            >= 100_000 => 10m, >= 20_000 => 5m, >= 5_000 => 3m, >= 1_000 => 2m, >= 500 => 1.5m, _ => 1m
        };
    }
}
