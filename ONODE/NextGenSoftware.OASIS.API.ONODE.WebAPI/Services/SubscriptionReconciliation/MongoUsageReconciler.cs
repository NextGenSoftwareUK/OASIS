using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SubscriptionReconciliation;

/// <summary>Read-only verification of ledger projections and external evidence. Never repairs balances.</summary>
public sealed class MongoUsageReconciler
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<UsageExternalReceipt> _receipts;
    private readonly IMongoCollection<UsageReconciliationReport> _reports;
    private readonly TimeProvider _time;
    private static readonly Meter Meter = new("OASIS.Subscription.Reconciliation", "1.0.0");
    private static readonly Counter<long> Drift = Meter.CreateCounter<long>("oasis.subscription.reconciliation.findings");
    private static readonly Histogram<double> Latency = Meter.CreateHistogram<double>("oasis.subscription.reconciliation.duration", "s");
    private static long _lastSuccess;
    private static readonly ObservableGauge<long> LastSuccess = Meter.CreateObservableGauge("oasis.subscription.reconciliation.last_success_unixtime", () => Interlocked.Read(ref _lastSuccess), "s");

    public MongoUsageReconciler(IConfiguration configuration, TimeProvider timeProvider = null)
        : this(new MongoClient(SubscriptionMongoConfiguration.ConnectionString(configuration)),
            SubscriptionMongoConfiguration.DatabaseName(configuration), timeProvider) { }

    public MongoUsageReconciler(IMongoClient client, string database, TimeProvider timeProvider = null)
    {
        _client = client;
        _time = timeProvider ?? TimeProvider.System;
        _database = client.GetDatabase(database, new MongoDatabaseSettings
        {
            WriteConcern = WriteConcern.WMajority.With(journal: true), ReadConcern = ReadConcern.Majority,
            ReadPreference = ReadPreference.Primary
        });
        _receipts = _database.GetCollection<UsageExternalReceipt>("subscription_usage_external_receipts");
        _reports = _database.GetCollection<UsageReconciliationReport>("subscription_usage_reconciliation");
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _receipts.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<UsageExternalReceipt>(Builders<UsageExternalReceipt>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.Month)),
            new CreateIndexModel<UsageExternalReceipt>(Builders<UsageExternalReceipt>.IndexKeys.Ascending(x => x.OperationId))
        }, ct);
        await _reports.Indexes.CreateOneAsync(new CreateIndexModel<UsageReconciliationReport>(
            Builders<UsageReconciliationReport>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.Month).Descending(x => x.CheckedAtUtc)), cancellationToken: ct);
        await _database.GetCollection<BsonDocument>("subscription_usage_audit").Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(new BsonDocument { { "UserId", 1 }, { "Month", 1 } },
                new CreateIndexOptions { Name = "ix_reconciliation_user_month" }), cancellationToken: ct);
        await _database.GetCollection<BsonDocument>("subscription_usage_buckets").Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(new BsonDocument { { "PeriodType", 1 }, { "UserId", 1 }, { "Period", 1 } },
                new CreateIndexOptions { Name = "ix_reconciliation_period_user" }), cancellationToken: ct);
    }

    public async Task<UsageExternalReceipt> RecordReceiptAsync(UsageExternalReceipt receipt, CancellationToken ct)
    {
        receipt.Validate();
        receipt.RecordedAtUtc = _time.GetUtcNow().UtcDateTime;
        try { await _receipts.InsertOneAsync(receipt, cancellationToken: ct); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var original = await _receipts.Find(x => x.Id == receipt.Id).SingleAsync(ct);
            if (!UsageReconciliationMath.SameReceipt(original, receipt))
                throw new InvalidOperationException("RECEIPT_ID_CONFLICT: immutable evidence already exists with different accounting fields.");
            return original;
        }
        return receipt;
    }

    public async Task<UsageReconciliationReport> ReconcileAsync(string userId, string month, CancellationToken ct)
    {
        if (!Guid.TryParseExact(userId, "D", out _) || !UsageReconciliationMath.IsMonth(month))
            throw new ArgumentException("A canonical avatar UUID and yyyy-MM month are required.");
        var watch = Stopwatch.StartNew();
        using var session = await _client.StartSessionAsync(cancellationToken: ct);
        var report = await session.WithTransactionAsync(async (tx, token) =>
        {
            var result = new UsageReconciliationReport { UserId = userId, Month = month, CheckedAtUtc = _time.GetUtcNow().UtcDateTime };
            var expected = new Dictionary<string, UsageLedgerTotals>(StringComparer.Ordinal);
            var providerCorrections = new Dictionary<(string OperationId, string Provider, string RequestId), decimal>();
            var audit = _database.GetCollection<BsonDocument>("subscription_usage_audit");
            using (var cursor = await audit.FindAsync(tx, new BsonDocument { { "UserId", userId }, { "Month", month } }, cancellationToken: token))
                while (await cursor.MoveNextAsync(token))
                    foreach (var entry in cursor.Current)
                    {
                        var delta = UsageReconciliationMath.ReadAudit(entry);
                        if (entry.GetValue("Kind", "").AsString == "correction" && entry.GetValue("CorrectionKind", "").AsString == "provider-measurement")
                        {
                            var key = (entry["OperationId"].AsString, entry["Provider"].AsString, entry["ProviderRequestId"].AsString);
                            providerCorrections[key] = checked(providerCorrections.GetValueOrDefault(key) + delta.SettledCostUsd);
                        }
                        foreach (var bucket in new[] { userId + ":month:" + month, userId + ":day:" + entry["Day"].AsString })
                            expected[bucket] = expected.GetValueOrDefault(bucket) + delta;
                    }

            var bucketFilter = new BsonDocument { { "UserId", userId }, { "$or", new BsonArray
            {
                new BsonDocument { { "PeriodType", "month" }, { "Period", month } },
                new BsonDocument { { "PeriodType", "day" }, { "Period", new BsonRegularExpression("^" + month + "-") } }
            } } };
            var buckets = await _database.GetCollection<BsonDocument>("subscription_usage_buckets").Find(tx, bucketFilter).ToListAsync(token);
            foreach (var bucket in buckets)
            {
                string id = bucket["_id"].AsString;
                var actual = UsageReconciliationMath.ReadBucket(bucket);
                if (!expected.Remove(id, out var sum) || actual != sum)
                    Add(result, "LEDGER_PROJECTION_DRIFT", id, "Bucket differs from immutable audit deltas.");
            }
            foreach (string missing in expected.Keys) Add(result, "MISSING_BUCKET", missing, "Audit exists without its projection.");

            var start = DateTime.SpecifyKind(DateTime.ParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
            result.AwaitingPeriodClose = result.CheckedAtUtc < start.AddMonths(1);
            var eventFilter = new BsonDocument { { "UserId", userId }, { "AuthorizedAtUtc", new BsonDocument { { "$gte", start }, { "$lt", start.AddMonths(1) } } } };
            var events = _database.GetCollection<BsonDocument>("subscription_usage_events");
            bool providerComplete = true;
            using (var cursor = await events.FindAsync(tx, eventFilter, cancellationToken: token))
                while (await cursor.MoveNextAsync(token))
                    foreach (var operation in cursor.Current)
                    {
                        string operationId = operation["OperationId"].AsString;
                        if (operation["Status"].AsString == "expired") continue; // No execution was claimed; exposure was released by WEB4.
                        if (operation["Status"].AsString != "settled")
                        {
                            providerComplete = false;
                            result.UnsettledOperations++;
                            if (!operation.TryGetValue("ExpiresAtUtc", out var expiry) || expiry.BsonType != BsonType.DateTime)
                                Add(result, "INVALID_OPERATION_EXPIRY", operationId, "An unsettled operation has no valid persisted lease expiry.");
                            else if (expiry.ToUniversalTime() <= result.CheckedAtUtc)
                                Add(result, "UNSETTLED_OPERATION", operationId, "The execution lease expired without a definitive provider outcome.");
                            continue;
                        }
                        var evidence = await _receipts.Find(tx, x => x.Source == "provider" && x.OperationId == operationId).ToListAsync(token);
                        using var persistedReceipts = JsonDocument.Parse(operation["ProviderReceiptsJson"].AsString);
                        if (persistedReceipts.RootElement.GetArrayLength() == 0)
                        {
                            providerComplete = false;
                            string fingerprint = operation.GetValue("SettlementFingerprint", "").AsString;
                            bool historical = fingerprint.StartsWith("legacy:", StringComparison.Ordinal);
                            Add(result, historical ? "HISTORICAL_PROVIDER_EVIDENCE_REQUIRED" : "MISSING_PROVIDER_MEASUREMENT",
                                historical ? operationId + ":" + fingerprint : operationId,
                                historical ? "Opening balances preserve accounting totals but do not establish per-provider measurements; reconcile the retained legacy source evidence."
                                    : "A settled operation has no immutable provider or service measurement.");
                        }
                        var expectedReceipts = persistedReceipts.RootElement.EnumerateArray().Where(x =>
                            !UsageReconciliationMath.IsInternalReceipt(x, operation["ConsumingService"].AsString,
                                operationId, operation.GetValue("Outcome", "").AsString)).ToList();
                        foreach (var measurement in expectedReceipts)
                        {
                            string provider = measurement.GetProperty("Provider").GetString();
                            string requestId = measurement.GetProperty("ProviderRequestId").GetString();
                            decimal expectedCost = checked(measurement.GetProperty("ActualCostUsd").GetDecimal() +
                                providerCorrections.GetValueOrDefault((operationId, provider, requestId)));
                            var matched = evidence.Where(x => x.Provider == provider && x.ProviderRequestId == requestId).ToList();
                            if (matched.Count == 0)
                            {
                                providerComplete = false;
                                Add(result, "MISSING_PROVIDER_RECEIPT", operationId + ":" + requestId, "Provider cost remains independently unverified.");
                            }
                            else if (matched.Count != 1 || matched[0].UserId != userId || matched[0].Month != month ||
                                matched[0].ConsumingService != operation["ConsumingService"].AsString ||
                                matched[0].AmountUsd != expectedCost)
                                Add(result, "PROVIDER_RECEIPT_DRIFT", operationId + ":" + requestId, "Provider receipt identity, count or cost differs from settlement.");
                        }
                        if (evidence.Any(x => !expectedReceipts.Any(r => r.GetProperty("Provider").GetString() == x.Provider && r.GetProperty("ProviderRequestId").GetString() == x.ProviderRequestId)))
                            Add(result, "UNEXPECTED_PROVIDER_RECEIPT", operationId, "External evidence contains a provider call absent from settlement.");
                    }
            // Detect evidence with no ledger operation, not only ledger entries missing evidence.
            var providerReceipts = _receipts.Find(tx, x => x.UserId == userId && x.Month == month && x.Source == "provider");
            using (var cursor = await providerReceipts.ToCursorAsync(token))
                while (await cursor.MoveNextAsync(token))
                    foreach (var receipt in cursor.Current)
                    {
                        var owned = await events.Find(tx, new BsonDocument { { "OperationId", receipt.OperationId }, { "UserId", userId } }).FirstOrDefaultAsync(token);
                        if (owned == null)
                            Add(result, "ORPHAN_PROVIDER_RECEIPT", receipt.Id, "Provider evidence has no owned ledger operation.");
                        else if (owned["Status"].AsString == "expired")
                            Add(result, "PROVIDER_WORK_AFTER_UNUSED_EXPIRY", receipt.Id, "Provider evidence exists for an authorization that never acknowledged execution.");
                    }

            var stripe = await _receipts.Find(tx, x => x.UserId == userId && x.Month == month && x.Source == "stripe").ToListAsync(token);
            decimal ledgerCost = buckets.Where(x => x["PeriodType"].AsString == "month").Sum(x => x["SettledCostUsd"].ToDecimal());
            // Current-month costs are still accumulating. Invoice completeness can only
            // be asserted after the UTC period closes; pending is not a balanced report.
            if (!result.AwaitingPeriodClose)
            {
                // A closed zero-cost period needs no usage invoice. Plan fees are separate.
                result.StripeEvidenceComplete = ledgerCost == 0 || stripe.Count > 0;
                if (!result.StripeEvidenceComplete) Add(result, "MISSING_STRIPE_RECEIPT", month, "No finalized usage-cost invoice evidence for this nonzero period.");
                else if (stripe.Sum(x => x.AmountUsd) != decimal.Round(ledgerCost, 2, MidpointRounding.AwayFromZero))
                    Add(result, "STRIPE_USAGE_DRIFT", month, "Finalized usage-cost invoice subtotal differs from the ledger rounded once at the invoice boundary.");
            }
            result.ProviderEvidenceComplete = providerComplete;
            return result;
        }, new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority), ct);
        await _reports.InsertOneAsync(report, cancellationToken: ct);
        Interlocked.Exchange(ref _lastSuccess, _time.GetUtcNow().ToUnixTimeSeconds());
        foreach (var finding in report.Findings) Drift.Add(1, new KeyValuePair<string, object>("code", finding.Code));
        Latency.Record(watch.Elapsed.TotalSeconds);
        return report;
    }

    private static void Add(UsageReconciliationReport report, string code, string reference, string detail) =>
        report.Findings.Add(new UsageReconciliationFinding { Code = code, Reference = reference, Detail = detail });

    public async Task<IReadOnlyList<UsageReconciliationReport>> GetReportsAsync(string userId, string month, CancellationToken ct) =>
        await _reports.Find(x => x.UserId == userId && x.Month == month).SortByDescending(x => x.CheckedAtUtc).Limit(100).ToListAsync(ct);

    public async Task RecordFailedCheckAsync(string userId, string month, string failureType, CancellationToken ct)
    {
        var report = new UsageReconciliationReport { UserId=userId, Month=month, CheckedAtUtc=_time.GetUtcNow().UtcDateTime };
        Add(report, "RECONCILIATION_FAILED", userId + ":" + month, "The check did not complete: " + failureType);
        await _reports.InsertOneAsync(report, cancellationToken: ct);
        Drift.Add(1, new KeyValuePair<string, object>("code", "RECONCILIATION_FAILED"));
    }

    public async Task<IReadOnlyList<(string Id, string UserId, string Month)>> GetWorkAsync(string afterId, int limit, CancellationToken ct)
    {
        string afterUser = null, afterMonth = null;
        if (!string.IsNullOrEmpty(afterId))
        {
            var parts = afterId.Split(":month:", StringSplitOptions.None);
            if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "D", out _) || !UsageReconciliationMath.IsMonth(parts[1]))
                throw new ArgumentException("A canonical account-month reconciliation cursor is required.");
            (afterUser, afterMonth) = (parts[0], parts[1]);
        }
        int pageSize = Math.Clamp(limit, 1, 100);
        // Each independent accounting source contributes its first N distinct keys.
        // Their sorted union contains the first N keys globally, even if a projection
        // was lost or an external receipt has no corresponding ledger activity.
        var pages = await Task.WhenAll(
            ReadWorkSourceAsync("subscription_usage_buckets", "Period", true, afterUser, afterMonth, pageSize, ct),
            ReadWorkSourceAsync("subscription_usage_audit", "Month", false, afterUser, afterMonth, pageSize, ct),
            ReadWorkSourceAsync("subscription_usage_external_receipts", "Month", false, afterUser, afterMonth, pageSize, ct));
        return pages.SelectMany(x => x).DistinctBy(x => x.Id).OrderBy(x => x.Id, StringComparer.Ordinal).Take(pageSize).ToList();
    }

    private async Task<List<(string Id, string UserId, string Month)>> ReadWorkSourceAsync(string collection, string monthField,
        bool buckets, string afterUser, string afterMonth, int limit, CancellationToken ct)
    {
        var filter = new BsonDocument();
        if (buckets) filter.Add("PeriodType", "month");
        if (afterUser != null) filter.Add("$or", new BsonArray {
            new BsonDocument("UserId", new BsonDocument("$gt", afterUser)),
            new BsonDocument { { "UserId", afterUser }, { monthField, new BsonDocument("$gt", afterMonth) } }
        });
        var index = buckets ? new BsonDocument { { "PeriodType", 1 }, { "UserId", 1 }, { "Period", 1 } }
            : new BsonDocument { { "UserId", 1 }, { "Month", 1 } };
        var pipeline = new[] {
            new BsonDocument("$match", filter),
            new BsonDocument("$sort", new BsonDocument { { "UserId", 1 }, { monthField, 1 } }),
            new BsonDocument("$group", new BsonDocument("_id", new BsonDocument { { "UserId", "$UserId" }, { "Month", "$" + monthField } })),
            new BsonDocument("$sort", new BsonDocument { { "_id.UserId", 1 }, { "_id.Month", 1 } }),
            new BsonDocument("$limit", limit)
        };
        var keys = await _database.GetCollection<BsonDocument>(collection)
            .Aggregate<BsonDocument>(pipeline, new AggregateOptions { Hint = index, AllowDiskUse = true }).ToListAsync(ct);
        return keys.Select(x => {
            string user = x["_id"]["UserId"].AsString, month = x["_id"]["Month"].AsString;
            if (!Guid.TryParseExact(user, "D", out _) || !UsageReconciliationMath.IsMonth(month))
                throw new InvalidOperationException("Reconciliation source contains an invalid account-month identity: " + collection);
            return (user + ":month:" + month, user, month);
        }).ToList();
    }
}
