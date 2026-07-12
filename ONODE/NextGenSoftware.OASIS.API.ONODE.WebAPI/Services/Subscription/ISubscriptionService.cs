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

        // Orders
        Task<List<OrderRecord>> GetOrdersAsync(string userId);
        Task AddOrderAsync(OrderRecord order);
    }
}
