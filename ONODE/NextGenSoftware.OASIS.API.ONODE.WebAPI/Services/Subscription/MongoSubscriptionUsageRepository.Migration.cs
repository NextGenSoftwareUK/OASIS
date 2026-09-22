using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository
    {
        private static readonly FilterDefinition<BsonDocument> LegacyOperationFilter = new BsonDocument("RequestFingerprint", BsonNull.Value);
        private static readonly JsonWriterSettings LegacyJson = new() { OutputMode = JsonOutputMode.CanonicalExtendedJson };

        private async Task EnsureOpeningBalanceAsync(IClientSessionHandle session, CancellationToken ct)
        {
            bool migrated = await _database.GetCollection<BsonDocument>("subscription_usage_migrations")
                .Find(session, new BsonDocument("_id", "v1-opening-balance")).AnyAsync(ct);
            bool legacy = await _database.GetCollection<BsonDocument>("subscription_usage_events").Find(session, LegacyOperationFilter).AnyAsync(ct);
            if (legacy || !migrated)
                throw new SubscriptionUsageConflictException("LEGACY_OPENING_BALANCE_REQUIRED: freeze legacy writers and finish the reviewed, signed opening-balance batches before reserving new usage.");
        }

        public Task<UsageMigrationInventory> GetMigrationInventoryAsync(CancellationToken cancellationToken) =>
            TransactAsync(async (session, ct) =>
            {
                var aggregates = await _database.GetCollection<BsonDocument>("subscription_usage_aggregates")
                    .Find(session, FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync(ct);
                var operations = await OriginalLegacyOperationsAsync(session, ct);
                string aggregateJson = new BsonArray(aggregates).ToJson(LegacyJson);
                string operationJson = new BsonArray(operations).ToJson(LegacyJson);
                return new UsageMigrationInventory {
                    LegacyAggregatesJson = aggregateJson, LegacyOperationsJson = operationJson,
                    SourceDigest = Hash(aggregateJson + "\n" + operationJson),
                    AlreadyImported = await _database.GetCollection<BsonDocument>("subscription_usage_migrations")
                        .Find(session, new BsonDocument("_id", "v1-opening-balance")).AnyAsync(ct)
                };
            }, cancellationToken);

        private async Task<List<BsonDocument>> OriginalLegacyOperationsAsync(IClientSessionHandle session, CancellationToken ct)
        {
            var operations = await _database.GetCollection<BsonDocument>("subscription_usage_events").Find(session, LegacyOperationFilter).ToListAsync(ct);
            var archived = await _database.GetCollection<BsonDocument>("subscription_usage_legacy_archive").Find(session, FilterDefinition<BsonDocument>.Empty).ToListAsync(ct);
            operations.AddRange(archived.Select(x => x["Source"].AsBsonDocument));
            if (operations.GroupBy(x => x["OperationId"].AsString).Any(x => x.Count() > 1))
                throw new SubscriptionUsageConflictException("A legacy writer changed an already imported operation. Stop that writer and reconcile the source before continuing.");
            return operations.OrderBy(x => x["_id"].ToString(), StringComparer.Ordinal).ToList();
        }

        public Task<string> ImportOpeningBalanceAsync(string actor, UsageOpeningBalanceManifest manifest, CancellationToken cancellationToken)
        {
            if (manifest == null || actor != manifest.ApprovedBy || !Guid.TryParseExact(manifest.MigrationId, "D", out _) ||
                manifest.Balances == null || manifest.Subscriptions == null || manifest.Orders == null ||
                manifest.Balances.Count + manifest.Subscriptions.Count + manifest.Orders.Count > 1000 || string.IsNullOrWhiteSpace(manifest.EvidenceSha256) ||
                string.IsNullOrWhiteSpace(manifest.Signature) || manifest.BatchCount < 1 || manifest.BatchCount > 100000 ||
                manifest.BatchIndex < 0 || manifest.BatchIndex >= manifest.BatchCount)
                throw new ArgumentException("An approved migration id, valid zero-based batch index/count and up to 1000 daily opening balances per batch are required.");
            if (manifest.Balances.Any(x => x == null) || manifest.Balances.GroupBy(x => new { x.UserId, x.Day }).Any(x => x.Count() > 1))
                throw new ArgumentException("Opening balances must contain exactly one row per avatar/day.");
            foreach (var balance in manifest.Balances)
                if (!Guid.TryParseExact(balance.UserId, "D", out _) || !DateTime.TryParseExact(balance.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                    balance.Requests < 0 || balance.ReservedTokens < 0 || balance.SettledTokens < 0 || balance.ReservedCostUsd < 0 || balance.SettledCostUsd < 0)
                    throw new ArgumentException("Opening balances require canonical avatar/date values and nonnegative quantities.");
            string manifestJson = UsageMigrationSignature.CanonicalJson(manifest);
            return TransactAsync(async (session, ct) =>
            {
                var migrations = _database.GetCollection<BsonDocument>("subscription_usage_migrations");
                string batchId = "v1-batch:" + manifest.MigrationId + ":" + manifest.BatchIndex;
                var existing = await migrations.Find(session, new BsonDocument("_id", batchId)).FirstOrDefaultAsync(ct);
                if (existing != null)
                {
                    if (existing["ManifestJson"].AsString != manifestJson)
                        throw new SubscriptionUsageConflictException("This migration batch was already imported with a different manifest.");
                    return manifest.MigrationId;
                }
                if (await migrations.Find(session, new BsonDocument("_id", "v1-opening-balance")).AnyAsync(ct))
                    throw new SubscriptionUsageConflictException("The opening-balance migration is already complete.");
                var aggregates = await _database.GetCollection<BsonDocument>("subscription_usage_aggregates")
                    .Find(session, FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync(ct);
                var originals = await OriginalLegacyOperationsAsync(session, ct);
                string digest = Hash(new BsonArray(aggregates).ToJson(LegacyJson) + "\n" + new BsonArray(originals).ToJson(LegacyJson));
                if (digest != manifest.SourceDigest) throw new SubscriptionUsageConflictException("Legacy source changed after review; export and approve a fresh inventory.");
                var plan = await migrations.Find(session, new BsonDocument("_id", "v1-plan")).FirstOrDefaultAsync(ct);
                if (plan != null && (plan["MigrationId"].AsString != manifest.MigrationId || plan["SourceDigest"].AsString != manifest.SourceDigest ||
                    plan["EvidenceSha256"].AsString != manifest.EvidenceSha256 || plan["Actor"].AsString != actor || plan["BatchCount"].AsInt32 != manifest.BatchCount))
                    throw new SubscriptionUsageConflictException("A different reviewed migration plan is already in progress.");
                var now = _time.GetUtcNow().UtcDateTime;
                if (plan == null)
                    await migrations.InsertOneAsync(session, new BsonDocument { { "_id", "v1-plan" }, { "MigrationId", manifest.MigrationId },
                        { "SourceDigest", manifest.SourceDigest }, { "EvidenceSha256", manifest.EvidenceSha256 }, { "Actor", actor },
                        { "BatchCount", manifest.BatchCount }, { "AppliedBatches", 0 }, { "StartedAtUtc", now } }, cancellationToken: ct);

                foreach (var balance in manifest.Balances)
                {
                    string month = balance.Day.Substring(0, 7);
                    var periodHistory = await _audit.Find(session, x => x.UserId == balance.UserId && x.Month == month).ToListAsync(ct);
                    if (periodHistory.Any(x => x.Kind != "opening-balance" || x.OperationId != manifest.MigrationId) ||
                        (periodHistory.Count == 0 && await _buckets.Find(session, x => x.UserId == balance.UserId && x.PeriodType == "month" && x.Period == month).AnyAsync(ct)))
                        throw new SubscriptionUsageConflictException("Opening balances cannot be imported into a period with v2 ledger activity.");
                    if (periodHistory.Any(x => x.Day == balance.Day))
                        throw new SubscriptionUsageConflictException("This avatar/day was already imported by another batch.");
                    var unresolved = originals.Where(x => x["UserId"].AsString == balance.UserId && DayOf(x) == balance.Day && x["Status"].AsString != "settled").ToList();
                    if (balance.ReservedTokens < unresolved.Sum(x => x.GetValue("RequestedUnits", 0).ToInt64()) ||
                        balance.ReservedCostUsd < unresolved.Sum(x => BsonDecimal(x.GetValue("ReservedCostUsd", 0))) || balance.Requests < unresolved.Count)
                        throw new ArgumentException("Opening balance omits exposure from unresolved legacy operations.");
                    var date = DateTime.SpecifyKind(DateTime.ParseExact(balance.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Utc);
                    var operation = new SubscriptionUsageEvent { UserId = balance.UserId, AuthorizedAtUtc = date };
                    var entry = new SubscriptionUsageAuditEntry {
                        Id = "opening:" + manifest.MigrationId + ":" + balance.UserId + ":" + balance.Day,
                        UserId = balance.UserId, OperationId = manifest.MigrationId, ConsumingService = "WEB4", Kind = "opening-balance",
                        Actor = actor, Reason = "Reviewed v1 MongoDB and Holon opening balance", ExternalReference = manifest.EvidenceSha256,
                        Month = month, Day = balance.Day, OccurredAtUtc = now, RequestsDelta = balance.Requests,
                        ReservedUnitsDelta = balance.ReservedTokens, SettledUnitsDelta = balance.SettledTokens,
                        ReservedCostDeltaUsd = balance.ReservedCostUsd, SettledCostDeltaUsd = balance.SettledCostUsd,
                        PayloadJson = JsonSerializer.Serialize(balance)
                    };
                    await ApplyDeltaAsync(session, operation, entry, ct);
                    await _audit.InsertOneAsync(session, entry, cancellationToken: ct);
                    foreach (var raw in originals.Where(x => x["UserId"].AsString == balance.UserId && DayOf(x) == balance.Day))
                    {
                        string sourceJson = raw.ToJson(LegacyJson);
                        await _database.GetCollection<BsonDocument>("subscription_usage_legacy_archive").InsertOneAsync(session, new BsonDocument {
                            { "_id", raw["OperationId"] }, { "MigrationId", manifest.MigrationId }, { "Source", raw },
                            { "SourceSha256", Hash(sourceJson) }, { "ImportedAtUtc", now }
                        }, cancellationToken: ct);
                        var changes = new BsonDocument { { "RequestFingerprint", Hash(sourceJson) }, { "ExpiresAtUtc", now }, { "MeterCategory", "ai.tokens" },
                            { "Status", raw["Status"].AsString == "settled" ? "settled" : "recovery-required" },
                            { "ProviderReceiptsJson", "[]" }, { "SettlementFingerprint", "legacy:" + Hash(sourceJson) } };
                        await _database.GetCollection<BsonDocument>("subscription_usage_events").UpdateOneAsync(session,
                            new BsonDocument("_id", raw["_id"]), new BsonDocument("$set", changes), cancellationToken: ct);
                    }
                }
                foreach (var subscription in manifest.Subscriptions)
                {
                    ValidateSubscription(subscription);
                    if (await Subscriptions.Find(session, x => x.UserId == subscription.UserId).AnyAsync(ct))
                        throw new SubscriptionUsageConflictException("A subscription opening snapshot already exists for this avatar; use canonical Stripe events for later changes.");
                    await Subscriptions.InsertOneAsync(session, subscription, cancellationToken: ct);
                    await BillingAudit.InsertOneAsync(session, new BsonDocument {
                        { "_id", "opening-subscription:" + subscription.UserId }, { "Kind", "subscription-opening-balance" },
                        { "UserId", subscription.UserId }, { "OccurredAtUtc", now }, { "Actor", actor }, { "EvidenceSha256", manifest.EvidenceSha256 },
                        { "PayloadJson", JsonSerializer.Serialize(subscription) }
                    }, cancellationToken: ct);
                }
                foreach (var invoice in manifest.Orders) await InsertInvoiceAsync(session, invoice, ct);                await migrations.InsertOneAsync(session, new BsonDocument { { "_id", batchId }, { "MigrationId", manifest.MigrationId },
                    { "BatchIndex", manifest.BatchIndex }, { "ManifestJson", manifestJson }, { "SourceDigest", manifest.SourceDigest },
                    { "Actor", actor }, { "ImportedAtUtc", now }, { "Signature", manifest.Signature } }, cancellationToken: ct);
                // Every batch writes the same plan document, serializing concurrent finalization.
                var progress = await migrations.FindOneAndUpdateAsync(session, new BsonDocument("_id", "v1-plan"),
                    new BsonDocument("$inc", new BsonDocument("AppliedBatches", 1)),
                    new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, ct);
                long imported = progress["AppliedBatches"].ToInt64();
                if (imported == manifest.BatchCount)
                {
                    if (await _database.GetCollection<BsonDocument>("subscription_usage_events").Find(session, LegacyOperationFilter).AnyAsync(ct))
                        throw new ArgumentException("The reviewed batches do not cover every legacy operation day; finalization is forbidden.");
                    foreach (var aggregate in aggregates)
                        if (!await _audit.Find(session, x => x.Kind == "opening-balance" && x.OperationId == manifest.MigrationId &&
                            x.UserId == aggregate["UserId"].AsString && x.Month == aggregate["Month"].AsString).AnyAsync(ct))
                            throw new ArgumentException("The reviewed batches do not cover every legacy avatar/month; finalization is forbidden.");
                    await migrations.InsertOneAsync(session, new BsonDocument { { "_id", "v1-opening-balance" }, { "MigrationId", manifest.MigrationId },
                        { "SourceDigest", manifest.SourceDigest }, { "EvidenceSha256", manifest.EvidenceSha256 }, { "Actor", actor },
                        { "BatchCount", manifest.BatchCount }, { "CompletedAtUtc", now } }, cancellationToken: ct);
                }
                return manifest.MigrationId;
            }, cancellationToken);
        }
        private static string DayOf(BsonDocument value) => value["AuthorizedAtUtc"].ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        private static decimal BsonDecimal(BsonValue value) => value.IsDecimal128 ? (decimal)value.AsDecimal128 : decimal.Parse(value.ToString(), CultureInfo.InvariantCulture);
    }
}
