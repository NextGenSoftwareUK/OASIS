using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public interface ISubscriptionBillingRepository
    {
        Task<SubscriptionRecord> GetSubscriptionAsync(string userId, CancellationToken cancellationToken = default);
        Task<SubscriptionRecord> FindSubscriptionAsync(string stripeId, bool customer, CancellationToken cancellationToken = default);
        Task SaveSubscriptionAsync(SubscriptionRecord record, CancellationToken cancellationToken = default);
        Task SetPayAsYouGoAsync(string userId, bool enabled, CancellationToken cancellationToken = default);
        Task<List<OrderRecord>> GetOrdersAsync(string userId, CancellationToken cancellationToken = default);
        Task AddOrderAsync(OrderRecord order, CancellationToken cancellationToken = default);
        Task<bool> ApplyStripeEventAsync(string eventId, string sourceFingerprint, string userId, DateTime eventCreatedAtUtc,
            Func<CancellationToken, Task<SubscriptionRecord>> fetchCanonicalSubscription, OrderRecord invoice, CancellationToken cancellationToken = default);
    }
}
