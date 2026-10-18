using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// Delegated usage grants let an avatar authorize one consuming service to reserve usage on its behalf
    /// without a live bearer (scheduled runs, inbound channel messages). A grant never widens the avatar's
    /// own policy: reservations still pass every subscription limit, and additionally the grant's endpoint
    /// allow-list, per-operation cap, monthly budget, expiry and revocation. Grant budget movements commit in
    /// the same transaction as the usage operation they belong to.
    /// </summary>
    public sealed partial class MongoSubscriptionUsageRepository : IDelegatedUsageGrantRepository
    {
        public const int MaxActiveGrantsPerAvatar = 100;
        private IMongoCollection<GrantDocument> Grants => _database.GetCollection<GrantDocument>("subscription_usage_grants");
        private IMongoCollection<GrantBucket> GrantBuckets => _database.GetCollection<GrantBucket>("subscription_usage_grant_buckets");

        private void InitializeGrantIndexes() =>
            Grants.Indexes.CreateOne(new CreateIndexModel<GrantDocument>(
                Builders<GrantDocument>.IndexKeys.Ascending(x => x.UserId).Descending(x => x.CreatedAtUtc), new CreateIndexOptions { Name = "ix_grant_owner" }));

        public Task<DelegatedUsageGrant> CreateGrantAsync(string userId, CreateDelegatedUsageGrantRequest request, CancellationToken cancellationToken)
        {
            var now = _time.GetUtcNow().UtcDateTime;
            DelegatedUsageGrantValidation.ValidateCreate(userId, request, now);
            return TransactAsync(async (session, ct) =>
            {
                long active = await Grants.CountDocumentsAsync(session, x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now, cancellationToken: ct);
                if (active >= MaxActiveGrantsPerAvatar)
                    throw new SubscriptionUsageLimitException("GRANT_LIMIT_REACHED", $"An avatar may hold at most {MaxActiveGrantsPerAvatar} active delegated grants.", 409);
                var grant = new GrantDocument
                {
                    GrantId = Guid.NewGuid().ToString("D"), UserId = userId, ConsumingService = request.ConsumingService.Trim().ToUpperInvariant(),
                    Scope = request.Scope.Trim(), Endpoints = request.Endpoints.Select(e => e.Trim()).Distinct(StringComparer.Ordinal).ToList(),
                    MonthlyBudgetUsd = request.MonthlyBudgetUsd, MaxOperationCostUsd = request.MaxOperationCostUsd,
                    CreatedAtUtc = now, ExpiresAtUtc = DateTime.SpecifyKind(request.ExpiresAtUtc, DateTimeKind.Utc)
                };
                await Grants.InsertOneAsync(session, grant, cancellationToken: ct);
                await GrantAuditAsync(session, grant, "grant-created", now, userId, ct);
                return await ProjectAsync(session, grant, now, ct);
            }, cancellationToken);
        }

        public Task<IReadOnlyList<DelegatedUsageGrant>> ListGrantsAsync(string userId, CancellationToken cancellationToken) =>
            TransactAsync<IReadOnlyList<DelegatedUsageGrant>>(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var grants = await Grants.Find(session, x => x.UserId == userId).SortByDescending(x => x.CreatedAtUtc).Limit(500).ToListAsync(ct);
                var result = new List<DelegatedUsageGrant>(grants.Count);
                foreach (var grant in grants) result.Add(await ProjectAsync(session, grant, now, ct));
                return result;
            }, cancellationToken);

        public Task<DelegatedUsageGrant> GetGrantAsync(string grantId, CancellationToken cancellationToken) =>
            TransactAsync(async (session, ct) =>
            {
                var grant = await Grants.Find(session, x => x.GrantId == grantId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The delegated usage grant was not found.");
                return await ProjectAsync(session, grant, _time.GetUtcNow().UtcDateTime, ct);
            }, cancellationToken);

        /// <summary>Revocation stops new reservations immediately; in-flight operations still settle so the ledger stays complete.</summary>
        public Task<DelegatedUsageGrant> RevokeGrantAsync(string userId, string grantId, CancellationToken cancellationToken) =>
            TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var grant = await Grants.Find(session, x => x.GrantId == grantId).FirstOrDefaultAsync(ct);
                if (grant == null || grant.UserId != userId) throw new KeyNotFoundException("The delegated usage grant was not found.");
                if (grant.RevokedAtUtc == null)
                {
                    grant.RevokedAtUtc = now;
                    await Grants.ReplaceOneAsync(session, x => x.GrantId == grantId, grant, cancellationToken: ct);
                    await GrantAuditAsync(session, grant, "grant-revoked", now, userId, ct);
                }
                return await ProjectAsync(session, grant, now, ct);
            }, cancellationToken);

        private async Task ReserveAgainstGrantAsync(IClientSessionHandle session, SubscriptionUsageEvent usageEvent, CancellationToken ct)
        {
            var grant = await Grants.Find(session, x => x.GrantId == usageEvent.GrantId).FirstOrDefaultAsync(ct)
                ?? throw new SubscriptionUsageLimitException("GRANT_NOT_FOUND", "The delegated usage grant was not found.", 403);
            var bucket = await LoadGrantBucketAsync(session, grant.GrantId, usageEvent.AuthorizedAtUtc, ct);
            DelegatedUsageGrantValidation.EnsureReservable(ToContract(grant, bucket), usageEvent);
            await ApplyGrantDeltaAsync(session, usageEvent, usageEvent.ReservedCostUsd, 0m, usageEvent.AuthorizedAtUtc, ct);
        }

        private async Task ApplyGrantDeltaAsync(IClientSessionHandle session, SubscriptionUsageEvent operation, decimal reservedDelta, decimal settledDelta, DateTime now, CancellationToken ct)
        {
            var bucket = await LoadGrantBucketAsync(session, operation.GrantId, operation.AuthorizedAtUtc, ct);
            bucket.ReservedCostUsd = checked(bucket.ReservedCostUsd + reservedDelta);
            bucket.SettledCostUsd = checked(bucket.SettledCostUsd + settledDelta);
            if (bucket.ReservedCostUsd < 0 || bucket.SettledCostUsd < 0)
                throw new InvalidOperationException("Delegated grant bucket invariant violated. Reconcile ledger before accepting further writes.");
            bucket.UpdatedAtUtc = now;
            await GrantBuckets.ReplaceOneAsync(session, x => x.Id == bucket.Id, bucket, new ReplaceOptions { IsUpsert = true }, ct);
        }

        private async Task<GrantBucket> LoadGrantBucketAsync(IClientSessionHandle session, string grantId, DateTime time, CancellationToken ct)
        {
            string month = time.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            string id = grantId + ":" + month;
            return await GrantBuckets.Find(session, x => x.Id == id).FirstOrDefaultAsync(ct)
                ?? new GrantBucket { Id = id, GrantId = grantId, Month = month, UpdatedAtUtc = time };
        }

        private async Task<DelegatedUsageGrant> ProjectAsync(IClientSessionHandle session, GrantDocument grant, DateTime now, CancellationToken ct)
        {
            return ToContract(grant, await LoadGrantBucketAsync(session, grant.GrantId, now, ct));
        }

        private static DelegatedUsageGrant ToContract(GrantDocument grant, GrantBucket bucket) =>
            new DelegatedUsageGrant
            {
                GrantId = grant.GrantId, UserId = grant.UserId, ConsumingService = grant.ConsumingService, Scope = grant.Scope,
                Endpoints = grant.Endpoints.ToList(), MonthlyBudgetUsd = grant.MonthlyBudgetUsd, MaxOperationCostUsd = grant.MaxOperationCostUsd,
                CreatedAtUtc = grant.CreatedAtUtc, ExpiresAtUtc = grant.ExpiresAtUtc, RevokedAtUtc = grant.RevokedAtUtc,
                MonthReservedCostUsd = bucket.ReservedCostUsd, MonthSettledCostUsd = bucket.SettledCostUsd
            };

        private Task GrantAuditAsync(IClientSessionHandle session, GrantDocument grant, string kind, DateTime now, string actor, CancellationToken ct) =>
            _audit.InsertOneAsync(session, new SubscriptionUsageAuditEntry
            {
                Id = grant.GrantId + ":" + kind, OperationId = grant.GrantId, UserId = grant.UserId, ConsumingService = grant.ConsumingService,
                Kind = kind, Actor = actor, OccurredAtUtc = now,
                Month = now.ToString("yyyy-MM", CultureInfo.InvariantCulture), Day = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                PayloadJson = JsonSerializer.Serialize(new { grant.Scope, grant.Endpoints, grant.MonthlyBudgetUsd, grant.MaxOperationCostUsd, grant.ExpiresAtUtc, grant.RevokedAtUtc })
            }, cancellationToken: ct);

        [BsonIgnoreExtraElements]
        internal sealed class GrantDocument
        {
            [BsonId] public string GrantId { get; set; }
            public string UserId { get; set; }
            public string ConsumingService { get; set; }
            public string Scope { get; set; }
            public List<string> Endpoints { get; set; } = new();
            [BsonRepresentation(BsonType.Decimal128)] public decimal MonthlyBudgetUsd { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal MaxOperationCostUsd { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public DateTime? RevokedAtUtc { get; set; }
        }

        [BsonIgnoreExtraElements]
        internal sealed class GrantBucket
        {
            [BsonId] public string Id { get; set; }
            public string GrantId { get; set; }
            public string Month { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal ReservedCostUsd { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal SettledCostUsd { get; set; }
            public DateTime UpdatedAtUtc { get; set; }
        }
    }
}
