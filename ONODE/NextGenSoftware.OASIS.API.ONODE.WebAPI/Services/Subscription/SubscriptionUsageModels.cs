using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class SubscriptionUsageSummary
    {
        public string UserId { get; set; }
        public string PlanId { get; set; }
        public int Karma { get; set; }
        public long MonthlyRequests { get; set; }
        public long DailyCalls { get; set; }
        public long DailyTokens { get; set; }
        public long ReservedTokens { get; set; }
        public decimal MonthlySpendUsd { get; set; }
        public int DailyCallLimit { get; set; }
        public long DailyTokenLimit { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal MonthlyBudgetUsd { get; set; }
        public long DailyCallsRemaining { get; set; }
        public long DailyTokensRemaining { get; set; }
        public decimal MonthlyBudgetRemainingUsd { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SubscriptionUsageEvent
    {
        public string OperationId { get; set; }
        public string UserId { get; set; }
        public string ConsumingService { get; set; }
        public string Endpoint { get; set; }
        public string MeterCategory { get; set; }
        public string RequestFingerprint { get; set; }
        public string PlanId { get; set; }
        public int Karma { get; set; }
        public SubscriptionUsagePolicy AuthorizationPolicy { get; set; }
        public string Status { get; set; }
        public string Outcome { get; set; }
        public string Provider { get; set; }
        public string ProviderRequestId { get; set; }
        public string Model { get; set; }
        public long RequestedUnits { get; set; }
        public long Units { get; set; }
        public long PromptTokens { get; set; }
        public long CompletionTokens { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal ReservedCostUsd { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal SettledCostUsd { get; set; }
        public string CostSource { get; set; }
        public string PricingCatalogueVersion { get; set; }
        public string SettlementFingerprint { get; set; }
        public DateTime AuthorizedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? SettledAtUtc { get; set; }
        public string ProviderReceiptsJson { get; set; }
        [BsonIgnore] public bool AlreadyAuthorized { get; set; }
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
        public long ReservedUnits { get; set; }
        public decimal ReservedCostUsd { get; set; }
        public decimal SettledCostUsd { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public sealed class SubscriptionUsageBucket
    {
        [BsonId] public string Id { get; set; }
        public string UserId { get; set; }
        public string PeriodType { get; set; }
        public string Period { get; set; }
        public long Requests { get; set; }
        public long ReservedUnits { get; set; }
        public long SettledUnits { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal ReservedCostUsd { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal SettledCostUsd { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    /// <summary>Append-only financial history; corrections append signed deltas without changing existing entries.</summary>
    public sealed class SubscriptionUsageAuditEntry
    {
        [BsonId] public string Id { get; set; }
        public string OperationId { get; set; }
        public string UserId { get; set; }
        public string ConsumingService { get; set; }
        public string Kind { get; set; }
        public string Actor { get; set; }
        public string Reason { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public string Month { get; set; }
        public string Day { get; set; }
        public long RequestsDelta { get; set; }
        public long ReservedUnitsDelta { get; set; }
        public long SettledUnitsDelta { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal ReservedCostDeltaUsd { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal SettledCostDeltaUsd { get; set; }
        public string PayloadJson { get; set; }
        public string ExternalReference { get; set; }
        public string CorrectionKind { get; set; }
        public string Provider { get; set; }
        public string ProviderRequestId { get; set; }
    }

    public sealed class UsageCorrectionRequest
    {
        public string CorrectionKind { get; set; }
        public string Provider { get; set; }
        public string ProviderRequestId { get; set; }
        public string CorrectionId { get; set; }
        public string OperationId { get; set; }
        public string Reason { get; set; }
        public string EvidenceReference { get; set; }
        public decimal CostDeltaUsd { get; set; }
        public long UnitsDelta { get; set; }
    }

    public sealed class SubscriptionUsagePolicy
    {
        public int MonthlyRequestLimit { get; set; }
        public int DailyCallLimit { get; set; }
        public long DailyTokenLimit { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public decimal MonthlyBudgetUsd { get; set; }
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
            if (policy.DailyTokenLimit > 0 && checked(aggregate.DailyTokens + aggregate.ReservedUnits + requestedUnits) > policy.DailyTokenLimit)
                throw new SubscriptionUsageLimitException("DAILY_TOKEN_LIMIT_EXCEEDED", "The daily token limit would be exceeded.");
            if (policy.MonthlyBudgetUsd > 0 && checked(aggregate.ReservedCostUsd + aggregate.SettledCostUsd + estimatedCostUsd) > policy.MonthlyBudgetUsd)
                throw new SubscriptionUsageLimitException("MONTHLY_BUDGET_EXCEEDED", "The monthly usage budget would be exceeded.");
        }
    }
}
