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

            // Return one row per group instead of transferring all source documents to WEB4.
            var eventQuery = _events.Aggregate()
                .Match(x => x.AuthorizedAtUtc >= fromUtc && x.AuthorizedAtUtc < toUtc)
                .Group<BsonDocument>(new BsonDocument
                {
                    { "_id", new BsonDocument { { "provider", OrUnknown("$Provider") }, { "model", OrUnknown("$Model") }, { "status", OrUnknown("$Status") } } },
                    { "count", new BsonDocument("$sum", 1) },
                    { "failed", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$or", new BsonArray
                        {
                            new BsonDocument("$in", new BsonArray { "$Status", new BsonArray { "failed", "expired" } }),
                            new BsonDocument("$eq", new BsonArray { "$Outcome", "failed" })
                        }),
                        1, 0
                    })) },
                    { "units", new BsonDocument("$sum", "$Units") },
                    { "cost", new BsonDocument("$sum", "$SettledCostUsd") }
                }).ToListAsync(ct);
            var invoiceQuery = Orders.Aggregate()
                .Match(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc && x.Status == "paid")
                .Group<BsonDocument>(new BsonDocument
                {
                    { "_id", new BsonDocument("$ifNull", new BsonArray { "$Currency", "USD" }) },
                    { "amount", new BsonDocument("$sum", "$Amount") }
                }).ToListAsync(ct);
            var subscriptionQuery = Subscriptions.Aggregate()
                .Group<BsonDocument>(new BsonDocument
                {
                    { "_id", new BsonDocument { { "plan", OrUnknown("$PlanId") }, { "status", OrUnknown("$Status") } } },
                    { "count", new BsonDocument("$sum", 1) }
                }).ToListAsync(ct);

            await Task.WhenAll(eventQuery, invoiceQuery, subscriptionQuery);
            var events = await eventQuery;
            var subscriptions = await subscriptionQuery;
            var revenueByCurrency = (await invoiceQuery).GroupBy(row => row["_id"].AsString.ToUpperInvariant())
                .ToDictionary(group => group.Key, group => group.Sum(row => row["amount"].ToDecimal()));
            return new SubscriptionAdminSnapshot
            {
                FromUtc = fromUtc, ToUtc = toUtc,
                Customers = subscriptions.Sum(row => row["count"].ToInt64()),
                ActiveSubscriptions = subscriptions.Where(row => row["_id"]["status"].AsString is "active" or "trialing" or "free")
                    .Sum(row => row["count"].ToInt64()),
                UsageOperations = events.Sum(row => row["count"].ToInt64()),
                FailedOperations = events.Sum(row => row["failed"].ToInt64()),
                SettledUnits = events.Sum(row => row["units"].ToInt64()),
                UsageCostUsd = events.Sum(row => row["cost"].ToDecimal()),
                RevenueUsd = revenueByCurrency.GetValueOrDefault("USD"),
                RevenueByCurrency = revenueByCurrency,
                SubscriptionsByPlan = AdminCountsBy(subscriptions, "plan"),
                OperationsByProvider = AdminCountsBy(events, "provider"),
                OperationsByModel = AdminCountsBy(events, "model"),
                OperationsByStatus = AdminCountsBy(events, "status")
            };
        }

        private static BsonDocument OrUnknown(string field) => new BsonDocument("$ifNull", new BsonArray { field, "unknown" });

        private static Dictionary<string, long> AdminCountsBy(IEnumerable<BsonDocument> rows, string field) =>
            rows.GroupBy(row => row["_id"][field].AsString)
                .ToDictionary(group => group.Key, group => group.Sum(row => row["count"].ToInt64()));

        private static SubscriptionAdminPage<T> Page<T>(List<T> items, int take, Func<T, string> cursor)
        {
            bool more = items.Count > take;
            if (more) items.RemoveAt(items.Count - 1);
            return new SubscriptionAdminPage<T> { Items = items, Limit = take, NextCursor = more ? cursor(items[^1]) : null };
        }
    }
}
