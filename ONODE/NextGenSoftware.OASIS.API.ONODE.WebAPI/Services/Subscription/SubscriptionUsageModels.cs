using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class UsageAuthorizationRequest
    {
        public string OperationId { get; set; }
        public string ConsumingService { get; set; }
        public string Endpoint { get; set; }
        public string MeterCategory { get; set; }
        public long RequestedUnits { get; set; } = 1;
        public decimal EstimatedCostUsd { get; set; }
    }

    public sealed class UsageSettlementRequest
    {
        public string OperationId { get; set; }
        public string Outcome { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public long PromptTokens { get; set; }
        public long CompletionTokens { get; set; }
        public long Units { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public decimal? ActualCostUsd { get; set; }
        public string CostSource { get; set; }
        public string PricingCatalogueVersion { get; set; }
    }

    public sealed class UsageSettlementResult
    {
        public bool Settled { get; set; }
        public bool AlreadySettled { get; set; }
        public string OperationId { get; set; }
        public string Message { get; set; }
    }

    public sealed class SubscriptionUsageSummary
    {
        public string UserId { get; set; }
        public string PlanId { get; set; }
        public int Karma { get; set; }
        public long MonthlyRequests { get; set; }
        public long DailyCalls { get; set; }
        public long DailyTokens { get; set; }
        public decimal MonthlySpendUsd { get; set; }
        public int DailyCallLimit { get; set; }
        public long DailyTokenLimit { get; set; }
        public decimal MonthlyBudgetUsd { get; set; }
        public long DailyCallsRemaining { get; set; }
        public long DailyTokensRemaining { get; set; }
        public decimal MonthlyBudgetRemainingUsd { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public class SubscriptionUsageEvent
    {
        public string OperationId { get; set; }
        public string UserId { get; set; }
        public string ConsumingService { get; set; }
        public string Endpoint { get; set; }
        public string MeterCategory { get; set; }
        public string Status { get; set; }
        public string Outcome { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public long RequestedUnits { get; set; }
        public long Units { get; set; }
        public long PromptTokens { get; set; }
        public long CompletionTokens { get; set; }
        public decimal ReservedCostUsd { get; set; }
        public decimal SettledCostUsd { get; set; }
        public string CostSource { get; set; }
        public string PricingCatalogueVersion { get; set; }
        public DateTime AuthorizedAtUtc { get; set; }
        public DateTime? SettledAtUtc { get; set; }
    }

    public class SubscriptionUsageAggregate
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Month { get; set; }
        public string Day { get; set; }
        public long MonthlyRequests { get; set; }
        public long DailyCalls { get; set; }
        public long DailyTokens { get; set; }
        public decimal ReservedCostUsd { get; set; }
        public decimal SettledCostUsd { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public sealed class SubscriptionUsagePolicy
    {
        public int MonthlyRequestLimit { get; set; }
        public int DailyCallLimit { get; set; }
        public long DailyTokenLimit { get; set; }
        public decimal MonthlyBudgetUsd { get; set; }
    }

    public static class SubscriptionUsagePolicyEvaluator
    {
        public static void EnsureAuthorized(SubscriptionUsagePolicy policy, SubscriptionUsageAggregate aggregate, long requestedUnits, decimal estimatedCostUsd)
        {
            if (requestedUnits < 0) throw new ArgumentOutOfRangeException(nameof(requestedUnits));
            if (estimatedCostUsd < 0) throw new ArgumentOutOfRangeException(nameof(estimatedCostUsd));
            if (policy.MonthlyRequestLimit >= 0 && aggregate.MonthlyRequests >= policy.MonthlyRequestLimit)
                throw new SubscriptionUsageLimitException("MONTHLY_REQUEST_LIMIT_EXCEEDED", "The monthly request limit has been reached.");
            if (policy.DailyCallLimit > 0 && aggregate.DailyCalls >= policy.DailyCallLimit)
                throw new SubscriptionUsageLimitException("DAILY_CALL_LIMIT_EXCEEDED", "The daily call limit has been reached.");
            if (policy.DailyTokenLimit > 0 && aggregate.DailyTokens + requestedUnits > policy.DailyTokenLimit)
                throw new SubscriptionUsageLimitException("DAILY_TOKEN_LIMIT_EXCEEDED", "The daily token limit would be exceeded.");
            if (policy.MonthlyBudgetUsd > 0 && aggregate.ReservedCostUsd + aggregate.SettledCostUsd + estimatedCostUsd > policy.MonthlyBudgetUsd)
                throw new SubscriptionUsageLimitException("MONTHLY_BUDGET_EXCEEDED", "The monthly AI budget would be exceeded.");
        }
    }
}
