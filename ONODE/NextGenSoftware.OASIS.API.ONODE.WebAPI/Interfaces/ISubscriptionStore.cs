using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.SubscriptionStore;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    /// <summary>Persistence for subscriptions, credits balance, and API usage (e.g. MongoDB).</summary>
    public interface ISubscriptionStore
    {
        bool IsConfigured { get; }
        Task<SubscriptionRecord> GetSubscriptionAsync(string avatarId);
        Task<SubscriptionRecord> GetSubscriptionByStripeSubscriptionIdAsync(string stripeSubscriptionId);
        Task UpsertSubscriptionAsync(SubscriptionRecord record);

        Task<decimal> GetCreditsBalanceAsync(string avatarId);
        Task AddCreditsAsync(string avatarId, decimal amountUsd);
        Task<bool> DeductCreditsAsync(string avatarId, decimal amountUsd);

        Task<int> GetUsageAsync(string avatarId, DateTime periodStartUtc);
        Task<int> IncrementUsageAsync(string avatarId, DateTime periodStartUtc);
    }
}
