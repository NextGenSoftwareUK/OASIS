using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Stripe;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")))
            Skip = "Set SUBSCRIPTION_TEST_MONGODB_URI to a MongoDB replica set to run the ledger tests.";
    }
}

public sealed class StripeMongoFactAttribute : FactAttribute
{
    public StripeMongoFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OASIS_OVERAGE_STRIPE_TEST_KEY")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OASIS_OVERAGE_FIXTURE_CUSTOMER")))
            Skip = "Needs a MongoDB replica set and an owned Stripe TEST-mode customer and subscription; live keys are never used.";
    }
}

/// <summary>Real-database tests for the usage visibility storage, app pool admission and app pay-as-you-go statements. Each uses its own throwaway database.</summary>
[Collection("PooledFlag")]
public sealed class UsageVisibilityMongoTests : IDisposable
{
    private readonly string _originalPooledFlag = Environment.GetEnvironmentVariable(AppAccountRules.PooledFlagName);

    public UsageVisibilityMongoTests()
        => Environment.SetEnvironmentVariable(AppAccountRules.PooledFlagName, "true");

    public void Dispose()
        => Environment.SetEnvironmentVariable(AppAccountRules.PooledFlagName, _originalPooledFlag);

    private sealed class Policy : ISubscriptionUsagePolicyProvider
    {
        // The app plan ("silver") is a pool of 10 requests a month; every other plan is unlimited so only the pool is under test.
        public SubscriptionUsagePolicy GetPolicy(string plan, int karma) => plan == "silver"
            ? new() { MonthlyRequestLimit = 10, DailyCallLimit = 0, DailyTokenLimit = 0, MonthlyBudgetUsd = 100m }
            : new() { MonthlyRequestLimit = -1, DailyCallLimit = 0, DailyTokenLimit = 0, MonthlyBudgetUsd = 100m };
    }
    private sealed class Clock(DateTime time) : TimeProvider { public override DateTimeOffset GetUtcNow() => new(time); }

    private static readonly string Owner = Guid.NewGuid().ToString("D");
    private static readonly string Reader = Guid.NewGuid().ToString("D");

    private static (MongoClient Client, string Scope, MongoSubscriptionUsageRepository Ledger, Clock Clock) Make()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var scope = "ovgx_" + Guid.NewGuid().ToString("N");
        var clock = new Clock(DateTime.UtcNow);
        // An initialised ledger: the opening-balance marker exists, so test data can be inserted before the first authorization.
        client.GetDatabase(scope).GetCollection<MongoDB.Bson.BsonDocument>("subscription_usage_migrations").InsertOne(new MongoDB.Bson.BsonDocument
        { { "_id", "v1-opening-balance" }, { "InitializationMode", "test" }, { "Actor", "test" }, { "CompletedAtUtc", DateTime.UtcNow } });
        return (client, scope, new MongoSubscriptionUsageRepository(client, scope, clock, new Policy(), initializeEmptyLedger: true), clock);
    }

    private static Task Subscriber(MongoClient client, string scope, string userId) =>
        client.GetDatabase(scope).GetCollection<SubscriptionRecord>("subscription_records").InsertOneAsync(new() { UserId = userId, PlanId = "enterprise", Status = "free" });

    private static async Task Drop(MongoClient client, string scope)
    {
        if (!scope.StartsWith("ovgx_", StringComparison.Ordinal) || scope.Length != 37) throw new InvalidOperationException("Refusing to remove an unexpected test database.");
        await client.DropDatabaseAsync(scope);
    }

    private static async Task<AppAccountRecord> NewApp(MongoSubscriptionUsageRepository ledger, string appId = "piper")
        => (await ledger.CreateAppAccountAsync(appId, "PIPER", "silver", "admin", "test app", pooledQuota: true)).Account;

    private static SubscriptionUsageEvent Request(string appId, string user) => new()
    {
        OperationId = Guid.NewGuid().ToString("D"), UserId = user, ConsumingService = "WEB4", Endpoint = "GET /test", MeterCategory = "api.request",
        RequestedUnits = 1, RequestFingerprint = new string('a', 64), Status = "authorized", PlanId = "enterprise",
        AppId = appId, AppPlanId = AppAccountRules.DefaultPerUserPlanId, PooledAppId = appId, PooledAppPlanId = "silver", AuthorizedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
    };

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task DailySeriesReadsUserBucketsAndSumsPoolShards()
    {
        var (client, scope, ledger, _) = Make();
        try
        {
            var buckets = client.GetDatabase(scope).GetCollection<SubscriptionUsageBucket>("subscription_usage_buckets");
            await buckets.InsertManyAsync(new[]
            {
                new SubscriptionUsageBucket { Id = Reader + ":day:2026-11-10", UserId = Reader, PeriodType = "day", Period = "2026-11-10", Requests = 7 },
                new SubscriptionUsageBucket { Id = Reader + ":day:2026-11-12", UserId = Reader, PeriodType = "day", Period = "2026-11-12", Requests = 3 },
                new SubscriptionUsageBucket { Id = Reader + ":month:2026-11", UserId = Reader, PeriodType = "month", Period = "2026-11", Requests = 10 },
                new SubscriptionUsageBucket { Id = "app:piper:day:2026-11-12", UserId = "app:piper", PeriodType = "day", Period = "2026-11-12", Requests = 5 },
                new SubscriptionUsageBucket { Id = "app:piper#1:day:2026-11-12", UserId = "app:piper#1", PeriodType = "day", Period = "2026-11-12", Requests = 6 },
                new SubscriptionUsageBucket { Id = "someone-else:day:2026-11-12", UserId = "someone-else", PeriodType = "day", Period = "2026-11-12", Requests = 99 }
            });
            var mine = UsageSeries.Fill(await ledger.GetDailySeriesAsync(new[] { Reader }, new DateTime(2026, 11, 9), new DateTime(2026, 11, 13), default), new DateTime(2026, 11, 9), new DateTime(2026, 11, 13));
            Assert.Equal(new long[] { 0, 7, 0, 3, 0 }, mine.Select(d => d.Requests).ToArray());
            var pool = await ledger.GetDailySeriesAsync(new[] { MongoSubscriptionUsageRepository.PoolShardSubject("piper", 0), MongoSubscriptionUsageRepository.PoolShardSubject("piper", 1) }, new DateTime(2026, 11, 12), new DateTime(2026, 11, 12), default);
            Assert.Equal(11, UsageSeries.Fill(pool, new DateTime(2026, 11, 12), new DateTime(2026, 11, 12)).Single().Requests);
        }
        finally { await Drop(client, scope); }
    }

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task OwnersAreStoredCheckedAndAudited()
    {
        var (client, scope, ledger, _) = Make();
        try
        {
            await NewApp(ledger);
            await ledger.SetAppOwnersAsync("piper", new[] { Owner }, "admin", "Business owner");
            Assert.True(await ledger.IsAppOwnerAsync("piper", Owner));
            Assert.False(await ledger.IsAppOwnerAsync("piper", Reader));
            Assert.Equal("piper", Assert.Single(await ledger.GetOwnedAppsAsync(Owner)).AppId);
            Assert.Empty(await ledger.GetOwnedAppsAsync(Reader));
            await Assert.ThrowsAsync<ArgumentException>(() => ledger.SetAppOwnersAsync("piper", new[] { "not-a-guid" }, "admin", "x"));
            await Assert.ThrowsAsync<ArgumentException>(() => ledger.SetAppOwnersAsync("piper", Enumerable.Range(0, 11).Select(_ => Guid.NewGuid().ToString("D")).ToList(), "admin", "x"));
            Assert.Equal(1, await client.GetDatabase(scope).GetCollection<MongoDB.Bson.BsonDocument>("subscription_billing_audit").CountDocumentsAsync(new MongoDB.Bson.BsonDocument("Kind", "app-account-owners-set")));
        }
        finally { await Drop(client, scope); }
    }

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task NotificationSettingsAreSavedAuditedAndTheNotifiedMarkerFiresOnce()
    {
        var (client, scope, ledger, _) = Make();
        try
        {
            var subject = "user:" + Reader;
            Assert.Null(await ledger.GetNotificationSettingsAsync(subject));
            Assert.True(await ledger.TryMarkNotifiedAsync(subject, "2026-11:80"));
            Assert.False(await ledger.TryMarkNotifiedAsync(subject, "2026-11:80"));
            Assert.True(await ledger.TryMarkNotifiedAsync(subject, "2026-11:100"));
            var saved = await ledger.SaveNotificationSettingsAsync(subject, new[] { 100, 50 }, emailRequested: false, actor: Reader, warningsOverride: false);
            Assert.Equal(new[] { 50, 100 }, saved.Thresholds.ToArray());
            var read = await ledger.GetNotificationSettingsAsync(subject);
            Assert.Equal(new[] { 50, 100 }, read.Thresholds.ToArray()); Assert.False(read.EmailRequested); Assert.False(read.WarningsOverride);
            Assert.False(await ledger.TryMarkNotifiedAsync(subject, "2026-11:100")); // saving settings did not reset the sent marker
            await Assert.ThrowsAsync<ArgumentException>(() => ledger.SaveNotificationSettingsAsync(subject, new[] { 0 }, true, Reader));
        }
        finally { await Drop(client, scope); }
    }

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task AppOverridesArePersisted()
    {
        var (client, scope, ledger, _) = Make();
        try
        {
            await NewApp(ledger);
            await ledger.SetAppOverridesAsync("piper", Patch("{\"ownerViewEnabled\":null,\"warningsEnabled\":true,\"accessLogEnabled\":false}"), Owner, administrator: true);
            var app = (await ledger.ListAppAccountsAsync()).Single();
            Assert.Null(app.Overrides.OwnerViewEnabled); Assert.True(app.Overrides.WarningsEnabled); Assert.False(app.Overrides.AccessLogEnabled);
        }
        finally { await Drop(client, scope); }
    }

    private static AppUsageSettingsPatch Patch(string json) => new(JsonDocument.Parse(json).RootElement);

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task OnlyOwnersOrAdministratorsMayReadOrChangeAnApp()
    {
        var (client, scope, ledger, _) = Make();
        try
        {
            await NewApp(ledger);
            await ledger.SetAppOwnersAsync("piper", new[] { Owner }, "admin", "test owner");
            Assert.Equal("piper", (await ledger.GetAppForOwnerAsync("piper", Owner, false)).AppId);
            Assert.Equal("piper", (await ledger.GetAppForOwnerAsync("piper", Reader, true)).AppId);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ledger.GetAppForOwnerAsync("piper", Reader, false));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ledger.SetAppOverridesAsync("piper", Patch("{\"warningsEnabled\":false}"), Reader, false));
        }
        finally { await Drop(client, scope); }
    }

    private static async Task Consent(MongoClient client, string scope, MongoSubscriptionUsageRepository ledger, Clock clock, decimal cap, string customer = "cus_test", string subscription = "sub_test", string app = "piper")
    {
        var payers = client.GetDatabase(scope).GetCollection<SubscriptionRecord>("subscription_records");
        await payers.InsertOneAsync(new()
        { UserId = Owner, PlanId = "silver", Status = "active", StripeCustomerId = customer, StripeSubscriptionId = subscription, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1) });
        await ledger.SetAppOwnersAsync(app, new[] { Owner }, "admin", "test owner");
        var prices = AppOveragePrices.Load();
        var payer = await payers.Find(x => x.UserId == Owner).FirstAsync();
        await ledger.SetAppPaygAsync(app, new AppPaygConsentRequest { Enabled = true, MonthlyCapUsd = cap, PricingVersion = prices.Version, PricingSha256 = prices.PricingSha256, Consent = true },
            Owner, false, AppBillingPayerSnapshot.AfterVerification(payer, clock.GetUtcNow().UtcDateTime));
    }

    /// <summary>Authorizes and settles one pooled request, so any charge beyond the pool is a settled app overage.</summary>
    private static async Task Use(MongoSubscriptionUsageRepository ledger, string app = "piper")
    {
        var request = Request(app, Reader);
        await ledger.AuthorizeAsync(request, 0, default);
        await ledger.StartAsync(new() { OperationId = request.OperationId, UserId = Reader, ConsumingService = "WEB4" }, default);
        await ledger.SettleAsync(Reader, new UsageSettlementRequest { OperationId = request.OperationId, UserId = Reader, ConsumingService = "WEB4", Outcome = "succeeded",
            Provider = "isolated-fixture", Model = "test-model", ProviderRequestId = request.OperationId, PromptTokens = 1, ActualCostUsd = 0, EstimatedCostUsd = 0, Units = 1,
            CostSource = "isolated-test-measurement", PricingCatalogueVersion = "fixture-v1",
            ProviderReceipts = new() { new() { Provider = "isolated-fixture", Model = "test-model", ProviderRequestId = request.OperationId, PromptTokens = 1, Units = 1, ActualCostUsd = 0,
                CostSource = "isolated-test-measurement", PricingCatalogueVersion = "fixture-v1" } } }, default);
    }

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task AppOverageIsRefusedWithoutConsentAndAdmittedUpToTheCapWithIt()
    {
        var (client, scope, ledger, clock) = Make();
        try
        {
            await Subscriber(client, scope, Reader);
            await NewApp(ledger);
            // The pool allows 10 requests. Without consent the 11th is a normal refusal.
            for (var i = 0; i < 10; i++) await ledger.AuthorizeAsync(Request("piper", Reader), 0, default);
            var refused = await Assert.ThrowsAsync<SubscriptionUsageLimitException>(() => ledger.AuthorizeAsync(Request("piper", Reader), 0, default));
            Assert.Equal("APP_MONTHLY_REQUEST_LIMIT_EXCEEDED", refused.Code);
            // Consent with the smallest cap, $0.50 at the silver rate of $0.0005: exactly 1,000 extra requests fit.
            await Consent(client, scope, ledger, clock, 0.50m);
            for (var i = 0; i < 1000; i++) await ledger.AuthorizeAsync(Request("piper", Reader), 0, default);
            var capped = await Assert.ThrowsAsync<SubscriptionUsageLimitException>(() => ledger.AuthorizeAsync(Request("piper", Reader), 0, default));
            Assert.Equal("APP_OVERAGE_SPENDING_CAP_EXCEEDED", capped.Code);
        }
        finally { await Drop(client, scope); }
    }

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task AFailedOwnerPaymentStopsAdmittingOverage()
    {
        var (client, scope, ledger, clock) = Make();
        try
        {
            await Subscriber(client, scope, Reader);
            await NewApp(ledger);
            await Consent(client, scope, ledger, clock, 50m);
            for (var i = 0; i < 10; i++) await ledger.AuthorizeAsync(Request("piper", Reader), 0, default);
            await ledger.AuthorizeAsync(Request("piper", Reader), 0, default); // admitted as billable overage
            await client.GetDatabase(scope).GetCollection<PaidOverageStatement>("subscription_overage_statements").InsertOneAsync(new()
            { Id = "failed1", UserId = "app:piper", Month = "2026-10", State = "open", PaymentFailed = true, AmountMinorUsd = 100, CreatedAtUtc = DateTime.UtcNow });
            await Assert.ThrowsAsync<SubscriptionUsageLimitException>(() => ledger.AuthorizeAsync(Request("piper", Reader), 0, default));
        }
        finally { await Drop(client, scope); }
    }

    private static string ThisMonth() => DateTime.UtcNow.ToString("yyyy-MM");

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task OwnerPreparedStatementBillsTheSettledOverageOnce()
    {
        var (client, scope, ledger, clock) = Make();
        try
        {
            await Subscriber(client, scope, Reader);
            await NewApp(ledger);
            await Consent(client, scope, ledger, clock, 50m);
            for (var i = 0; i < 10 + 20; i++) await Use(ledger); // 20 over the pool of 10 x $0.0005 = $0.01
            var key = Guid.NewGuid().ToString("D");
            var statement = await ledger.PrepareAppStatementForOwnerAsync("piper", ThisMonth(), key, Owner, false);
            Assert.NotNull(statement);
            Assert.Equal(1, statement.AmountMinorUsd); Assert.Equal("app:piper", statement.UserId); Assert.Equal("cus_test", statement.StripeCustomerId);
            Assert.Equal(statement.Id, (await ledger.PrepareAppStatementForOwnerAsync("piper", ThisMonth(), key, Owner, false)).Id); // same request key replays
            Assert.Null(await ledger.PrepareAppStatementForOwnerAsync("piper", ThisMonth(), Guid.NewGuid().ToString("D"), Owner, false)); // nothing left to bill
            Assert.Equal(statement.Id, (await ledger.GetAppStatementForOwnerAsync("piper", statement.Id, Owner, false)).Id);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ledger.PrepareAppStatementForOwnerAsync("piper", ThisMonth(), Guid.NewGuid().ToString("D"), Reader, false));
        }
        finally { await Drop(client, scope); }
    }

    [Trait("Category", "LiveService")]
    [MongoFact]
    public async Task NoStatementWithinTheAllowanceOrWithoutConsent()
    {
        var (client, scope, ledger, clock) = Make();
        try
        {
            await Subscriber(client, scope, Reader);
            await NewApp(ledger);
            await ledger.SetAppOwnersAsync("piper", new[] { Owner }, "admin", "test owner");
            for (var i = 0; i < 8; i++) await Use(ledger); // within the pool of 10
            Assert.Null(await ledger.PrepareAppStatementForOwnerAsync("piper", ThisMonth(), Guid.NewGuid().ToString("D"), Owner, false));
        }
        finally { await Drop(client, scope); }
    }

    [Trait("Category", "LiveService")]
    [StripeMongoFact]
    public async Task AppStatementIsCollectedAsOnePaidStripeTestInvoice()
    {
        string key = Environment.GetEnvironmentVariable("OASIS_OVERAGE_STRIPE_TEST_KEY"); Assert.StartsWith("sk_test_", key);
        string customer = Environment.GetEnvironmentVariable("OASIS_OVERAGE_FIXTURE_CUSTOMER"), subscription = Environment.GetEnvironmentVariable("OASIS_OVERAGE_FIXTURE_SUBSCRIPTION");
        var (client, scope, ledger, clock) = Make();
        try
        {
            var app = "t" + Guid.NewGuid().ToString("N")[..10];
            await NewApp(ledger, app);
            await Subscriber(client, scope, Reader);
            await Consent(client, scope, ledger, clock, 50m, customer, subscription, app);
            for (var i = 0; i < 10 + 20; i++) await Use(ledger, app);
            var statement = await ledger.PrepareAppStatementForOwnerAsync(app, ThisMonth(), Guid.NewGuid().ToString("D"), Owner, false);
            Assert.NotNull(statement);
            var gateway = new StripePaidOverageGateway(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "STRIPE_SECRET_KEY", key } }).Build());
            var billing = new PaidOverageBillingService(ledger, gateway, NullLogger<PaidOverageBillingService>.Instance, clock);
            var paid = await billing.CollectAsync(statement, default);
            Assert.Equal("paid", paid.State); Assert.StartsWith("in_", paid.StripeInvoiceId);
            Assert.Equal(paid.StripeInvoiceId, (await billing.CollectAsync(statement, default)).StripeInvoiceId); // collecting again does not create a second invoice
            var stripe = new StripeClient(key);
            var invoice = await new InvoiceService(stripe).GetAsync(paid.StripeInvoiceId);
            Assert.False(invoice.Livemode); Assert.Equal("usd", invoice.Currency); Assert.Equal(statement.AmountMinorUsd, invoice.Subtotal); Assert.Equal(statement.AmountMinorUsd, invoice.AmountPaid);
            Assert.Equal("app:" + app, invoice.Metadata["oasis_avatar_id"]);
            var matching = (await new InvoiceService(stripe).ListAsync(new() { Customer = customer, Limit = 100 })).Data.Where(x => x.Metadata?.GetValueOrDefault("oasis_overage_statement_id") == statement.Id);
            Assert.Single(matching);
        }
        finally { await Drop(client, scope); }
    }
}
