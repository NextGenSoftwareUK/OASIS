using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Server-owned temporary subscription classification and demo allowance.</summary>
public static class SandboxSubscriptionRules
{
    public const int DailyDemoCalls = 200;

    // These fields are issued by SandboxSubscriptionLifecycle, never taken from
    // query bodies, avatar metadata, role hints or a client-supplied plan.
    public static bool IsIssuedFreeSubscription(SubscriptionRecord record) =>
        record != null && record.SandboxExpiresAtUtc.HasValue && record.CurrentPeriodEnd == record.SandboxExpiresAtUtc &&
        record.PlanId == "free" && record.Status is "free" or "active" && !record.PayAsYouGoEnabled &&
        record.StripeCustomerId == null && record.StripeSubscriptionId == null;

    public static SubscriptionUsagePolicy EffectivePolicy(SubscriptionUsagePolicy ordinary,
        SubscriptionRecord subscription, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(ordinary);
        if (!IsIssuedFreeSubscription(subscription) || subscription.CreatedAt == default ||
            subscription.CurrentPeriodStart != subscription.CreatedAt || subscription.CreatedAt > utcNow ||
            subscription.SandboxExpiresAtUtc <= utcNow) return ordinary;

        // Return a captured policy, without mutating the shared ordinary plan
        // or resetting this avatar's usage, subscription lifetime or balance.
        return new SubscriptionUsagePolicy
        {
            MonthlyRequestLimit = ordinary.MonthlyRequestLimit,
            DailyCallLimit = DailyDemoCalls,
            DailyTokenLimit = ordinary.DailyTokenLimit,
            MonthlyBudgetUsd = ordinary.MonthlyBudgetUsd
        };
    }
}
