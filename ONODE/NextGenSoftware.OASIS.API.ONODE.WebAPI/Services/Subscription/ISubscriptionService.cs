using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}
