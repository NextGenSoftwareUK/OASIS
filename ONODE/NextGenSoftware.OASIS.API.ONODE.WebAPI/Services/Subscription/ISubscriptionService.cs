using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public interface ISubscriptionService
    {
        // Subscription records
        Task<SubscriptionRecord> GetSubscriptionAsync(string userId);
        Task<SubscriptionRecord> GetSubscriptionByStripeCustomerIdAsync(string stripeCustomerId);
        Task<SubscriptionRecord> GetSubscriptionByStripeSubscriptionIdAsync(string stripeSubscriptionId);
        Task UpsertSubscriptionAsync(SubscriptionRecord record);
        Task SetPayAsYouGoAsync(string userId, bool enabled);

        // Usage tracking
        Task<UsageRecord> GetUsageAsync(string userId, int year, int month);
        Task IncrementUsageAsync(string userId);
        Task IncrementOverageAsync(string userId);
        Task<SubscriptionAuthorizationDecision> AuthorizeAndIncrementRequestAsync(string userId, string consumingService);
        Task<SubscriptionAuthorizationDecision> AuthorizeUsageAsync(string userId, int karma, UsageAuthorizationRequest request, CancellationToken cancellationToken);
        Task<UsageSettlementResult> SettleUsageAsync(string userId, UsageSettlementRequest request, CancellationToken cancellationToken);
        Task<SubscriptionUsageSummary> GetUsageSummaryAsync(string userId, int karma, CancellationToken cancellationToken);
        Task<IReadOnlyList<SubscriptionUsageEvent>> GetUsageEventsAsync(string userId, int limit, CancellationToken cancellationToken);

        // Orders
        Task<List<OrderRecord>> GetOrdersAsync(string userId);
        Task AddOrderAsync(OrderRecord order);
    }

    public class SubscriptionAuthorizationDecision
    {
        public bool Allowed { get; set; }
        public int StatusCode { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string PlanId { get; set; }
        public long CurrentUsage { get; set; }
        public int Limit { get; set; }
        public long Remaining { get; set; }
        public string OperationId { get; set; }
        public int Karma { get; set; }
        public int DailyCallLimit { get; set; }
        public long DailyCallsRemaining { get; set; }
        public long DailyTokenLimit { get; set; }
        public long DailyTokensRemaining { get; set; }
        public decimal MonthlyBudgetUsd { get; set; }
        public decimal MonthlyBudgetRemainingUsd { get; set; }
    }
}
