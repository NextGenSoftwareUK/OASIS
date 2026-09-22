using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public sealed class LedgerClock : TimeProvider
{
    public DateTime Now = new(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
    public override DateTimeOffset GetUtcNow() => new(Now);
}

public sealed class MongoLedgerTests : IAsyncLifetime
{
    private readonly string _databaseName = "ledger_tests_" + Guid.NewGuid().ToString("N");
    private MongoClient _client;
    private MongoSubscriptionUsageRepository _ledger;
    private readonly LedgerClock _clock = new();
    private readonly string _user = Guid.NewGuid().ToString("D");
    private readonly FixedPolicyProvider _policy = new();
    private static readonly SubscriptionUsagePolicy Unlimited = new() { MonthlyRequestLimit = -1 };

    public async Task InitializeAsync()
    {
        string uri = Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        if (string.IsNullOrWhiteSpace(uri)) throw new InvalidOperationException("SUBSCRIPTION_TEST_MONGODB_URI must point to a disposable replica set; integration coverage is never silently skipped.");
        var settings = MongoClientSettings.FromConnectionString(uri); settings.ApplicationName = _databaseName;
        _client = new MongoClient(settings);
        _ledger = new MongoSubscriptionUsageRepository(_client, _databaseName, _clock, _policy);
        var inventory = await _ledger.GetMigrationInventoryAsync(default);
        await _ledger.ImportOpeningBalanceAsync("test-admin", new UsageOpeningBalanceManifest { MigrationId = Guid.NewGuid().ToString(), ApprovedBy = "test-admin", SourceDigest = inventory.SourceDigest, EvidenceSha256 = new string('a', 64), Signature = new string('b', 64) }, default);
    }
    public Task DisposeAsync() => _client == null ? Task.CompletedTask : _client.DropDatabaseAsync(_databaseName);

    private sealed class FixedPolicyProvider : ISubscriptionUsagePolicyProvider
    {
        public SubscriptionUsagePolicy Current = Unlimited;
        public SubscriptionUsagePolicy GetPolicy(string planId, int karma) => Current;
    }
    private async Task<(SubscriptionUsageEvent Event, SubscriptionUsageAggregate Aggregate)> Authorize(SubscriptionUsageEvent operation, SubscriptionUsagePolicy policy, CancellationToken ct)
    {
        _policy.Current = policy;
        await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_records").UpdateOneAsync(
            new BsonDocument("_id", _user), new BsonDocument("$setOnInsert", new BsonDocument { { "PlanId", "enterprise" }, { "Status", "active" } }),
            new UpdateOptions { IsUpsert = true }, ct);
        return await _ledger.AuthorizeAsync(operation, 0, ct);
    }
    private SubscriptionUsageEvent Reservation(long units = 10, decimal cost = .05m, string service = "WEB6") => new() {
        OperationId = Guid.NewGuid().ToString("D"), UserId = _user, ConsumingService = service,
        Endpoint = "POST:/api/chat", MeterCategory = "ai.tokens", Status = "authorized", RequestFingerprint = new string('a', 64),
        RequestedUnits = units, ReservedCostUsd = cost, AuthorizedAtUtc = _clock.Now, ExpiresAtUtc = _clock.Now.AddMinutes(15)
    };
    private UsageSettlementRequest Settlement(SubscriptionUsageEvent op, decimal cost = .03m, long tokens = 7, string outcome = "succeeded") => new() {
        OperationId = op.OperationId, UserId = op.UserId, ConsumingService = op.ConsumingService, Outcome = outcome,
        Provider = "test-provider", Model = "test-model", ProviderRequestId = op.OperationId,
        PromptTokens = tokens, CompletionTokens = 0, Units = 1, ActualCostUsd = cost, EstimatedCostUsd = op.ReservedCostUsd,
        CostSource = "provider-invoice", PricingCatalogueVersion = "test-v1",
        ProviderReceipts = new() { new() { Provider = "test-provider", Model = "test-model", ProviderRequestId = op.OperationId,
            PromptTokens = tokens, Units = 1, ActualCostUsd = cost, CostSource = "provider-invoice", PricingCatalogueVersion = "test-v1" } }
    };
    private async Task Start(SubscriptionUsageEvent op) => await _ledger.StartAsync(new UsageStartRequest {
        OperationId = op.OperationId, UserId = op.UserId, ConsumingService = op.ConsumingService }, default);
    private async Task<SubscriptionUsageEvent> AuthorizeAndStart()
    {
        var op = Reservation();
        await Authorize(op, Unlimited, default);
        await Start(op);
        return op;
    }

    [Theory]
    [InlineData("WEB5")][InlineData("WEB6")][InlineData("WEB7")][InlineData("WEB8")][InlineData("WEB9")][InlineData("WEB10")]
    public async Task EveryServiceUsesSameLedgerProtocol(string service)
    {
        var op = Reservation(service: service);
        await Authorize(op, Unlimited, default); await Start(op);
        var result = await _ledger.SettleAsync(_user, Settlement(op), default);
        Assert.False(result.AlreadySettled); Assert.Equal("settled", result.Event.Status);
        Assert.Equal(7, result.Aggregate.DailyTokens); Assert.Equal(0, result.Aggregate.ReservedUnits);
        Assert.Equal(.03m, result.Aggregate.SettledCostUsd); Assert.Equal(0, result.Aggregate.ReservedCostUsd);
        Assert.Equal(3, (await _ledger.GetAuditAsync(_user, null, 100, default)).Count);
    }

    [Fact]
    public async Task ConcurrentFirstBucketCreationNeverOversubscribesTokenReservation()
    {
        var policy = new SubscriptionUsagePolicy { MonthlyRequestLimit = -1, DailyTokenLimit = 100 };
        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(async _ => {
            try { await Authorize(Reservation(), policy, default); return true; }
            catch (SubscriptionUsageLimitException ex) { Assert.Equal("DAILY_TOKEN_LIMIT_EXCEEDED", ex.Code); return false; }
        }));
        Assert.Equal(10, results.Count(x => x));
        var aggregate = await _ledger.GetAggregateAsync(_user, default);
        Assert.Equal(10, aggregate.MonthlyRequests); Assert.Equal(100, aggregate.ReservedUnits);
        Assert.Equal(10, (await _ledger.GetAuditAsync(_user, null, 100, default)).Count);
    }

    [Theory]
    [InlineData("monthly")][InlineData("daily")][InlineData("budget")]
    public async Task ConcurrentReservationLimitsAreAtomic(string dimension)
    {
        var policy = new SubscriptionUsagePolicy { MonthlyRequestLimit = dimension == "monthly" ? 3 : -1,
            DailyCallLimit = dimension == "daily" ? 3 : 0, MonthlyBudgetUsd = dimension == "budget" ? .15m : 0 };
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ => {
            try { await Authorize(Reservation(), policy, default); return true; }
            catch (SubscriptionUsageLimitException) { return false; }
        }));
        Assert.Equal(3, outcomes.Count(x => x));
    }

    [Fact]
    public async Task ConcurrentSameIdRetriesChargeExactlyOnce()
    {
        var op = Reservation();
        var reservations = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Authorize(op, Unlimited, default)));
        Assert.Single(reservations, x => !x.Event.AlreadyAuthorized);
        await Start(op);
        var settlement = Settlement(op);
        var settlements = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => _ledger.SettleAsync(_user, settlement, default)));
        Assert.Single(settlements, x => !x.AlreadySettled);
        Assert.Equal(.03m, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
        Assert.Equal(3, (await _ledger.GetAuditAsync(_user, null, 100, default)).Count);
    }

    [Fact]
    public async Task DifferentAuthorizationPayloadCannotReuseOperationId()
    {
        var op = Reservation(); await Authorize(op, Unlimited, default);
        op.RequestFingerprint = new string('b', 64);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => Authorize(op, Unlimited, default));
    }

    [Theory]
    [InlineData("user")][InlineData("service")]
    public async Task OwnershipAppliesToReserveStartAndSettlement(string attack)
    {
        var op = await AuthorizeAndStart();
        var settlement = Settlement(op);
        if (attack == "user") { op.UserId = Guid.NewGuid().ToString("D"); settlement.UserId = op.UserId; }
        else { op.ConsumingService = "WEB7"; settlement.ConsumingService = op.ConsumingService; }
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => Authorize(op, Unlimited, default));
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => Start(op));
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.SettleAsync(op.UserId, settlement, default));
    }

    [Theory]
    [InlineData("succeeded")][InlineData("failed")][InlineData("cancelled")]
    public async Task EveryOutcomePreservesActualProviderCharge(string outcome)
    {
        var op = await AuthorizeAndStart();
        await _ledger.SettleAsync(_user, Settlement(op, outcome: outcome), default);
        var aggregate = await _ledger.GetAggregateAsync(_user, default);
        Assert.Equal(.03m, aggregate.SettledCostUsd); Assert.Equal(7, aggregate.DailyTokens);
    }

    [Fact]
    public async Task SettlementRequiresExecutionStart()
    {
        var op = Reservation(); await Authorize(op, Unlimited, default);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.SettleAsync(_user, Settlement(op), default));
    }

    [Fact]
    public async Task ExpiryReleasesOnlyUnstartedReservationsAndCannotReauthorize()
    {
        var op = Reservation(); await Authorize(op, Unlimited, default);
        _clock.Now = _clock.Now.AddHours(1);
        Assert.Equal(1, await _ledger.ExpireAsync(_clock.Now, 50, default));
        Assert.Equal(0, await _ledger.ExpireAsync(_clock.Now, 50, default));
        var aggregate = await _ledger.GetAggregateAsync(_user, default);
        Assert.Equal(0, aggregate.MonthlyRequests); Assert.Equal(0, aggregate.ReservedUnits); Assert.Equal(0, aggregate.ReservedCostUsd);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => Start(op));
        var retry = await Authorize(op, Unlimited, default);
        Assert.True(retry.Event.AlreadyAuthorized); Assert.Equal("expired", retry.Event.Status);
    }

    [Fact]
    public async Task ExpiredExecutionKeepsExposureAndCanSettleAfterCrash()
    {
        var op = await AuthorizeAndStart(); _clock.Now = _clock.Now.AddHours(1);
        await _ledger.ExpireAsync(_clock.Now, 50, default);
        var aggregate = await _ledger.GetAggregateAsync(_user, default);
        Assert.Equal(.05m, aggregate.ReservedCostUsd); Assert.Equal(10, aggregate.ReservedUnits);
        Assert.Equal("recovery-required", (await _ledger.GetEventsAsync(_user, 1, default))[0].Status);
        // A new repository instance models process restart; no in-memory state is necessary.
        _ledger = new MongoSubscriptionUsageRepository(_client, _databaseName, _clock, _policy);
        await _ledger.SettleAsync(_user, Settlement(op), default);
        Assert.Equal(.03m, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
    }

    [Fact]
    public async Task SettlementAndExpiryRaceNeverLoseOrDoubleReleaseReservation()
    {
        var op = await AuthorizeAndStart(); _clock.Now = _clock.Now.AddHours(1);
        await Task.WhenAll(_ledger.ExpireAsync(_clock.Now, 50, default), _ledger.SettleAsync(_user, Settlement(op), default));
        var aggregate = await _ledger.GetAggregateAsync(_user, default);
        Assert.Equal(0, aggregate.ReservedCostUsd); Assert.Equal(0, aggregate.ReservedUnits); Assert.Equal(.03m, aggregate.SettledCostUsd);
    }

    [Fact]
    public async Task LateSettlementNeverResetsCurrentDailyOrMonthlyCounters()
    {
        _clock.Now = new DateTime(2026, 9, 30, 23, 59, 0, DateTimeKind.Utc);
        var old = await AuthorizeAndStart(); _clock.Now = _clock.Now.AddMinutes(2);
        var current = await AuthorizeAndStart();
        await _ledger.SettleAsync(_user, Settlement(current), default);
        await _ledger.SettleAsync(_user, Settlement(old), default);
        var today = await _ledger.GetAggregateAsync(_user, default);
        var previous = await _ledger.GetAggregateForPeriodAsync(_user, old.AuthorizedAtUtc, default);
        Assert.Equal(1, today.MonthlyRequests); Assert.Equal(1, today.DailyCalls); Assert.Equal(7, today.DailyTokens);
        Assert.Equal(1, previous.MonthlyRequests); Assert.Equal(7, previous.DailyTokens);
    }

    [Fact]
    public async Task SettlementFingerprintRejectsChangedEvidenceEvenWhenPriceMatches()
    {
        var op = await AuthorizeAndStart(); var settlement = Settlement(op);
        await _ledger.SettleAsync(_user, settlement, default);
        settlement.EstimatedCostUsd += .01m;
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.SettleAsync(_user, settlement, default));
    }

    [Fact]
    public async Task ReceiptCannotBeChargedToTwoOperations()
    {
        var first = await AuthorizeAndStart(); var second = await AuthorizeAndStart();
        var firstSettlement = Settlement(first); await _ledger.SettleAsync(_user, firstSettlement, default);
        var duplicate = Settlement(second); duplicate.ProviderRequestId = first.OperationId; duplicate.ProviderReceipts[0].ProviderRequestId = first.OperationId;
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.SettleAsync(_user, duplicate, default));
        Assert.Equal(.03m, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
    }

    [Fact]
    public async Task CorrectionsAreIdempotentAppendOnlyAndProhibitOverRefunds()
    {
        var op = await AuthorizeAndStart(); await _ledger.SettleAsync(_user, Settlement(op), default);
        var correction = new UsageCorrectionRequest { CorrectionId = Guid.NewGuid().ToString(), OperationId = op.OperationId,
            CorrectionKind = "billing-adjustment", CostDeltaUsd = -.01m, UnitsDelta = -2, Reason = "Provider refund", EvidenceReference = "provider:refund:123" };
        var first = await _ledger.CorrectAsync("admin", correction, default);
        var retry = await _ledger.CorrectAsync("admin", correction, default); Assert.Equal(first.Id, retry.Id);
        var projection = (await _ledger.GetEventsAsync(_user, 10, default)).Single();
        Assert.Equal(.03m, projection.SettledCostUsd);
        Assert.Equal(.02m, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
        Assert.Equal(4, (await _ledger.GetAuditAsync(_user, null, 100, default)).Count);
        correction.CorrectionId = Guid.NewGuid().ToString(); correction.CostDeltaUsd = -.03m;
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.CorrectAsync("admin", correction, default));
    }

    [Fact]
    public async Task ConcurrentCorrectionsCannotOverRefundOperation()
    {
        var op = await AuthorizeAndStart(); await _ledger.SettleAsync(_user, Settlement(op), default);
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ => {
            try { await _ledger.CorrectAsync("admin", new UsageCorrectionRequest { CorrectionId = Guid.NewGuid().ToString(),
                OperationId = op.OperationId, CorrectionKind = "billing-adjustment", CostDeltaUsd = -.01m, Reason = "Verified refund", EvidenceReference = "refund:123" }, default); return true; }
            catch (SubscriptionUsageConflictException) { return false; }
        }));
        Assert.Equal(3, results.Count(x => x));
        Assert.Equal(0m, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
    }

    [Fact]
    public async Task Decimal128PreservesFractionalCostExactly()
    {
        var op = await AuthorizeAndStart(); const decimal cost = 0.000000123456789012345678901m;
        await _ledger.SettleAsync(_user, Settlement(op, cost), default);
        Assert.Equal(cost, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
        var raw = await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_usage_events").Find(new BsonDocument("OperationId", op.OperationId)).SingleAsync();
        Assert.Equal(BsonType.Decimal128, raw["SettledCostUsd"].BsonType);
    }

    [Fact]
    public async Task AuditCursorUsesTimestampAndIdWithoutDroppingTies()
    {
        var op = await AuthorizeAndStart(); await _ledger.SettleAsync(_user, Settlement(op), default);
        var first = await _ledger.GetAuditAsync(_user, null, 2, default);
        var second = await _ledger.GetAuditAsync(_user, first.Last().Id, 2, default);
        Assert.Equal(3, first.Concat(second).Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public async Task EquivalentDecimalScalesAreIdempotent()
    {
        var op = await AuthorizeAndStart(); var settlement = Settlement(op, .10m);
        await _ledger.SettleAsync(_user, settlement, default);
        settlement.ActualCostUsd = .1m; settlement.ProviderReceipts[0].ActualCostUsd = .1m;
        Assert.True((await _ledger.SettleAsync(_user, settlement, default)).AlreadySettled);
    }

    [Theory]
    [InlineData("actual")][InlineData("negative")][InlineData("provider")][InlineData("source")]
    [InlineData("version")][InlineData("receipt")][InlineData("totals")][InlineData("outcome")]
    public async Task InvalidMeasuredSettlementNeverMutatesLedger(string invalid)
    {
        var op = await AuthorizeAndStart(); var settlement = Settlement(op);
        switch (invalid)
        {
            case "actual": settlement.ActualCostUsd = null; break;
            case "negative": settlement.PromptTokens = -1; break;
            case "provider": settlement.Provider = ""; break;
            case "source": settlement.CostSource = ""; break;
            case "version": settlement.PricingCatalogueVersion = ""; break;
            case "receipt": settlement.ProviderReceipts.Clear(); break;
            case "totals": settlement.ActualCostUsd = .04m; break;
            case "outcome": settlement.Outcome = "unknown"; break;
        }
        await Assert.ThrowsAsync<ArgumentException>(() => _ledger.SettleAsync(_user, settlement, default));
        var aggregate = await _ledger.GetAggregateAsync(_user, default);
        Assert.Equal(0, aggregate.SettledCostUsd); Assert.Equal(.05m, aggregate.ReservedCostUsd);
    }

    [Fact]
    public async Task MigrationBatchesPreserveLegacyExposureAndDoNotOpenGateEarly()
    {
        var db = _client.GetDatabase(_databaseName);
        await db.GetCollection<BsonDocument>("subscription_usage_migrations").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        var legacy = Reservation();
        await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument {
            { "_id", ObjectId.GenerateNewId() }, { "OperationId", legacy.OperationId }, { "UserId", _user }, { "ConsumingService", "WEB6" },
            { "Endpoint", legacy.Endpoint }, { "MeterCategory", "chat" }, { "Status", "authorized" }, { "RequestedUnits", 10L },
            { "ReservedCostUsd", new BsonDecimal128(.05m) }, { "AuthorizedAtUtc", _clock.Now }
        });
        await db.GetCollection<BsonDocument>("subscription_usage_aggregates").InsertOneAsync(new BsonDocument {
            { "_id", _user + ":2026-09" }, { "UserId", _user }, { "Month", "2026-09" }, { "Day", "2026-09-22" }, { "MonthlyRequests", 5L }
        });
        var inventory = await _ledger.GetMigrationInventoryAsync(default);
        var batch = new UsageOpeningBalanceManifest {
            MigrationId = Guid.NewGuid().ToString(), ApprovedBy = "admin", SourceDigest = inventory.SourceDigest,
            EvidenceSha256 = new string('a', 64), Signature = new string('b', 64), BatchCount = 2, BatchIndex = 0,
            Balances = new() { new() { UserId = _user, Day = "2026-09-22", Requests = 5, ReservedTokens = 10, ReservedCostUsd = .05m } }
        };
        await _ledger.ImportOpeningBalanceAsync("admin", batch, default);
        Assert.False((await _ledger.GetMigrationInventoryAsync(default)).AlreadyImported);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => Authorize(Reservation(), Unlimited, default));
        Assert.Equal(batch.MigrationId, await _ledger.ImportOpeningBalanceAsync("admin", batch, default));
        batch.BatchIndex = 1; batch.Balances = new() { new() { UserId = _user, Day = "2026-09-23", Requests = 2 } };
        await _ledger.ImportOpeningBalanceAsync("admin", batch, default);
        Assert.True((await _ledger.GetMigrationInventoryAsync(default)).AlreadyImported);
        Assert.Equal(inventory.SourceDigest, (await _ledger.GetMigrationInventoryAsync(default)).SourceDigest);
        Assert.Equal(7, (await _ledger.GetAggregateAsync(_user, default)).MonthlyRequests);
        Assert.Equal(1, await db.GetCollection<BsonDocument>("subscription_usage_legacy_archive").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        await _ledger.SettleAsync(_user, Settlement(legacy), default);
        Assert.Equal(.03m, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
        await Authorize(Reservation(), Unlimited, default);
    }

    [Fact]
    public async Task ConcurrentOpeningBalanceBatchesFinalizeExactlyOnce()
    {
        var db = _client.GetDatabase(_databaseName);
        await db.GetCollection<BsonDocument>("subscription_usage_migrations").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        var inventory = await _ledger.GetMigrationInventoryAsync(default); string id = Guid.NewGuid().ToString();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(index => _ledger.ImportOpeningBalanceAsync("admin", new UsageOpeningBalanceManifest {
            MigrationId = id, ApprovedBy = "admin", SourceDigest = inventory.SourceDigest, EvidenceSha256 = new string('a', 64), Signature = new string('b', 64),
            BatchCount = 8, BatchIndex = index, Balances = new() { new() { UserId = _user, Day = $"2026-09-{index + 1:D2}", Requests = 1 } }
        }, default)));
        Assert.True((await _ledger.GetMigrationInventoryAsync(default)).AlreadyImported);
        Assert.Equal(8, (await _ledger.GetAggregateAsync(_user, default)).MonthlyRequests);
        Assert.Equal(8, (await _ledger.GetAuditAsync(_user, null, 100, default)).Count);
    }

    [Fact]
    public async Task OpeningBalanceSourceDriftAndIncompleteCoverageAreRejected()
    {
        var db = _client.GetDatabase(_databaseName);
        await db.GetCollection<BsonDocument>("subscription_usage_migrations").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        var inventory = await _ledger.GetMigrationInventoryAsync(default);
        var manifest = new UsageOpeningBalanceManifest { MigrationId = Guid.NewGuid().ToString(), ApprovedBy = "admin", SourceDigest = inventory.SourceDigest,
            EvidenceSha256 = new string('a', 64), Signature = new string('b', 64) };
        await db.GetCollection<BsonDocument>("subscription_usage_aggregates").InsertOneAsync(new BsonDocument {
            { "_id", _user + ":2026-09" }, { "UserId", _user }, { "Month", "2026-09" }
        });
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.ImportOpeningBalanceAsync("admin", manifest, default));
        manifest.SourceDigest = (await _ledger.GetMigrationInventoryAsync(default)).SourceDigest;
        await Assert.ThrowsAsync<ArgumentException>(() => _ledger.ImportOpeningBalanceAsync("admin", manifest, default));
        Assert.False((await _ledger.GetMigrationInventoryAsync(default)).AlreadyImported);
    }
    [Fact]
    public async Task ReviewedLegacySubscriptionsAndInvoicesImportWithoutRuntimeFallback()
    {
        var db = _client.GetDatabase(_databaseName);
        await db.GetCollection<BsonDocument>("subscription_usage_migrations").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        var inventory = await _ledger.GetMigrationInventoryAsync(default);
        var manifest = new UsageOpeningBalanceManifest { MigrationId = Guid.NewGuid().ToString(), ApprovedBy = "admin", SourceDigest = inventory.SourceDigest,
            EvidenceSha256 = new string('a', 64), Signature = new string('b', 64), Subscriptions = new() { CanonicalSubscription() }, Orders = new() { Invoice() } };
        await _ledger.ImportOpeningBalanceAsync("admin", manifest, default);
        Assert.Equal(manifest.MigrationId, await _ledger.ImportOpeningBalanceAsync("admin", manifest, default));
        Assert.Equal("bronze", (await _ledger.GetSubscriptionAsync(_user)).PlanId); Assert.Single(await _ledger.GetOrdersAsync(_user));
        Assert.Null(await _ledger.GetSubscriptionAsync(Guid.NewGuid().ToString()));
        Assert.Equal(2, await db.GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task BillingCannotBypassIncompleteOpeningBalanceFence()
    {
        await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_usage_migrations").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.SaveSubscriptionAsync(new SubscriptionRecord { UserId = _user, PlanId = "free", Status = "active" }));
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.AddOrderAsync(Invoice()));
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.ApplyStripeEventAsync("evt_one", new string('a', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription()), null, default));
    }

    [Fact]
    public async Task StripeInvoiceOwnerMismatchRollsBackSubscriptionMutation()
    {
        var invoice = Invoice(); invoice.UserId = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.ApplyStripeEventAsync("evt_one", new string('a', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription()), invoice, default));
        Assert.Null(await _ledger.GetSubscriptionAsync(_user)); Assert.Empty(await _ledger.GetOrdersAsync(_user));
    }
    private sealed class BlockingPolicyProvider : ISubscriptionUsagePolicyProvider
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;
        public SubscriptionUsagePolicy GetPolicy(string planId, int karma)
        {
            if (Interlocked.Increment(ref _calls) == 1) { Entered.SetResult(); Release.Task.GetAwaiter().GetResult(); }
            return new SubscriptionUsagePolicyProvider().GetPolicy(planId, karma);
        }
    }

    [Theory]
    [InlineData("cancelled")][InlineData("downgrade")]
    public async Task SubscriptionChangesBeforeReservationCommitAreRechecked(string change)
    {
        await _ledger.ApplyStripeEventAsync("evt_active", new string('a', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription()), null, default);
        var policies = new BlockingPolicyProvider();
        var concurrent = new MongoSubscriptionUsageRepository(_client, _databaseName, _clock, policies);
        var op = Reservation(units: change == "downgrade" ? 60000 : 10);
        var reservation = concurrent.AuthorizeAsync(op, 0, default);
        await policies.Entered.Task;
        await _ledger.ApplyStripeEventAsync("evt_changed", new string('b', 64), _user, _clock.Now, _ => {
            var record = CanonicalSubscription(plan: change == "downgrade" ? "free" : "bronze");
            if (change == "cancelled") record.Status = "canceled";
            return Task.FromResult(record);
        }, null, default);
        policies.Release.SetResult();
        var error = await Assert.ThrowsAsync<SubscriptionUsageLimitException>(() => reservation);
        Assert.Equal(change == "cancelled" ? "INACTIVE_SUBSCRIPTION" : "DAILY_TOKEN_LIMIT_EXCEEDED", error.Code);
        Assert.Empty(await _ledger.GetEventsAsync(_user, 100, default));
        Assert.Equal(0, (await _ledger.GetAggregateAsync(_user, default)).MonthlyRequests);
    }
    [Theory]
    [InlineData("valid")][InlineData("other-receipt")][InlineData("half-binding")][InlineData("billing-bound")][InlineData("missing-kind")]
    public async Task ProviderCorrectionsRequireExplicitOwnedReceiptBinding(string mode)
    {
        var operation = await AuthorizeAndStart(); await _ledger.SettleAsync(_user, Settlement(operation), default);
        var correction = new UsageCorrectionRequest { CorrectionId = Guid.NewGuid().ToString(), OperationId = operation.OperationId,
            CorrectionKind = mode == "missing-kind" ? null : mode == "billing-bound" ? "billing-adjustment" : "provider-measurement", Provider = "test-provider",
            ProviderRequestId = mode == "half-binding" ? null : mode == "other-receipt" ? Guid.NewGuid().ToString() : operation.OperationId,
            CostDeltaUsd = .01m, Reason = "Provider invoice reconciliation", EvidenceReference = "invoice:123" };
        if (mode == "other-receipt")
            await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.CorrectAsync("admin", correction, default));
        else if (mode != "valid")
            await Assert.ThrowsAsync<ArgumentException>(() => _ledger.CorrectAsync("admin", correction, default));
        else
        {
            var applied = await _ledger.CorrectAsync("admin", correction, default);
            Assert.Equal("provider-measurement", applied.CorrectionKind); Assert.Equal(operation.OperationId, applied.ProviderRequestId);
            Assert.Equal(applied.Id, (await _ledger.CorrectAsync("admin", correction, default)).Id);
            Assert.Equal(.04m, (await _ledger.GetAggregateAsync(_user, default)).SettledCostUsd);
            Assert.Equal(.03m, (await _ledger.GetEventsAsync(_user, 1, default)).Single().SettledCostUsd);
        }
    }
    [Fact]
    public async Task OldCancelledStripeEventCannotUndoSubsequentFreeActivation()
    {
        Task<SubscriptionRecord> Canceled(CancellationToken _) { var record = CanonicalSubscription(); record.Status = "canceled"; return Task.FromResult(record); }
        await _ledger.ApplyStripeEventAsync("evt_canceled", new string('a', 64), _user, _clock.Now, Canceled, null, default);
        await _ledger.SaveSubscriptionAsync(new SubscriptionRecord { UserId = _user, PlanId = "free", Status = "active" });
        await _ledger.ApplyStripeEventAsync("evt_old_canceled", new string('b', 64), _user, _clock.Now, Canceled, null, default);
        var current = await _ledger.GetSubscriptionAsync(_user);
        Assert.Equal("free", current.PlanId); Assert.Equal("active", current.Status);
    }
    [Fact]
    public async Task FreeActivationTransactionRetryPreservesRequestAndRetiredStripeIdentity()
    {
        await _ledger.ApplyStripeEventAsync("evt_canceled", new string('a', 64), _user, _clock.Now, _ => {
            var record = CanonicalSubscription(); record.Status = "canceled"; return Task.FromResult(record);
        }, null, default);
        await _ledger.SetPayAsYouGoAsync(_user, true);
        var request = new SubscriptionRecord { UserId = _user, PlanId = "free", Status = "active" };
        var admin = _client.GetDatabase("admin");
        await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
            { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times", 1) },
            { "data", new BsonDocument { { "failCommands", new BsonArray { "update" } }, { "errorCode", 112 },
                { "errorLabels", new BsonArray { "TransientTransactionError" } }, { "appName", _databaseName } } }
        });
        try { await _ledger.SaveSubscriptionAsync(request); }
        finally { await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } }); }
        Assert.Null(request.StripeSubscriptionId); Assert.Null(request.FreePlanActivatedAtUtc);
        await _ledger.SaveSubscriptionAsync(request);
        var saved = await _ledger.GetSubscriptionAsync(_user);
        Assert.Equal("free", saved.PlanId); Assert.Equal("sub_current", saved.StripeSubscriptionId);
        Assert.True(saved.PayAsYouGoEnabled);
        Assert.Equal(1, await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_billing_audit")
            .CountDocumentsAsync(new BsonDocument("Kind", "free-plan-activation")));
    }
    private SubscriptionRecord CanonicalSubscription(string id = "sub_current", string plan = "bronze", int ageDays = 1) => new() {
        UserId = _user, StripeSubscriptionId = id, StripeCustomerId = "cus_" + _user, PlanId = plan, Status = "active",
        StripeSubscriptionCreatedAtUtc = _clock.Now.AddDays(-ageDays), CurrentPeriodStart = _clock.Now.AddDays(-1), CurrentPeriodEnd = _clock.Now.AddDays(30)
    };
    private OrderRecord Invoice(string id = "in_one", decimal amount = 9m) => new() {
        UserId = _user, StripeInvoiceId = id, PlanId = "bronze", Amount = amount, Currency = "USD", Status = "paid", CreatedAt = _clock.Now
    };

    [Fact]
    public async Task StripeEventAndInvoiceAreAtomicIdempotentAndAudited()
    {
        Assert.True(await _ledger.ApplyStripeEventAsync("evt_one", new string('a', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription()), Invoice(), default));
        Assert.False(await _ledger.ApplyStripeEventAsync("evt_one", new string('a', 64), _user, _clock.Now,
            _ => throw new InvalidOperationException("Duplicate must not refetch canonical state"), Invoice(), default));
        Assert.Single(await _ledger.GetOrdersAsync(_user));
        Assert.Equal("bronze", (await _ledger.GetSubscriptionAsync(_user)).PlanId);
        Assert.Equal(2, await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.ApplyStripeEventAsync("evt_one", new string('b', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription()), Invoice(), default));
    }

    [Fact]
    public async Task ConcurrentInvoiceDeliveryCannotDuplicateOrLoseOrders()
    {
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => _ledger.AddOrderAsync(Invoice())));
        await Task.WhenAll(Enumerable.Range(0, 10).Select(index => _ledger.AddOrderAsync(Invoice("in_" + index))));
        Assert.Equal(11, (await _ledger.GetOrdersAsync(_user)).Count);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.AddOrderAsync(Invoice(amount: 8m)));
        var foreign = Invoice(); foreign.UserId = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.AddOrderAsync(foreign));
    }

    [Fact]
    public async Task OldCheckoutCannotReplaceNewerStripeSubscription()
    {
        await _ledger.ApplyStripeEventAsync("evt_new", new string('a', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription("sub_new", "silver", 1)), null, default);
        await _ledger.ApplyStripeEventAsync("evt_old", new string('b', 64), _user, _clock.Now.AddDays(-10),
            _ => Task.FromResult(CanonicalSubscription("sub_old", "bronze", 10)), Invoice(), default);
        Assert.Equal("silver", (await _ledger.GetSubscriptionAsync(_user)).PlanId);
        Assert.Single(await _ledger.GetOrdersAsync(_user));
    }

    [Fact]
    public async Task ConcurrentStripeCallbacksRefetchCanonicalStateAfterWriteConflict()
    {
        await _ledger.ApplyStripeEventAsync("evt_initial", new string('a', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription()), null, default);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int fetches = 0;
        var oldDelivery = _ledger.ApplyStripeEventAsync("evt_old_read", new string('b', 64), _user, _clock.Now, async _ => {
            if (Interlocked.Increment(ref fetches) == 1) { entered.SetResult(); await release.Task; return CanonicalSubscription(plan: "bronze"); }
            return CanonicalSubscription(plan: "silver");
        }, null, default);
        await entered.Task;
        await _ledger.ApplyStripeEventAsync("evt_current", new string('c', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription(plan: "silver")), null, default);
        release.SetResult(); await oldDelivery;
        Assert.True(fetches >= 2); Assert.Equal("silver", (await _ledger.GetSubscriptionAsync(_user)).PlanId);
    }

    [Fact]
    public async Task StripeLookupFailureCannotCommitPartialSubscriptionOrInvoice()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _ledger.ApplyStripeEventAsync("evt_unavailable", new string('a', 64), _user, _clock.Now,
            _ => throw new InvalidOperationException("Stripe unavailable"), Invoice(), default));
        Assert.Null(await _ledger.GetSubscriptionAsync(_user)); Assert.Empty(await _ledger.GetOrdersAsync(_user));
        Assert.Equal(0, await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task FreeActivationCannotOverrideActivePaidSubscription()
    {
        await _ledger.ApplyStripeEventAsync("evt_one", new string('a', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription()), null, default);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.SaveSubscriptionAsync(new SubscriptionRecord { UserId = _user, PlanId = "free", Status = "active" }));
        await _ledger.SetPayAsYouGoAsync(_user, true);
        await _ledger.ApplyStripeEventAsync("evt_two", new string('b', 64), _user, _clock.Now,
            _ => Task.FromResult(CanonicalSubscription(plan: "silver")), null, default);
        Assert.True((await _ledger.GetSubscriptionAsync(_user)).PayAsYouGoEnabled);
    }
    [Fact]
    public async Task TransactionFailureRollsBackEventBucketsAndAuditTogether()
    {
        // failCommand is explicitly enabled only on the disposable test replica set.
        var admin = _client.GetDatabase("admin");
        await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
            { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times", 1) },
            { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "errorCode", 2 }, { "appName", _databaseName } } }
        });
        try
        {
            var op = Reservation(); await Assert.ThrowsAnyAsync<MongoException>(() => Authorize(op, Unlimited, default));
            Assert.Empty(await _ledger.GetEventsAsync(_user, 100, default));
            Assert.Empty(await _ledger.GetAuditAsync(_user, null, 100, default));
            Assert.Equal(0, (await _ledger.GetAggregateAsync(_user, default)).MonthlyRequests);
        }
        finally { await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } }); }
    }
}

public sealed class EmptyLedgerInitializationTests : IAsyncLifetime
{
    private readonly string _databaseName = "ledger_empty_init_" + Guid.NewGuid().ToString("N");
    private MongoClient _client;

    public Task InitializeAsync()
    {
        string uri = Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        if (string.IsNullOrWhiteSpace(uri))
            throw new InvalidOperationException("SUBSCRIPTION_TEST_MONGODB_URI must point to a disposable replica set.");
        _client = new MongoClient(uri);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => _client == null ? Task.CompletedTask : _client.DropDatabaseAsync(_databaseName);

    [Fact]
    public async Task ExplicitEmptyInstallationCreatesFenceAndPermitsFirstSubscription()
    {
        var ledger = new MongoSubscriptionUsageRepository(_client, _databaseName, TimeProvider.System,
            initializeEmptyLedger: true);
        string user = Guid.NewGuid().ToString("D");

        await ledger.SaveSubscriptionAsync(new SubscriptionRecord
        {
            UserId = user, PlanId = "free", Status = "free", CreatedAt = DateTime.UtcNow
        });

        var marker = await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_usage_migrations")
            .Find(new BsonDocument("_id", "v1-opening-balance")).SingleAsync();
        Assert.Equal(SubscriptionMongoConfiguration.EmptyInstallation, marker["InitializationMode"].AsString);
    }

    [Fact]
    public async Task EmptyInstallationRefusesAnyExistingLedgerOrBillingData()
    {
        await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_records")
            .InsertOneAsync(new BsonDocument { { "_id", Guid.NewGuid().ToString("D") }, { "PlanId", "free" } });
        var ledger = new MongoSubscriptionUsageRepository(_client, _databaseName, TimeProvider.System,
            initializeEmptyLedger: true);

        var error = await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() =>
            ledger.SaveSubscriptionAsync(new SubscriptionRecord
            {
                UserId = Guid.NewGuid().ToString("D"), PlanId = "free", Status = "free", CreatedAt = DateTime.UtcNow
            }));
        Assert.Contains("EMPTY_LEDGER_INITIALIZATION_REFUSED", error.Message);
    }
}

public sealed class IdentityAndValidationTests
{
    [Theory]
    [InlineData("WEB5")][InlineData("WEB6")][InlineData("WEB7")][InlineData("WEB8")][InlineData("WEB9")][InlineData("WEB10")]
    public void ServiceCredentialBindsDeclaredService(string service)
    {
        var context = new DefaultHttpContext(); context.Request.Headers["X-OASIS-Service"] = service;
        context.Request.Headers["X-OASIS-Service-Key"] = new string('k', 32);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string> { ["SUBSCRIPTION_SERVICE_KEY_" + service] = new string('k', 32) }).Build();
        Assert.Equal(service, SubscriptionServiceIdentity.RequireService(context, config, service));
        Assert.Throws<UnauthorizedAccessException>(() => SubscriptionServiceIdentity.RequireService(context, config, "OTHER"));
        context.Request.Headers["X-OASIS-Service-Key"] = new string('x', 32);
        Assert.Throws<UnauthorizedAccessException>(() => SubscriptionServiceIdentity.RequireService(context, config, service));
    }
    [Fact]
    public void AdminRequiresAuthenticatedClaimAndServerAllowlist()
    {
        var id = Guid.NewGuid().ToString(); var context = new DefaultHttpContext();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string> { ["SUBSCRIPTION_ADMIN_AVATAR_IDS"] = id }).Build();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", id), new Claim("oasis.subscription.admin", "true") }));
        Assert.Throws<UnauthorizedAccessException>(() => SubscriptionServiceIdentity.RequireAdministrator(context, config));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(context.User.Claims, "Bearer"));
        Assert.Equal(id, SubscriptionServiceIdentity.RequireAdministrator(context, config));
        Assert.Throws<UnauthorizedAccessException>(() => SubscriptionServiceIdentity.RequireAdministrator(context, new ConfigurationBuilder().Build()));
    }
    [Fact]
    public void TokenPolicyIncludesReservedTokensBeforeProviderCompletes()
    {
        Assert.Throws<SubscriptionUsageLimitException>(() => SubscriptionUsagePolicyEvaluator.EnsureAuthorized(
            new SubscriptionUsagePolicy { MonthlyRequestLimit = -1, DailyTokenLimit = 10 },
            new SubscriptionUsageAggregate { DailyTokens = 1, ReservedUnits = 9 }, 1, 0));
    }
}
