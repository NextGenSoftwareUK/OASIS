using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public interface ISubscriptionUsageRepository
    {
        Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate)> AuthorizeAsync(
            SubscriptionUsageEvent usageEvent, SubscriptionUsagePolicy policy, CancellationToken cancellationToken);
        Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate, bool AlreadySettled)> SettleAsync(
            string userId, UsageSettlementRequest request, CancellationToken cancellationToken);
        Task<SubscriptionUsageAggregate> GetAggregateAsync(string userId, CancellationToken cancellationToken);
        Task<IReadOnlyList<SubscriptionUsageEvent>> GetEventsAsync(string userId, int limit, CancellationToken cancellationToken);
    }

    public sealed class SubscriptionUsageLimitException : System.Exception
    {
        public string Code { get; }
        public SubscriptionUsageLimitException(string code, string message) : base(message) => Code = code;
    }

    public sealed class SubscriptionUsageConflictException : System.Exception
    {
        public SubscriptionUsageConflictException(string message) : base(message) { }
    }
}
