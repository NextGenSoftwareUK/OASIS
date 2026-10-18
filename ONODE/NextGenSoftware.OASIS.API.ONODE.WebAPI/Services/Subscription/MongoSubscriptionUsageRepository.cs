using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// A replica-set transaction commits the operation projection, both calendar buckets and an
    /// append-only audit entry. Snapshot/majority semantics serialize writers sharing a bucket.
    /// Driver retries cover labelled transaction conflicts/ambiguous commits. Duplicate-key
    /// creation races are retried at most three times against a fresh snapshot; all other errors propagate.
    /// </summary>
    public sealed partial class MongoSubscriptionUsageRepository : ISubscriptionUsageRepository, IPaidOverageStatementStore
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<UsageEventDocument> _events;
        private readonly IMongoCollection<SubscriptionUsageBucket> _buckets;
        private readonly IMongoCollection<SubscriptionUsageAuditEntry> _audit;
        private readonly IMongoCollection<BsonDocument> _receipts;
        private readonly TimeProvider _time;
        private readonly ISubscriptionUsagePolicyProvider _policyProvider;
        private readonly bool _initializeEmptyLedger;
        private readonly PaidOveragePricing _overagePricing;
        private static readonly TransactionOptions TransactionOptions = new(
            readConcern: ReadConcern.Snapshot, readPreference: ReadPreference.Primary, writeConcern: WriteConcern.WMajority.With(journal: true));

        public MongoSubscriptionUsageRepository(IConfiguration configuration) : this(
            new MongoClient(SubscriptionMongoConfiguration.ConnectionString(configuration)),
            SubscriptionMongoConfiguration.DatabaseName(configuration), TimeProvider.System,
            initializeEmptyLedger: SubscriptionMongoConfiguration.InitializeEmptyLedger(configuration)) { }

        public MongoSubscriptionUsageRepository(IMongoClient client, string databaseName, TimeProvider time,
            ISubscriptionUsagePolicyProvider policyProvider = null, bool initializeEmptyLedger = false, PaidOveragePricing overagePricing = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _policyProvider = policyProvider ?? new SubscriptionUsagePolicyProvider();
            _initializeEmptyLedger = initializeEmptyLedger;
            _overagePricing = overagePricing;
            var database = client.GetDatabase(databaseName);
            _database = database;
            InitializeBillingIndexes();
            InitializeGrantIndexes();
            _events = database.GetCollection<UsageEventDocument>("subscription_usage_events");
            _buckets = database.GetCollection<SubscriptionUsageBucket>("subscription_usage_buckets");
            _audit = database.GetCollection<SubscriptionUsageAuditEntry>("subscription_usage_audit");
            _receipts = database.GetCollection<BsonDocument>("subscription_usage_receipts");
            _events.Indexes.CreateMany(new[] {
                new CreateIndexModel<UsageEventDocument>(Builders<UsageEventDocument>.IndexKeys.Ascending(x => x.OperationId), new CreateIndexOptions { Unique = true, Name = "ux_operation_id" }),
                new CreateIndexModel<UsageEventDocument>(Builders<UsageEventDocument>.IndexKeys.Ascending(x => x.UserId).Descending(x => x.AuthorizedAtUtc), new CreateIndexOptions { Name = "ix_user_authorized" }),
                new CreateIndexModel<UsageEventDocument>(Builders<UsageEventDocument>.IndexKeys.Ascending(x => x.Status).Ascending(x => x.ExpiresAtUtc), new CreateIndexOptions { Name = "ix_status_expiry" }),
                new CreateIndexModel<UsageEventDocument>(Builders<UsageEventDocument>.IndexKeys.Ascending(x => x.ConsumingService).Ascending(x => x.Status).Ascending(x => x.AuthorizedAtUtc), new CreateIndexOptions { Name = "ix_service_recovery" })
            });
            _audit.Indexes.CreateMany(new[] {
                new CreateIndexModel<SubscriptionUsageAuditEntry>(Builders<SubscriptionUsageAuditEntry>.IndexKeys.Ascending(x => x.UserId).Descending(x => x.OccurredAtUtc).Descending(x => x.Id), new CreateIndexOptions { Name = "ix_user_audit_cursor" }),
                new CreateIndexModel<SubscriptionUsageAuditEntry>(Builders<SubscriptionUsageAuditEntry>.IndexKeys.Ascending(x => x.OperationId).Ascending(x => x.Kind), new CreateIndexOptions { Name = "ix_operation_audit" }),
                new CreateIndexModel<SubscriptionUsageAuditEntry>(Builders<SubscriptionUsageAuditEntry>.IndexKeys.Ascending(x => x.Month).Ascending(x => x.UserId), new CreateIndexOptions { Name = "ix_month_reconciliation" })
            });
            _buckets.Indexes.CreateOne(new CreateIndexModel<SubscriptionUsageBucket>(Builders<SubscriptionUsageBucket>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.PeriodType).Ascending(x => x.Period), new CreateIndexOptions { Name = "ix_user_period", Unique = true }));
        }

        public Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate)> AuthorizeAsync(
            SubscriptionUsageEvent usageEvent, int karma, CancellationToken cancellationToken)
        {
            ValidateAuthorization(usageEvent);
            return TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                var existing = await _events.Find(session, x => x.OperationId == usageEvent.OperationId).FirstOrDefaultAsync(ct);
                if (existing != null)
                {
                    EnsureOwner(existing, usageEvent.UserId, usageEvent.ConsumingService);
                    if (existing.AuthorizationPolicy == null) throw new SubscriptionUsageConflictException("Imported legacy operations cannot be re-authorized; use their audited recovery or correction workflow.");
                    if (!SameAuthorization(existing, usageEvent))
                        throw new SubscriptionUsageConflictException("The operation id was already used for a different authorization payload.");
                    if ((existing.Status is "authorized" or "executing") && existing.ExpiresAtUtc <= _time.GetUtcNow().UtcDateTime)
                        await ExpireOperationAsync(session, existing, _time.GetUtcNow().UtcDateTime, ct);
                    SubscriptionLedgerMetrics.Duplicates.Add(1, new KeyValuePair<string, object>("operation", "authorize"), new KeyValuePair<string, object>("service", usageEvent.ConsumingService));
                    existing.AlreadyAuthorized = true;
                    return ((SubscriptionUsageEvent)existing, await LoadAggregateAsync(session, usageEvent.UserId, existing.AuthorizedAtUtc, ct));
                }

                var subscription = await Subscriptions.Find(session, x => x.UserId == usageEvent.UserId).FirstOrDefaultAsync(ct)
                    ?? throw new SubscriptionUsageLimitException("SUBSCRIPTION_REQUIRED", "A valid OASIS subscription is required.", 402);
                if (subscription.Status is not ("active" or "trialing" or "free") ||
                    subscription.CurrentPeriodEnd.HasValue && subscription.CurrentPeriodEnd.Value <= _time.GetUtcNow().UtcDateTime)
                    throw new SubscriptionUsageLimitException("INACTIVE_SUBSCRIPTION", "The OASIS subscription is inactive or expired.", 402);
                var policy = _policyProvider.GetPolicy(subscription.PlanId, karma);
                usageEvent.PlanId = subscription.PlanId; usageEvent.Karma = karma; usageEvent.AuthorizationPolicy = policy;
                // This explicit version fence linearizes reservation authorization with concurrent
                // Stripe/administrator subscription mutations, which also write this document.
                subscription.AuthorizationVersion = checked(subscription.AuthorizationVersion + 1);
                await Subscriptions.ReplaceOneAsync(session, x => x.UserId == usageEvent.UserId, subscription, cancellationToken: ct);
                var aggregate = await LoadAggregateAsync(session, usageEvent.UserId, usageEvent.AuthorizedAtUtc, ct);
                if (PaidOveragePricing.HasConsent(subscription) && !subscription.OveragePaymentBlocked)
                {
                    var pricing = _overagePricing ?? PaidOveragePricing.Load();
                    if(subscription.PaidOverageTermsVersion != pricing.Version || subscription.PaidOverageTermsSha256 != pricing.PricingSha256 || !subscription.PaidOverageTermsAcceptedAtUtc.HasValue ||
                        subscription.MonthlyOverageLimitUsd <= 0 || string.IsNullOrWhiteSpace(subscription.StripeCustomerId) ||
                        string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
                        throw new SubscriptionUsageLimitException("OVERAGE_TERMS_REQUIRED", "Accept the published overage terms and a USD spending cap before additional usage.", 402);
                    usageEvent.PaidOveragePricingVersion=pricing.Version;
                    usageEvent.ReservedRequestOverageUsd=PaidOveragePricing.RequestReservation(policy,aggregate,pricing.Rate(subscription.PlanId));
                    // Full provider exposure covers settlement order and plan changes. Only actual excess is billed.
                    usageEvent.ReservedOverageUsd=checked(usageEvent.ReservedRequestOverageUsd+usageEvent.ReservedCostUsd);
                    if(checked(aggregate.SettledOverageUsd+aggregate.ReservedOverageUsd+usageEvent.ReservedOverageUsd)>subscription.MonthlyOverageLimitUsd)
                        throw new SubscriptionUsageLimitException("OVERAGE_SPENDING_CAP_EXCEEDED", "This reservation would exceed the agreed USD monthly overage spending cap.", 402);
                }
                else
                    SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy, aggregate, usageEvent.MeterCategory == "ai.tokens" ? usageEvent.RequestedUnits : 0, usageEvent.ReservedCostUsd);
                if (usageEvent.GrantId != null)
                    await ReserveAgainstGrantAsync(session, usageEvent, ct);
                var entry = Audit(usageEvent, "authorized", usageEvent.AuthorizedAtUtc, usageEvent.ConsumingService);
                entry.RequestsDelta = 1;
                entry.ReservedUnitsDelta = usageEvent.MeterCategory == "ai.tokens" ? usageEvent.RequestedUnits : 0;
                entry.ReservedCostDeltaUsd = usageEvent.ReservedCostUsd;
                entry.ReservedOverageDeltaUsd = usageEvent.ReservedOverageUsd;
                entry.PayloadJson = JsonSerializer.Serialize(usageEvent);
                await _events.InsertOneAsync(session, UsageEventDocument.From(usageEvent), cancellationToken: ct);
                await ApplyDeltaAsync(session, usageEvent, entry, ct);
                await _audit.InsertOneAsync(session, entry, cancellationToken: ct);
                return (usageEvent, await LoadAggregateAsync(session, usageEvent.UserId, usageEvent.AuthorizedAtUtc, ct));
            }, cancellationToken);
        }

        public Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate, bool AlreadySettled)> SettleAsync(
            string userId, UsageSettlementRequest request, CancellationToken cancellationToken)
        {
            UsageLedgerValidation.ValidateSettlement(userId, request);
            string fingerprint = UsageLedgerValidation.SettlementFingerprint(request);
            return TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                var operation = await _events.Find(session, x => x.OperationId == request.OperationId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The usage authorization operation was not found.");
                EnsureOwner(operation, userId, request.ConsumingService);
                if (operation.Status == "settled")
                {
                    if (operation.SettlementFingerprint != fingerprint)
                        throw new SubscriptionUsageConflictException("The operation id was already settled with a different payload.");
                    SubscriptionLedgerMetrics.Duplicates.Add(1, new KeyValuePair<string, object>("operation", "settle"), new KeyValuePair<string, object>("service", request.ConsumingService));
                    return ((SubscriptionUsageEvent)operation, await LoadAggregateAsync(session, userId, operation.AuthorizedAtUtc, ct), true);
                }
                if (operation.Status is not ("executing" or "recovery-required"))
                    throw new SubscriptionUsageConflictException("The operation is not eligible for settlement.");

                var now = _time.GetUtcNow().UtcDateTime;
                var entry = Audit(operation, "settled", now, request.ConsumingService);
                entry.ReservedUnitsDelta = operation.MeterCategory == "ai.tokens" ? -operation.RequestedUnits : 0;
                entry.ReservedCostDeltaUsd = -operation.ReservedCostUsd;
                // Token meters reserve and settle tokens; request meters count Units separately.
                entry.SettledUnitsDelta = checked(request.PromptTokens + request.CompletionTokens);
                entry.SettledCostDeltaUsd = request.ActualCostUsd.Value;
                entry.ReservedOverageDeltaUsd = -operation.ReservedOverageUsd;
                if(operation.PaidOveragePricingVersion != null)
                {
                    var before=await LoadAggregateAsync(session,userId,operation.AuthorizedAtUtc,ct);
                    operation.SettledRequestOverageUsd=request.Outcome=="succeeded" ? operation.ReservedRequestOverageUsd : 0;
                    // Customer exposure cannot exceed what was reserved; any provider overrun remains the platform's cost.
                    operation.SettledAiOverageUsd=Math.Min(PaidOveragePricing.AiSettlement(operation.AuthorizationPolicy,before,request.ActualCostUsd.Value),
                        Math.Max(0,operation.ReservedOverageUsd-operation.SettledRequestOverageUsd));
                    entry.SettledOverageDeltaUsd=checked(operation.SettledRequestOverageUsd+operation.SettledAiOverageUsd);
                }
                if (entry.SettledCostDeltaUsd > operation.ReservedCostUsd || (operation.MeterCategory == "ai.tokens" && entry.SettledUnitsDelta > operation.RequestedUnits))
                    SubscriptionLedgerMetrics.ReservationOverruns.Add(1, new KeyValuePair<string, object>("service", request.ConsumingService));
                entry.PayloadJson = JsonSerializer.Serialize(request);
                foreach (var receipt in request.ProviderReceipts)
                {
                    string receiptId = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { receipt.Provider, receipt.ProviderRequestId }))));
                    var existingReceipt = await _receipts.Find(session, new BsonDocument("_id", receiptId)).FirstOrDefaultAsync(ct);
                    if (existingReceipt != null && existingReceipt["OperationId"].AsString != operation.OperationId)
                        throw new SubscriptionUsageConflictException("Provider receipt was already attributed to another operation.");
                    await _receipts.InsertOneAsync(session, new BsonDocument {
                        { "_id", receiptId }, { "OperationId", operation.OperationId }, { "UserId", userId },
                        { "ConsumingService", request.ConsumingService }, { "Provider", receipt.Provider },
                        { "ProviderRequestId", receipt.ProviderRequestId }, { "OccurredAtUtc", now }
                    }, cancellationToken: ct);
                }
                operation.Status = "settled";
                operation.Outcome = request.Outcome;
                operation.Provider = request.Provider;
                operation.ProviderRequestId = request.ProviderRequestId;
                operation.Model = request.Model;
                operation.Units = request.Units;
                operation.PromptTokens = request.PromptTokens;
                operation.CompletionTokens = request.CompletionTokens;
                operation.SettledCostUsd = request.ActualCostUsd.Value;
                operation.CostSource = request.CostSource;
                operation.PricingCatalogueVersion = request.PricingCatalogueVersion;
                operation.SettlementFingerprint = fingerprint;
                operation.SettledAtUtc = now;
                operation.ProviderReceiptsJson = JsonSerializer.Serialize(request.ProviderReceipts);
                if (operation.GrantId != null)
                    await ApplyGrantDeltaAsync(session, operation, -operation.ReservedCostUsd, request.ActualCostUsd.Value, now, ct);
                await ApplyDeltaAsync(session, operation, entry, ct);
                await _events.ReplaceOneAsync(session, x => x.OperationId == operation.OperationId, operation, cancellationToken: ct);
                await _audit.InsertOneAsync(session, entry, cancellationToken: ct);
                return ((SubscriptionUsageEvent)operation, await LoadAggregateAsync(session, userId, operation.AuthorizedAtUtc, ct), false);
            }, cancellationToken);
        }

        public Task<UsageStartResult> StartAsync(UsageStartRequest request, CancellationToken cancellationToken)
        {
            if (request == null || !Guid.TryParseExact(request.OperationId, "D", out _) ||
                !Guid.TryParseExact(request.UserId, "D", out _) || !SubscriptionServiceIdentity.IsService(request.ConsumingService))
                throw new ArgumentException("Canonical operation and avatar UUIDs and service identity are required.");
            return TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                var operation = await _events.Find(session, x => x.OperationId == request.OperationId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The usage operation was not found.");
                EnsureOwner(operation, request.UserId, request.ConsumingService);
                if (operation.Status == "executing" && operation.ExpiresAtUtc > _time.GetUtcNow().UtcDateTime)
                    return new UsageStartResult { Started = true, AlreadyStarted = true, OperationId = operation.OperationId, Status = operation.Status };
                if (operation.Status != "authorized" || operation.ExpiresAtUtc <= _time.GetUtcNow().UtcDateTime)
                    throw new SubscriptionUsageConflictException("The execution lease is unavailable or expired; provider execution is forbidden.");
                operation.Status = "executing";
                var entry = Audit(operation, "executing", _time.GetUtcNow().UtcDateTime, request.ConsumingService);
                entry.PayloadJson = JsonSerializer.Serialize(request);
                await _events.ReplaceOneAsync(session, x => x.OperationId == operation.OperationId, operation, cancellationToken: ct);
                await _audit.InsertOneAsync(session, entry, cancellationToken: ct);
                return new UsageStartResult { Started = true, AlreadyStarted = false, OperationId = operation.OperationId, Status = operation.Status };
            }, cancellationToken);
        }
        public Task<SubscriptionUsageAggregate> GetAggregateAsync(string userId, CancellationToken cancellationToken) =>
            GetAggregateForPeriodAsync(userId, _time.GetUtcNow().UtcDateTime, cancellationToken);

        public Task<SubscriptionUsageAggregate> GetAggregateForPeriodAsync(string userId, DateTime period, CancellationToken cancellationToken) =>
            TransactAsync((session, ct) => LoadAggregateAsync(session, userId, period, ct), cancellationToken);

        public async Task<IReadOnlyList<SubscriptionUsageEvent>> GetEventsAsync(string userId, int limit, CancellationToken cancellationToken) =>
            (await _events.Find(x => x.UserId == userId).SortByDescending(x => x.AuthorizedAtUtc).ThenByDescending(x => x.OperationId)
                .Limit(Math.Clamp(limit, 1, 500)).ToListAsync(cancellationToken)).Cast<SubscriptionUsageEvent>().ToList();

        public async Task<IReadOnlyList<SubscriptionUsageAuditEntry>> GetAuditAsync(string userId, string beforeId, int limit, CancellationToken cancellationToken)
        {
            var f = Builders<SubscriptionUsageAuditEntry>.Filter;
            var filter = f.Eq(x => x.UserId, userId);
            if (!string.IsNullOrEmpty(beforeId))
            {
                var cursor = await _audit.Find(x => x.Id == beforeId && x.UserId == userId).FirstOrDefaultAsync(cancellationToken)
                    ?? throw new ArgumentException("Audit cursor not found for this avatar.");
                filter &= f.Lt(x => x.OccurredAtUtc, cursor.OccurredAtUtc) |
                    (f.Eq(x => x.OccurredAtUtc, cursor.OccurredAtUtc) & f.Lt(x => x.Id, cursor.Id));
            }
            return await _audit.Find(filter).SortByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                .Limit(Math.Clamp(limit, 1, 500)).ToListAsync(cancellationToken);
        }

        public Task<SubscriptionUsageAuditEntry> CorrectAsync(string actor, UsageCorrectionRequest correction, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(actor) || correction == null || !Guid.TryParseExact(correction.CorrectionId, "D", out _) ||
                !Guid.TryParseExact(correction.OperationId, "D", out _) || string.IsNullOrWhiteSpace(correction.Reason) ||
                correction.Reason.Length > 2000 || string.IsNullOrWhiteSpace(correction.EvidenceReference) || correction.EvidenceReference.Length > 1000)
                throw new ArgumentException("An actor, operation UUID, correction UUID, reason and evidence reference are required.");
            if (correction.CorrectionKind is not ("billing-adjustment" or "provider-measurement"))
                throw new ArgumentException("CorrectionKind must be billing-adjustment or provider-measurement.");
            if (correction.CorrectionKind == "provider-measurement" && (string.IsNullOrWhiteSpace(correction.Provider) ||
                string.IsNullOrWhiteSpace(correction.ProviderRequestId) || correction.Provider.Length > 128 || correction.ProviderRequestId.Length > 256))
                throw new ArgumentException("A provider-measurement correction requires both provider and provider receipt id.");
            if (correction.CorrectionKind == "billing-adjustment" && (correction.Provider != null || correction.ProviderRequestId != null))
                throw new ArgumentException("A billing-adjustment cannot alter provider receipt evidence.");
            string payload = JsonSerializer.Serialize(new { correction.CorrectionId, correction.OperationId, correction.CorrectionKind,
                correction.Provider, correction.ProviderRequestId, correction.Reason, correction.EvidenceReference, correction.UnitsDelta,
                CostDeltaUsd = correction.CostDeltaUsd.ToString("G29", CultureInfo.InvariantCulture) });
            return TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                string id = "correction:" + correction.CorrectionId;
                var existing = await _audit.Find(session, x => x.Id == id).FirstOrDefaultAsync(ct);
                if (existing != null)
                {
                    if (existing.Actor != actor || existing.PayloadJson != payload)
                        throw new SubscriptionUsageConflictException("Correction id was reused by a different actor or payload.");
                    return existing;
                }
                var operation = await _events.Find(session, x => x.OperationId == correction.OperationId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The usage operation was not found.");
                if (operation.Status != "settled") throw new SubscriptionUsageConflictException("Only settled operations can receive financial corrections.");
                // Check the operation balance, not only the global bucket balance, to prohibit over-refunds.
                var previous = await _audit.Find(session, x => x.OperationId == operation.OperationId && x.Kind == "correction").ToListAsync(ct);
                if (checked(operation.SettledCostUsd + previous.Sum(x => x.SettledCostDeltaUsd) + correction.CostDeltaUsd) < 0 ||
                    checked((operation.PromptTokens + operation.CompletionTokens) + previous.Sum(x => x.SettledUnitsDelta) + correction.UnitsDelta) < 0)
                    throw new SubscriptionUsageConflictException("Correction would make this operation's net usage or cost negative.");
                if (correction.CorrectionKind == "provider-measurement")
                {
                    var receipts = JsonSerializer.Deserialize<List<UsageProviderReceipt>>(operation.ProviderReceiptsJson ?? "[]");
                    var receipt = receipts?.SingleOrDefault(x => x.Provider == correction.Provider && x.ProviderRequestId == correction.ProviderRequestId)
                        ?? throw new SubscriptionUsageConflictException("The referenced provider receipt does not belong to this operation.");
                    if(receipt.NativeFeeValuations!=null)
                        throw new SubscriptionUsageConflictException("Native measurement evidence is immutable; a generic receipt-cost delta cannot replace verified native valuation evidence.");
                    var providerCorrections = previous.Where(x => x.CorrectionKind == "provider-measurement" && x.Provider == correction.Provider && x.ProviderRequestId == correction.ProviderRequestId).ToList();
                    if (checked(receipt.ActualCostUsd + providerCorrections.Sum(x => x.SettledCostDeltaUsd) + correction.CostDeltaUsd) < 0 ||
                        checked(receipt.PromptTokens + receipt.CompletionTokens + providerCorrections.Sum(x => x.SettledUnitsDelta) + correction.UnitsDelta) < 0)
                        throw new SubscriptionUsageConflictException("Correction would make the referenced provider receipt's net usage or cost negative.");
                }
                var entry = Audit(operation, "correction", _time.GetUtcNow().UtcDateTime, actor);
                entry.CorrectionKind = correction.CorrectionKind;
                entry.Provider = correction.Provider;
                entry.ProviderRequestId = correction.ProviderRequestId;
                entry.Id = id;
                entry.Reason = correction.Reason;
                entry.ExternalReference = correction.EvidenceReference;
                entry.SettledUnitsDelta = correction.UnitsDelta;
                entry.SettledCostDeltaUsd = correction.CostDeltaUsd;
                entry.PayloadJson = payload;
                await ApplyOverageCorrectionAsync(session,operation,entry,ct);
                // Write-conflicts on shared buckets also serialize concurrent corrections to the same operation.
                await ApplyDeltaAsync(session, operation, entry, ct);
                await _audit.InsertOneAsync(session, entry, cancellationToken: ct);
                return entry;
            }, cancellationToken);
        }

        public async Task<int> ExpireAsync(DateTime now, int batchSize, CancellationToken cancellationToken)
        {
            var due = await _events.Find(x => (x.Status == "authorized" || x.Status == "executing") && x.ExpiresAtUtc <= now)
                .SortBy(x => x.ExpiresAtUtc).Limit(Math.Clamp(batchSize, 1, 1000)).ToListAsync(cancellationToken);
            int expired = 0;
            foreach (var candidate in due)
                expired += await TransactAsync(async (session, ct) =>
                {
                    var operation = await _events.Find(session, x => x.OperationId == candidate.OperationId).FirstOrDefaultAsync(ct);
                    if (operation == null || operation.Status is not ("authorized" or "executing") || operation.ExpiresAtUtc > now) return 0;
                    await ExpireOperationAsync(session, operation, now, ct);
                    return 1;
                }, cancellationToken);
            return expired;
        }

        private async Task ExpireOperationAsync(IClientSessionHandle session, UsageEventDocument operation, DateTime now, CancellationToken ct)
        {
            bool executed = operation.Status == "executing";
            operation.Status = executed ? "recovery-required" : "expired";
            var entry = Audit(operation, operation.Status, now, "WEB4:expiry-worker");
            entry.Reason = executed ? "Execution lease expired. Exposure remains reserved until provider evidence establishes the outcome."
                : "Unstarted reservation expired. All reserved exposure and the unexecuted request count are released.";
            if (!executed)
            {
                entry.RequestsDelta = -1;
                entry.ReservedUnitsDelta = operation.MeterCategory == "ai.tokens" ? -operation.RequestedUnits : 0;
                entry.ReservedCostDeltaUsd = -operation.ReservedCostUsd;
                entry.ReservedOverageDeltaUsd = -operation.ReservedOverageUsd;
                if (operation.GrantId != null)
                    await ApplyGrantDeltaAsync(session, operation, -operation.ReservedCostUsd, 0m, now, ct);
                await ApplyDeltaAsync(session, operation, entry, ct);
            }            entry.PayloadJson = JsonSerializer.Serialize(new { operation.OperationId, operation.ExpiresAtUtc });
            await _events.ReplaceOneAsync(session, x => x.OperationId == operation.OperationId, operation, cancellationToken: ct);
            await _audit.InsertOneAsync(session, entry, cancellationToken: ct);
        }

        private async Task ApplyDeltaAsync(IClientSessionHandle session, SubscriptionUsageEvent operation, SubscriptionUsageAuditEntry delta, CancellationToken ct)
        {
            foreach (var type in new[] { "month", "day" })
            {
                var bucket = await LoadBucketAsync(session, operation.UserId, operation.AuthorizedAtUtc, type, ct);
                if(type=="month" && delta.Kind=="settled")operation.SettledSequence=++bucket.SettlementSequence;
                bucket.Requests = checked(bucket.Requests + delta.RequestsDelta);
                bucket.ReservedUnits = checked(bucket.ReservedUnits + delta.ReservedUnitsDelta);
                bucket.SettledUnits = checked(bucket.SettledUnits + delta.SettledUnitsDelta);
                bucket.ReservedCostUsd = checked(bucket.ReservedCostUsd + delta.ReservedCostDeltaUsd);
                bucket.SettledCostUsd = checked(bucket.SettledCostUsd + delta.SettledCostDeltaUsd);
                bucket.ReservedOverageUsd = checked(bucket.ReservedOverageUsd + delta.ReservedOverageDeltaUsd);
                bucket.SettledOverageUsd = checked(bucket.SettledOverageUsd + delta.SettledOverageDeltaUsd);
                if (bucket.Requests < 0 || bucket.ReservedUnits < 0 || bucket.SettledUnits < 0 || bucket.ReservedCostUsd < 0 || bucket.SettledCostUsd < 0 || bucket.ReservedOverageUsd < 0 || bucket.SettledOverageUsd < 0)
                    throw new InvalidOperationException("Usage bucket invariant violated. Reconcile ledger before accepting further writes.");
                bucket.UpdatedAtUtc = delta.OccurredAtUtc;
                await _buckets.ReplaceOneAsync(session, x => x.Id == bucket.Id, bucket, new ReplaceOptions { IsUpsert = true }, ct);
            }
        }

        private async Task<SubscriptionUsageAggregate> LoadAggregateAsync(IClientSessionHandle session, string userId, DateTime time, CancellationToken ct)
        {
            var month = await LoadBucketAsync(session, userId, time, "month", ct);
            var day = await LoadBucketAsync(session, userId, time, "day", ct);
            return new SubscriptionUsageAggregate {
                Id = month.Id, UserId = userId, Month = month.Period, Day = day.Period,
                MonthlyRequests = month.Requests, DailyCalls = day.Requests, DailyTokens = day.SettledUnits,
                ReservedUnits = day.ReservedUnits, ReservedCostUsd = month.ReservedCostUsd, SettledCostUsd = month.SettledCostUsd,
                ReservedOverageUsd = month.ReservedOverageUsd, SettledOverageUsd = month.SettledOverageUsd,
                UpdatedAtUtc = month.UpdatedAtUtc > day.UpdatedAtUtc ? month.UpdatedAtUtc : day.UpdatedAtUtc
            };
        }

        private async Task<SubscriptionUsageBucket> LoadBucketAsync(IClientSessionHandle session, string userId, DateTime date, string type, CancellationToken ct)
        {
            var period = date.ToString(type == "month" ? "yyyy-MM" : "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var id = userId + ":" + type + ":" + period;
            return await _buckets.Find(session, x => x.Id == id).FirstOrDefaultAsync(ct)
                ?? new SubscriptionUsageBucket { Id = id, UserId = userId, PeriodType = type, Period = period, UpdatedAtUtc = date };
        }

        private async Task<T> TransactAsync<T>(Func<IClientSessionHandle, CancellationToken, Task<T>> callback, CancellationToken ct,
            [System.Runtime.CompilerServices.CallerMemberName] string operation = null)
        {
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            var operationTag = new KeyValuePair<string, object>("operation", operation);
            try
            {
                for (int attempt = 0; ; attempt++)
                {
                    using var session = await _client.StartSessionAsync(cancellationToken: ct);
                    try { return await session.WithTransactionAsync(callback, TransactionOptions, ct); }
                    catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey && attempt < 2)
                    { SubscriptionLedgerMetrics.RaceRetries.Add(1, operationTag); }
                    catch (MongoCommandException ex) when (ex.Code == 11000 && attempt < 2)
                    { SubscriptionLedgerMetrics.RaceRetries.Add(1, operationTag); }
                }
            }
            catch (SubscriptionUsageLimitException ex)
            {
                SubscriptionLedgerMetrics.Denied.Add(1, operationTag, new KeyValuePair<string, object>("reason", ex.Code)); throw;
            }
            catch (SubscriptionUsageConflictException)
            { SubscriptionLedgerMetrics.Conflicts.Add(1, operationTag); throw; }
            catch (Exception)
            { SubscriptionLedgerMetrics.Failures.Add(1, operationTag); throw; }
            finally { SubscriptionLedgerMetrics.Duration.Record(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds, operationTag); }
        }
        private static SubscriptionUsageAuditEntry Audit(SubscriptionUsageEvent operation, string kind, DateTime now, string actor) => new() {
            Id = operation.OperationId + ":" + kind, OperationId = operation.OperationId, UserId = operation.UserId,
            ConsumingService = operation.ConsumingService, Kind = kind, Actor = actor, OccurredAtUtc = now,
            Month = operation.AuthorizedAtUtc.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            Day = operation.AuthorizedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };

        public static void EnsureOwner(SubscriptionUsageEvent operation, string userId, string service)
        {
            if (!string.Equals(operation.UserId, userId, StringComparison.Ordinal) || !string.Equals(operation.ConsumingService, service, StringComparison.Ordinal))
                throw new SubscriptionUsageConflictException("The operation id belongs to another avatar or consuming service.");
        }

        public static bool SameAuthorization(SubscriptionUsageEvent x, SubscriptionUsageEvent y) =>
            x.ConsumingService == y.ConsumingService && x.Endpoint == y.Endpoint && x.MeterCategory == y.MeterCategory &&
            x.RequestedUnits == y.RequestedUnits && x.ReservedCostUsd == y.ReservedCostUsd && x.RequestFingerprint == y.RequestFingerprint &&
            x.GrantId == y.GrantId;

        public static void ValidateAuthorization(SubscriptionUsageEvent value)
        {
            if (value == null || !Guid.TryParseExact(value.OperationId, "D", out _) || !Guid.TryParseExact(value.UserId, "D", out _) ||
                !SubscriptionServiceIdentity.IsService(value.ConsumingService) || value.RequestFingerprint == null ||
                !System.Text.RegularExpressions.Regex.IsMatch(value.RequestFingerprint, "^[a-f0-9]{64}$") ||
                string.IsNullOrWhiteSpace(value.Endpoint) || value.Endpoint.Length > 2048 || string.IsNullOrWhiteSpace(value.MeterCategory) ||
                value.MeterCategory is not ("ai.tokens" or "api.request") || value.RequestedUnits < 0 || (value.MeterCategory == "ai.tokens" && value.RequestedUnits == 0) || value.ReservedCostUsd < 0 ||
                value.Status != "authorized" || value.AuthorizedAtUtc.Kind != DateTimeKind.Utc || value.ExpiresAtUtc <= value.AuthorizedAtUtc)
                throw new ArgumentException("Invalid usage authorization. UUIDs, service, fingerprint, endpoint, meter and positive expiry are required.");
        }

        private sealed class UsageEventDocument : SubscriptionUsageEvent
        {
            [BsonId] public ObjectId Id { get; set; }
            public static UsageEventDocument From(SubscriptionUsageEvent value) => JsonSerializer.Deserialize<UsageEventDocument>(JsonSerializer.Serialize(value));
        }
    }
}
