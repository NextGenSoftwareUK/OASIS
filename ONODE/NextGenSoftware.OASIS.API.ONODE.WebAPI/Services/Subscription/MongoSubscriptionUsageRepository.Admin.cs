using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository
    {
        public async Task<SubscriptionAdminPage<SubscriptionRecord>> GetAdminSubscriptionsAsync(string cursor, int limit, CancellationToken ct)
        {
            int take = Math.Clamp(limit, 1, 200);
            var filter = string.IsNullOrWhiteSpace(cursor)
                ? Builders<SubscriptionRecord>.Filter.Empty
                : Builders<SubscriptionRecord>.Filter.Gt(x => x.UserId, cursor);
            var items = await Subscriptions.Find(filter).SortBy(x => x.UserId).Limit(take + 1).ToListAsync(ct);
            return Page(items, take, x => x.UserId);
        }

        public async Task<SubscriptionAdminPage<OrderRecord>> GetAdminInvoicesAsync(DateTime? before, int limit, CancellationToken ct)
        {
            int take = Math.Clamp(limit, 1, 200);
            var filter = before.HasValue
                ? Builders<OrderRecord>.Filter.Lt(x => x.CreatedAt, before.Value)
                : Builders<OrderRecord>.Filter.Empty;
            var items = await Orders.Find(filter).SortByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Limit(take + 1).ToListAsync(ct);
            return Page(items, take, x => x.CreatedAt.ToUniversalTime().ToString("O"));
        }

        public async Task<SubscriptionAdminPage<SubscriptionUsageEvent>> GetAdminEventsAsync(DateTime? before, int limit, CancellationToken ct)
        {
            int take = Math.Clamp(limit, 1, 200);
            var filter = before.HasValue
                ? Builders<UsageEventDocument>.Filter.Lt(x => x.AuthorizedAtUtc, before.Value)
                : Builders<UsageEventDocument>.Filter.Empty;
            var items = await _events.Find(filter).SortByDescending(x => x.AuthorizedAtUtc).ThenByDescending(x => x.OperationId).Limit(take + 1).ToListAsync(ct);
            return Page(items.Cast<SubscriptionUsageEvent>().ToList(), take, x => x.AuthorizedAtUtc.ToUniversalTime().ToString("O"));
        }

        public async Task<SubscriptionAdminPage<SubscriptionUsageAuditEntry>> GetAdminAuditAsync(DateTime? before, int limit, CancellationToken ct)
        {
            int take = Math.Clamp(limit, 1, 200);
            var filter = before.HasValue
                ? Builders<SubscriptionUsageAuditEntry>.Filter.Lt(x => x.OccurredAtUtc, before.Value)
                : Builders<SubscriptionUsageAuditEntry>.Filter.Empty;
            var items = await _audit.Find(filter).SortByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id).Limit(take + 1).ToListAsync(ct);
            return Page(items, take, x => x.OccurredAtUtc.ToUniversalTime().ToString("O"));
        }

        public async Task<SubscriptionAdminSnapshot> GetAdminAnalyticsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
        {
            if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc || fromUtc >= toUtc || toUtc - fromUtc > TimeSpan.FromDays(366))
                throw new ArgumentException("A UTC analytics range of at most 366 days is required.");

            var events = await _events.Find(x => x.AuthorizedAtUtc >= fromUtc && x.AuthorizedAtUtc < toUtc).ToListAsync(ct);
            var invoices = await Orders.Find(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc && x.Status == "paid").ToListAsync(ct);
            var subscriptions = await Subscriptions.Find(Builders<SubscriptionRecord>.Filter.Empty).ToListAsync(ct);
            return new SubscriptionAdminSnapshot
            {
                FromUtc = fromUtc, ToUtc = toUtc,
                Customers = subscriptions.LongCount(),
                ActiveSubscriptions = subscriptions.LongCount(x => x.Status is "active" or "trialing" or "free"),
                UsageOperations = events.LongCount(),
                FailedOperations = events.LongCount(x => x.Status is "failed" or "expired" || x.Outcome == "failed"),
                SettledUnits = events.Sum(x => x.Units),
                UsageCostUsd = events.Sum(x => x.SettledCostUsd),
                RevenueUsd = invoices.Sum(x => x.Amount),
                SubscriptionsByPlan = subscriptions.GroupBy(x => x.PlanId ?? "unknown").ToDictionary(x => x.Key, x => x.LongCount()),
                OperationsByProvider = events.GroupBy(x => x.Provider ?? "unknown").ToDictionary(x => x.Key, x => x.LongCount()),
                OperationsByModel = events.GroupBy(x => x.Model ?? "unknown").ToDictionary(x => x.Key, x => x.LongCount()),
                OperationsByStatus = events.GroupBy(x => x.Status ?? "unknown").ToDictionary(x => x.Key, x => x.LongCount())
            };
        }

        private static SubscriptionAdminPage<T> Page<T>(List<T> items, int take, Func<T, string> cursor)
        {
            bool more = items.Count > take;
            if (more) items.RemoveAt(items.Count - 1);
            return new SubscriptionAdminPage<T> { Items = items, Limit = take, NextCursor = more ? cursor(items[^1]) : null };
        }
    }
}
