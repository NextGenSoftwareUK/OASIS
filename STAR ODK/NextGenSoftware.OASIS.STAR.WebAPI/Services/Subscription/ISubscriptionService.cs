using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Services.Subscription
{
    public interface ISubscriptionService
    {
        Task<SubscriptionRecord> GetSubscriptionAsync(string userId);
        Task<UsageRecord> GetUsageAsync(string userId, int year, int month);
        Task IncrementUsageAsync(string userId);
        Task IncrementOverageAsync(string userId);
    }
}
