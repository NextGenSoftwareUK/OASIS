using System;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using SubscriptionAuthorizationDecision = NextGenSoftware.OASIS.API.Core.Services.Subscriptions.SubscriptionAuthorizationResult;
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
        Task<SubscriptionAuthorizationDecision> AuthorizeUsageAsync(string userId, int karma, UsageAuthorizationRequest request, CancellationToken cancellationToken);
        /// <summary>As above, evaluated against the app's plan when app credentials were verified (the more generous plan applies).</summary>
        Task<SubscriptionAuthorizationDecision> AuthorizeUsageAsync(string userId, int karma, UsageAuthorizationRequest request, AppAccountRecord app, CancellationToken cancellationToken);
        Task<UsageSettlementResult> SettleUsageAsync(string userId, UsageSettlementRequest request, CancellationToken cancellationToken);
        Task<SubscriptionUsageSummary> GetUsageSummaryAsync(string userId, int karma, CancellationToken cancellationToken);
        Task<IReadOnlyList<SubscriptionUsageEvent>> GetUsageEventsAsync(string userId, int limit, CancellationToken cancellationToken);

        // Orders
        Task<List<OrderRecord>> GetOrdersAsync(string userId);
        Task AddOrderAsync(OrderRecord order);
    }

}
