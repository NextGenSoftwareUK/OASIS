using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

/// <summary>Delegated grants reserve, settle and expire inside the same transaction as the usage operation.</summary>
public sealed class DelegatedGrantLedgerTests : IAsyncLifetime
{
    private readonly string _databaseName = "grant_tests_" + Guid.NewGuid().ToString("N");
    private MongoClient _client;
    private MongoSubscriptionUsageRepository _ledger;
    private readonly LedgerClock _clock = new();
    private readonly string _user = Guid.NewGuid().ToString("D");

    public async Task InitializeAsync()
    {
        string uri = Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        if (string.IsNullOrWhiteSpace(uri)) throw new InvalidOperationException("SUBSCRIPTION_TEST_MONGODB_URI must point to a disposable replica set; integration coverage is never silently skipped.");
        _client = new MongoClient(uri);
        _ledger = new MongoSubscriptionUsageRepository(_client, _databaseName, _clock, new UnlimitedPolicy());
        var inventory = await _ledger.GetMigrationInventoryAsync(default);
        await _ledger.ImportOpeningBalanceAsync("test-admin", new UsageOpeningBalanceManifest { MigrationId = Guid.NewGuid().ToString(), ApprovedBy = "test-admin", SourceDigest = inventory.SourceDigest, EvidenceSha256 = new string('a', 64), Signature = new string('b', 64) }, default);
        await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_records").InsertOneAsync(
            new BsonDocument { { "_id", _user }, { "PlanId", "enterprise" }, { "Status", "active" } });
    }
    public Task DisposeAsync() => _client == null ? Task.CompletedTask : _client.DropDatabaseAsync(_databaseName);

    private sealed class UnlimitedPolicy : ISubscriptionUsagePolicyProvider
    {
        public SubscriptionUsagePolicy GetPolicy(string planId, int karma) => new() { MonthlyRequestLimit = -1 };
    }

    private Task<DelegatedUsageGrant> Grant(decimal budget = 1m, decimal perOperation = .5m, string endpoint = "POST:/v1/agent/run") =>
        _ledger.CreateGrantAsync(_user, new CreateDelegatedUsageGrantRequest {
            ConsumingService = "WEB6", Scope = "web6:schedule:test", Endpoints = new() { endpoint },
            MonthlyBudgetUsd = budget, MaxOperationCostUsd = perOperation, ExpiresAtUtc = _clock.Now.AddDays(30) }, default);

    private SubscriptionUsageEvent Reservation(string grantId, decimal cost = .4m, string endpoint = "POST:/v1/agent/run") => new() {
        OperationId = Guid.NewGuid().ToString("D"), UserId = _user, ConsumingService = "WEB6", GrantId = grantId,
        Endpoint = endpoint, MeterCategory = "ai.tokens", Status = "authorized", RequestFingerprint = new string('a', 64),
        RequestedUnits = 100, ReservedCostUsd = cost, AuthorizedAtUtc = _clock.Now, ExpiresAtUtc = _clock.Now.AddMinutes(15)
    };

    private UsageSettlementRequest Settlement(SubscriptionUsageEvent op, decimal cost) => new() {
        OperationId = op.OperationId, UserId = op.UserId, ConsumingService = op.ConsumingService, Outcome = "succeeded",
        Provider = "test-provider", Model = "test-model", ProviderRequestId = op.OperationId, PromptTokens = 10, Units = 1,
        ActualCostUsd = cost, EstimatedCostUsd = op.ReservedCostUsd, CostSource = "provider-invoice", PricingCatalogueVersion = "test-v1",
        ProviderReceipts = new() { new() { Provider = "test-provider", Model = "test-model", ProviderRequestId = op.OperationId,
            PromptTokens = 10, Units = 1, ActualCostUsd = cost, CostSource = "provider-invoice", PricingCatalogueVersion = "test-v1" } }
    };

    private async Task<string> Denial(SubscriptionUsageEvent op) =>
        (await Assert.ThrowsAsync<SubscriptionUsageLimitException>(() => _ledger.AuthorizeAsync(op, 0, default))).Code;

    [Fact]
    public async Task SettlementMovesReservedExposureToSettledSpendOnTheGrant()
    {
        var grant = await Grant();
        var op = Reservation(grant.GrantId);
        await _ledger.AuthorizeAsync(op, 0, default);
        Assert.Equal(.4m, (await _ledger.GetGrantAsync(grant.GrantId, default)).MonthReservedCostUsd);

        await _ledger.StartAsync(new UsageStartRequest { OperationId = op.OperationId, UserId = _user, ConsumingService = "WEB6" }, default);
        await _ledger.SettleAsync(_user, Settlement(op, .25m), default);

        var after = await _ledger.GetGrantAsync(grant.GrantId, default);
        Assert.Equal(0m, after.MonthReservedCostUsd);
        Assert.Equal(.25m, after.MonthSettledCostUsd);
    }

    [Fact]
    public async Task MonthlyBudgetCountsReservedAndSettledExposure()
    {
        var grant = await Grant(budget: 1m, perOperation: .5m);
        await _ledger.AuthorizeAsync(Reservation(grant.GrantId, .5m), 0, default);
        await _ledger.AuthorizeAsync(Reservation(grant.GrantId, .5m), 0, default);
        Assert.Equal("GRANT_BUDGET_EXCEEDED", await Denial(Reservation(grant.GrantId, .01m)));
    }

    [Fact]
    public async Task PerOperationCapEndpointAllowListRevocationAndExpiryAreEnforced()
    {
        var grant = await Grant();
        Assert.Equal("GRANT_OPERATION_CAP_EXCEEDED", await Denial(Reservation(grant.GrantId, .51m)));
        Assert.Equal("GRANT_ENDPOINT_NOT_ALLOWED", await Denial(Reservation(grant.GrantId, endpoint: "POST:/v1/complete")));

        await _ledger.RevokeGrantAsync(_user, grant.GrantId, default);
        Assert.Equal("GRANT_REVOKED", await Denial(Reservation(grant.GrantId)));

        var second = await Grant();
        _clock.Now = _clock.Now.AddDays(31);
        Assert.Equal("GRANT_EXPIRED", await Denial(Reservation(second.GrantId)));
    }

    [Fact]
    public async Task GrantCannotReserveForAnotherAvatar()
    {
        var grant = await Grant();
        var op = Reservation(grant.GrantId);
        op.UserId = Guid.NewGuid().ToString("D");
        // The other avatar has its own active subscription, so only the grant binding can refuse the reservation.
        await _client.GetDatabase(_databaseName).GetCollection<BsonDocument>("subscription_records").InsertOneAsync(
            new BsonDocument { { "_id", op.UserId }, { "PlanId", "enterprise" }, { "Status", "active" } });
        Assert.Equal("GRANT_SCOPE_MISMATCH", await Denial(op));
    }

    [Fact]
    public async Task UnstartedExpiryReleasesGrantReservation()
    {
        var grant = await Grant();
        await _ledger.AuthorizeAsync(Reservation(grant.GrantId), 0, default);
        _clock.Now = _clock.Now.AddMinutes(16);
        Assert.Equal(1, await _ledger.ExpireAsync(_clock.Now, 10, default));
        Assert.Equal(0m, (await _ledger.GetGrantAsync(grant.GrantId, default)).MonthReservedCostUsd);
    }

    [Fact]
    public async Task OnlyTheIssuingAvatarCanRevoke()
    {
        var grant = await Grant();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _ledger.RevokeGrantAsync(Guid.NewGuid().ToString("D"), grant.GrantId, default));
        Assert.Null((await _ledger.GetGrantAsync(grant.GrantId, default)).RevokedAtUtc);
    }

    [Fact]
    public async Task ReplayWithDifferentGrantIsAConflict()
    {
        var grant = await Grant();
        var op = Reservation(grant.GrantId);
        await _ledger.AuthorizeAsync(op, 0, default);
        op.GrantId = (await Grant()).GrantId;
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => _ledger.AuthorizeAsync(op, 0, default));
    }
}

public sealed class DelegatedGrantValidationTests
{
    private static readonly DateTime Now = new(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
    private static CreateDelegatedUsageGrantRequest Valid() => new() {
        ConsumingService = "WEB6", Scope = "web6:channel:x", Endpoints = new() { "POST:/v1/agent/run" },
        MonthlyBudgetUsd = 5, MaxOperationCostUsd = 1, ExpiresAtUtc = Now.AddDays(30) };

    [Fact]
    public void ValidRequestIsAccepted() => DelegatedUsageGrantValidation.ValidateCreate(Guid.NewGuid().ToString("D"), Valid(), Now);

    [Theory]
    [InlineData("WEB4")][InlineData("STRIPE")][InlineData("")]
    public void Web4AndUnknownServicesCannotBeDelegated(string service)
    {
        var request = Valid(); request.ConsumingService = service;
        Assert.Throws<ArgumentException>(() => DelegatedUsageGrantValidation.ValidateCreate(Guid.NewGuid().ToString("D"), request, Now));
    }

    [Fact]
    public void WildcardOrMissingEndpointsAreRejected()
    {
        var request = Valid(); request.Endpoints = new();
        Assert.Throws<ArgumentException>(() => DelegatedUsageGrantValidation.ValidateCreate(Guid.NewGuid().ToString("D"), request, Now));
    }

    [Fact]
    public void PerOperationCapAboveBudgetAndOverlongLifetimeAreRejected()
    {
        var capped = Valid(); capped.MaxOperationCostUsd = 6;
        Assert.Throws<ArgumentException>(() => DelegatedUsageGrantValidation.ValidateCreate(Guid.NewGuid().ToString("D"), capped, Now));
        var forever = Valid(); forever.ExpiresAtUtc = Now.AddDays(400);
        Assert.Throws<ArgumentException>(() => DelegatedUsageGrantValidation.ValidateCreate(Guid.NewGuid().ToString("D"), forever, Now));
    }
}
