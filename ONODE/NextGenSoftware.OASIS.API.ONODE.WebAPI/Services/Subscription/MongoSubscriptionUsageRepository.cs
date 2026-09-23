using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// Transactional usage ledger. The unique operation id and aggregate update are committed in
    /// one MongoDB transaction, making authorization and settlement safe across WEB4 replicas.
    /// </summary>
    public sealed class MongoSubscriptionUsageRepository : ISubscriptionUsageRepository
    {
        private readonly IMongoClient _client;
        private readonly IMongoCollection<UsageEventDocument> _events;
        private readonly IMongoCollection<UsageAggregateDocument> _aggregates;

        public MongoSubscriptionUsageRepository(IConfiguration configuration)
        {
            string connectionString = configuration["SUBSCRIPTION_MONGODB_CONNECTION_STRING"]
                ?? Environment.GetEnvironmentVariable("SUBSCRIPTION_MONGODB_CONNECTION_STRING")
                ?? NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.StorageProviders?.MongoDBOASIS?.ConnectionString;
            string databaseName = configuration["SUBSCRIPTION_MONGODB_DATABASE"]
                ?? Environment.GetEnvironmentVariable("SUBSCRIPTION_MONGODB_DATABASE")
                ?? NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.StorageProviders?.MongoDBOASIS?.DBName;

            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("WEB4 subscription usage requires SUBSCRIPTION_MONGODB_CONNECTION_STRING and SUBSCRIPTION_MONGODB_DATABASE (or the matching MongoDBOASIS DNA settings).");

            _client = new MongoClient(connectionString);
            var database = _client.GetDatabase(databaseName);
            _events = database.GetCollection<UsageEventDocument>("subscription_usage_events");
            _aggregates = database.GetCollection<UsageAggregateDocument>("subscription_usage_aggregates");

            _events.Indexes.CreateOne(new CreateIndexModel<UsageEventDocument>(
                Builders<UsageEventDocument>.IndexKeys.Ascending(x => x.OperationId),
                new CreateIndexOptions { Unique = true, Name = "ux_operation_id" }));
            _events.Indexes.CreateOne(new CreateIndexModel<UsageEventDocument>(
                Builders<UsageEventDocument>.IndexKeys.Ascending(x => x.UserId).Descending(x => x.AuthorizedAtUtc),
                new CreateIndexOptions { Name = "ix_user_authorized" }));
        }

        public async Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate)> AuthorizeAsync(
            SubscriptionUsageEvent usageEvent, SubscriptionUsagePolicy policy, CancellationToken cancellationToken)
        {
            using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            return await session.WithTransactionAsync(async (transaction, ct) =>
            {
                var existing = await _events.Find(transaction, x => x.OperationId == usageEvent.OperationId).FirstOrDefaultAsync(ct);
                if (existing != null)
                {
                    if (!string.Equals(existing.UserId, usageEvent.UserId, StringComparison.Ordinal))
                        throw new SubscriptionUsageConflictException("The operation id belongs to another avatar.");
                    if (!SameAuthorization(existing, usageEvent))
                        throw new SubscriptionUsageConflictException("The operation id was already used for a different authorization payload.");
                    var existingAggregate = await LoadAggregateAsync(transaction, usageEvent.UserId, usageEvent.AuthorizedAtUtc, ct);
                    return (existing.ToModel(), existingAggregate.ToModel());
                }

                var aggregate = await LoadAggregateAsync(transaction, usageEvent.UserId, usageEvent.AuthorizedAtUtc, ct);
                SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy, aggregate.ToModel(), usageEvent.RequestedUnits, usageEvent.ReservedCostUsd);

                aggregate.MonthlyRequests++;
                aggregate.DailyCalls++;
                aggregate.ReservedCostUsd += usageEvent.ReservedCostUsd;
                aggregate.UpdatedAtUtc = usageEvent.AuthorizedAtUtc;

                await _events.InsertOneAsync(transaction, UsageEventDocument.FromModel(usageEvent), cancellationToken: ct);
                await _aggregates.ReplaceOneAsync(transaction, x => x.Id == aggregate.Id, aggregate,
                    new ReplaceOptions { IsUpsert = true }, ct);
                return (usageEvent, aggregate.ToModel());
            }, cancellationToken: cancellationToken);
        }

        public async Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate, bool AlreadySettled)> SettleAsync(
            string userId, UsageSettlementRequest request, CancellationToken cancellationToken)
        {
            using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            return await session.WithTransactionAsync(async (transaction, ct) =>
            {
                var usageEvent = await _events.Find(transaction, x => x.OperationId == request.OperationId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The usage authorization operation was not found.");
                if (!string.Equals(usageEvent.UserId, userId, StringComparison.Ordinal))
                    throw new SubscriptionUsageConflictException("The operation id belongs to another avatar.");
                var aggregate = await LoadAggregateAsync(transaction, userId, usageEvent.AuthorizedAtUtc, ct);
                if (usageEvent.Status == "settled")
                {
                    if (!SameSettlement(usageEvent, request))
                        throw new SubscriptionUsageConflictException("The operation id was already settled with a different payload.");
                    return (usageEvent.ToModel(), aggregate.ToModel(), true);
                }

                decimal settledCost = request.ActualCostUsd ?? request.EstimatedCostUsd;
                aggregate.ReservedCostUsd = Math.Max(0m, aggregate.ReservedCostUsd - usageEvent.ReservedCostUsd);
                aggregate.SettledCostUsd += settledCost;
                aggregate.DailyTokens += checked(request.PromptTokens + request.CompletionTokens);
                aggregate.UpdatedAtUtc = DateTime.UtcNow;

                usageEvent.Status = "settled";
                usageEvent.Outcome = request.Outcome;
                usageEvent.Provider = request.Provider;
                usageEvent.Model = request.Model;
                usageEvent.Units = request.Units;
                usageEvent.PromptTokens = request.PromptTokens;
                usageEvent.CompletionTokens = request.CompletionTokens;
                usageEvent.SettledCostUsd = settledCost;
                usageEvent.CostSource = request.CostSource;
                usageEvent.PricingCatalogueVersion = request.PricingCatalogueVersion;
                usageEvent.SettledAtUtc = aggregate.UpdatedAtUtc;

                await _events.ReplaceOneAsync(transaction, x => x.OperationId == usageEvent.OperationId, usageEvent, cancellationToken: ct);
                await _aggregates.ReplaceOneAsync(transaction, x => x.Id == aggregate.Id, aggregate, cancellationToken: ct);
                return (usageEvent.ToModel(), aggregate.ToModel(), false);
            }, cancellationToken: cancellationToken);
        }

        public async Task<SubscriptionUsageAggregate> GetAggregateAsync(string userId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var doc = await _aggregates.Find(x => x.Id == AggregateId(userId, now)).FirstOrDefaultAsync(cancellationToken);
            doc ??= NewAggregate(userId, now);
            if (!string.Equals(doc.Day, now.ToString("yyyy-MM-dd"), StringComparison.Ordinal))
            {
                doc.Day = now.ToString("yyyy-MM-dd");
                doc.DailyCalls = 0;
                doc.DailyTokens = 0;
            }
            return doc.ToModel();
        }

        public async Task<IReadOnlyList<SubscriptionUsageEvent>> GetEventsAsync(string userId, int limit, CancellationToken cancellationToken) =>
            (await _events.Find(x => x.UserId == userId).SortByDescending(x => x.AuthorizedAtUtc)
                .Limit(Math.Clamp(limit, 1, 500)).ToListAsync(cancellationToken)).Select(x => x.ToModel()).ToList();

        private async Task<UsageAggregateDocument> LoadAggregateAsync(IClientSessionHandle session, string userId, DateTime now, CancellationToken ct)
        {
            var aggregate = await _aggregates.Find(session, x => x.Id == AggregateId(userId, now)).FirstOrDefaultAsync(ct) ?? NewAggregate(userId, now);
            if (!string.Equals(aggregate.Day, now.ToString("yyyy-MM-dd"), StringComparison.Ordinal))
            {
                aggregate.Day = now.ToString("yyyy-MM-dd");
                aggregate.DailyCalls = 0;
                aggregate.DailyTokens = 0;
            }
            return aggregate;
        }

        private static string AggregateId(string userId, DateTime now) => $"{userId}:{now:yyyy-MM}";
        private static bool SameAuthorization(UsageEventDocument x, SubscriptionUsageEvent y) =>
            string.Equals(x.ConsumingService, y.ConsumingService, StringComparison.Ordinal) &&
            string.Equals(x.Endpoint, y.Endpoint, StringComparison.Ordinal) &&
            string.Equals(x.MeterCategory, y.MeterCategory, StringComparison.Ordinal) &&
            x.RequestedUnits == y.RequestedUnits && x.ReservedCostUsd == y.ReservedCostUsd;

        private static bool SameSettlement(UsageEventDocument x, UsageSettlementRequest y) =>
            string.Equals(x.Outcome, y.Outcome, StringComparison.Ordinal) &&
            string.Equals(x.Provider, y.Provider, StringComparison.Ordinal) &&
            string.Equals(x.Model, y.Model, StringComparison.Ordinal) &&
            x.Units == y.Units && x.PromptTokens == y.PromptTokens && x.CompletionTokens == y.CompletionTokens &&
            x.SettledCostUsd == (y.ActualCostUsd ?? y.EstimatedCostUsd) &&
            string.Equals(x.CostSource, y.CostSource, StringComparison.Ordinal) &&
            string.Equals(x.PricingCatalogueVersion, y.PricingCatalogueVersion, StringComparison.Ordinal);
        private static UsageAggregateDocument NewAggregate(string userId, DateTime now) => new()
        {
            Id = AggregateId(userId, now), UserId = userId, Month = now.ToString("yyyy-MM"), Day = now.ToString("yyyy-MM-dd"), UpdatedAtUtc = now
        };

        private sealed class UsageEventDocument : SubscriptionUsageEvent
        {
            [BsonId] public ObjectId Id { get; set; }
            public static UsageEventDocument FromModel(SubscriptionUsageEvent x) => new()
            {
                OperationId=x.OperationId, UserId=x.UserId, ConsumingService=x.ConsumingService, Endpoint=x.Endpoint,
                MeterCategory=x.MeterCategory, Status=x.Status, Outcome=x.Outcome, Provider=x.Provider, Model=x.Model,
                RequestedUnits=x.RequestedUnits, Units=x.Units, PromptTokens=x.PromptTokens, CompletionTokens=x.CompletionTokens,
                ReservedCostUsd=x.ReservedCostUsd, SettledCostUsd=x.SettledCostUsd, CostSource=x.CostSource,
                PricingCatalogueVersion=x.PricingCatalogueVersion, AuthorizedAtUtc=x.AuthorizedAtUtc, SettledAtUtc=x.SettledAtUtc
            };
            public SubscriptionUsageEvent ToModel() => FromModel(this);
        }

        private sealed class UsageAggregateDocument
        {
            [BsonId] public string Id { get; set; }
            public string UserId { get; set; }
            public string Month { get; set; }
            public string Day { get; set; }
            public long MonthlyRequests { get; set; }
            public long DailyCalls { get; set; }
            public long DailyTokens { get; set; }
            public decimal ReservedCostUsd { get; set; }
            public decimal SettledCostUsd { get; set; }
            public DateTime UpdatedAtUtc { get; set; }

            public SubscriptionUsageAggregate ToModel() => new()
            {
                Id=Id, UserId=UserId, Month=Month, Day=Day, MonthlyRequests=MonthlyRequests, DailyCalls=DailyCalls,
                DailyTokens=DailyTokens, ReservedCostUsd=ReservedCostUsd, SettledCostUsd=SettledCostUsd, UpdatedAtUtc=UpdatedAtUtc
            };
        }
    }
}
