using System;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed partial class MongoSubscriptionUsageRepository : ISandboxSubscriptionStore
{
    public Task CreateSandboxSubscriptionAsync(string avatarId, DateTime createdUtc, DateTime expiresUtc, CancellationToken token)
    {
        if (!Guid.TryParseExact(avatarId, "D", out var id) || id == Guid.Empty ||
            createdUtc.Kind != DateTimeKind.Utc || expiresUtc.Kind != DateTimeKind.Utc ||
            createdUtc.Ticks % TimeSpan.TicksPerMillisecond != 0 || expiresUtc.Ticks % TimeSpan.TicksPerMillisecond != 0 ||
            createdUtc > _time.GetUtcNow().UtcDateTime || expiresUtc <= createdUtc || expiresUtc <= _time.GetUtcNow().UtcDateTime)
            throw new ArgumentException("A canonical demo identity and an unexpired UTC lifetime are required.");

        return TransactAsync(async (session, ct) => {
            await EnsureOpeningBalanceAsync(session, ct);
            var existing = await Subscriptions.Find(session, record => record.UserId == avatarId).FirstOrDefaultAsync(ct);
            if (existing != null) {
                if (IsSandboxFreeSubscription(existing) && existing.CreatedAt == createdUtc && existing.SandboxExpiresAtUtc == expiresUtc)
                    return true; // Same issuance is idempotent; it never extends a lifetime.
                throw new SubscriptionUsageConflictException("Sandbox issuance cannot replace an existing subscription or change its lifetime.");
            }
            var now = _time.GetUtcNow().UtcDateTime;
            var record = new SubscriptionRecord {
                UserId = avatarId, PlanId = "free", Status = "free", PayAsYouGoEnabled = false,
                CreatedAt = createdUtc, UpdatedAt = now, FreePlanActivatedAtUtc = now,
                CurrentPeriodStart = createdUtc, CurrentPeriodEnd = expiresUtc, SandboxExpiresAtUtc = expiresUtc
            };
            await Subscriptions.InsertOneAsync(session, record, cancellationToken: ct);
            await BillingAudit.InsertOneAsync(session, new BsonDocument {
                { "_id", Guid.NewGuid().ToString("D") }, { "Kind", "sandbox-free-plan-activation" },
                { "UserId", avatarId }, { "OccurredAtUtc", now }, { "PayloadJson", JsonSerializer.Serialize(record) }
            }, cancellationToken: ct);
            return true;
        }, token);
    }

    private static bool IsSandboxFreeSubscription(SubscriptionRecord record) =>
        record != null && record.SandboxExpiresAtUtc.HasValue && record.CurrentPeriodEnd == record.SandboxExpiresAtUtc &&
        record.PlanId == "free" && record.Status is "free" or "active" && !record.PayAsYouGoEnabled &&
        record.StripeCustomerId == null && record.StripeSubscriptionId == null;

    public async Task<SandboxSubscriptionSweep> RetireSandboxSubscriptionsAsync(int maximumRecords, CancellationToken token)
    {
        if (maximumRecords < 1 || maximumRecords > 1000) throw new ArgumentOutOfRangeException(nameof(maximumRecords));
        var now = _time.GetUtcNow().UtcDateTime;
        // Exclude unresolved work before taking the retirement page. Otherwise a full
        // page of old recovery cases would indefinitely starve later eligible records.
        // The lookup returns at most one event; facet output and query time are bounded.
        var pipeline = new BsonDocument[] {
            new("$match", new BsonDocument {
                { "SandboxExpiresAtUtc", new BsonDocument { { "$type", "date" }, { "$lte", now } } },
                { "PlanId", "free" }, { "Status", new BsonDocument("$in", new BsonArray { "free", "active" }) },
                { "PayAsYouGoEnabled", false }, { "StripeCustomerId", BsonNull.Value }, { "StripeSubscriptionId", BsonNull.Value }
            }),
            new("$sort", new BsonDocument { { "SandboxExpiresAtUtc", 1 }, { "_id", 1 } }),
            new("$lookup", new BsonDocument {
                { "from", "subscription_usage_events" }, { "localField", "_id" }, { "foreignField", "UserId" },
                { "pipeline", new BsonArray { new BsonDocument("$match", new BsonDocument("Status", new BsonDocument("$nin", new BsonArray { "settled", "expired" }))), new BsonDocument("$limit", 1) } },
                { "as", "SandboxPending" }
            }),
            new("$facet", new BsonDocument {
                { "eligible", new BsonArray {
                    new BsonDocument("$match", new BsonDocument("SandboxPending.0", new BsonDocument("$exists", false))),
                    new BsonDocument("$limit", maximumRecords), new BsonDocument("$project", new BsonDocument("SandboxPending", 0)) } },
                { "deferred", new BsonArray {
                    new BsonDocument("$match", new BsonDocument("SandboxPending.0", new BsonDocument("$exists", true))),
                    new BsonDocument("$limit", maximumRecords), new BsonDocument("$count", "count") } }
            })
        };
        var page = await Subscriptions.Aggregate<BsonDocument>(pipeline,
            new AggregateOptions { MaxTime = TimeSpan.FromSeconds(15), AllowDiskUse = false }, token).SingleAsync(token);
        var candidates = page["eligible"].AsBsonArray.Select(value => BsonSerializer.Deserialize<SubscriptionRecord>(value.AsBsonDocument));
        int removed = 0, deferred = page["deferred"].AsBsonArray.Count == 0 ? 0 : page["deferred"][0]["count"].AsInt32;
        foreach (var candidate in candidates) {
            var retired = await TransactAsync(async (session, ct) => {
                await EnsureOpeningBalanceAsync(session, ct);
                var record = await Subscriptions.Find(session, value => value.UserId == candidate.UserId).FirstOrDefaultAsync(ct);
                if (!IsSandboxFreeSubscription(record) || record.SandboxExpiresAtUtc > _time.GetUtcNow().UtcDateTime) return false;
                // Authorize also writes this subscription document. Snapshot transactions serialize
                // retirement against a concurrent reservation; unresolved work is never orphaned.
                if (await _events.Find(session, entry => entry.UserId == record.UserId &&
                    entry.Status != "settled" && entry.Status != "expired").Limit(1).AnyAsync(ct)) return false;
                var deleted = await Subscriptions.DeleteOneAsync(session,
                    Builders<SubscriptionRecord>.Filter.Eq(value => value.UserId, record.UserId), cancellationToken: ct);
                if (deleted.DeletedCount != 1) throw new SubscriptionUsageConflictException("Sandbox retirement did not delete its exact subscription.");
                await BillingAudit.InsertOneAsync(session, new BsonDocument {
                    { "_id", Guid.NewGuid().ToString("D") }, { "Kind", "sandbox-free-plan-retirement" },
                    { "UserId", record.UserId }, { "OccurredAtUtc", _time.GetUtcNow().UtcDateTime },
                    { "PayloadJson", JsonSerializer.Serialize(record) }
                }, cancellationToken: ct);
                return true;
            }, token);
            if (retired) removed++; else deferred++;
        }
        return new(removed, deferred);
    }
}
