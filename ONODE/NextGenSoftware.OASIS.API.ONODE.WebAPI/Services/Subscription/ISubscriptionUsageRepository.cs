using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public interface ISubscriptionUsageRepository
    {
        Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate)> AuthorizeAsync(
            SubscriptionUsageEvent usageEvent, int karma, CancellationToken cancellationToken);
        Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate, bool AlreadySettled)> SettleAsync(
            string userId, UsageSettlementRequest request, CancellationToken cancellationToken);
        Task<UsageStartResult> StartAsync(UsageStartRequest request, CancellationToken cancellationToken);
        Task<SubscriptionUsageAggregate> GetAggregateAsync(string userId, CancellationToken cancellationToken);
        Task<SubscriptionUsageAggregate> GetAggregateForPeriodAsync(string userId, DateTime period, CancellationToken cancellationToken);
        Task<IReadOnlyList<SubscriptionUsageEvent>> GetEventsAsync(string userId, int limit, CancellationToken cancellationToken);
        Task<IReadOnlyList<SubscriptionUsageAuditEntry>> GetAuditAsync(string userId, string beforeId, int limit, CancellationToken cancellationToken);
        Task<SubscriptionUsageAuditEntry> CorrectAsync(string actor, UsageCorrectionRequest correction, CancellationToken cancellationToken);
        Task<UsageMigrationInventory> GetMigrationInventoryAsync(CancellationToken cancellationToken);
        Task<string> ImportOpeningBalanceAsync(string actor, UsageOpeningBalanceManifest manifest, CancellationToken cancellationToken);
        Task<int> ExpireAsync(DateTime now, int batchSize, CancellationToken cancellationToken);
    }

    public sealed class SubscriptionUsageLimitException : Exception
    {
        public string Code { get; }
        public int StatusCode { get; }
        public SubscriptionUsageLimitException(string code, string message, int statusCode = 429) : base(message) { Code = code; StatusCode = statusCode; }
    }

    public sealed class SubscriptionUsageConflictException : Exception
    {
        public SubscriptionUsageConflictException(string message) : base(message) { }
    }
}
