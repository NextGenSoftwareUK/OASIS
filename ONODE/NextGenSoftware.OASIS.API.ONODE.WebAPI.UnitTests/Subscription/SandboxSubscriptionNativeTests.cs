using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed class SandboxSubscriptionNativeFactAttribute : FactAttribute
{
    public SandboxSubscriptionNativeFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION")))
            Skip = "Requires the owned loopback Mongo replica set; native coverage is reported separately.";
    }
}

public class SandboxSubscriptionNativeTests : IAsyncLifetime
{
    private const string Database = "cosmic_cql_sandbox_subscription_tests";
    private const string Owner = "cosmic-cql-sandbox-subscriptions-v1";
    private sealed class Clock : TimeProvider {
        public DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
        public override DateTimeOffset GetUtcNow() => new(Now);
    }
    private readonly Clock clock = new();
    private MongoClient client = null!;
    private IMongoDatabase database = null!;
    private MongoSubscriptionUsageRepository ledger = null!;
    private bool owned;

    public async Task InitializeAsync()
    {
        var uri = Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION")
            ?? throw new InvalidOperationException("Owned replica set is required.");
        var parsed = MongoUrl.Create(uri);
        Assert.Equal("127.0.0.1", Assert.Single(parsed.Servers).Host);
        Assert.Equal(27039, Assert.Single(parsed.Servers).Port);
        Assert.Equal("cosmicLinuxDemoRs", parsed.ReplicaSetName);
        client = new MongoClient(uri);
        database = client.GetDatabase(Database);
        using var collections = await database.ListCollectionNamesAsync();
        Assert.Empty(await collections.ToListAsync()); // Never adopt or reset an existing namespace.
        await database.GetCollection<BsonDocument>("cosmic_test_owner").InsertOneAsync(new BsonDocument("_id", Owner));
        owned = true;
        ledger = new MongoSubscriptionUsageRepository(client, Database, clock, initializeEmptyLedger: true);
    }

    public async Task DisposeAsync()
    {
        if (!owned) return;
        var markers = await database.GetCollection<BsonDocument>("cosmic_test_owner").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(Owner, Assert.Single(markers)["_id"].AsString);
        await client.DropDatabaseAsync(Database); // Exact owned disposable fixture only; accounting source is never used.
    }

    [SandboxSubscriptionNativeFact]
    public async Task IssuanceIsIdempotentAndCannotExtendOrReplaceAnExistingAccount()
    {
        var user = Guid.NewGuid().ToString("D"); var created = clock.Now; var expires = created.AddHours(24);
        await Task.WhenAll(ledger.CreateSandboxSubscriptionAsync(user, created, expires, default),
            ledger.CreateSandboxSubscriptionAsync(user, created, expires, default));
        await ledger.CreateSandboxSubscriptionAsync(user, created, expires, default);
        var record = await ledger.GetSubscriptionAsync(user);
        Assert.Equal("free", record.PlanId); Assert.False(record.PayAsYouGoEnabled);
        Assert.Null(record.StripeCustomerId); Assert.Equal(expires, record.CurrentPeriodEnd);
        Assert.Equal(expires, record.SandboxExpiresAtUtc);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => ledger.CreateSandboxSubscriptionAsync(user, created, expires.AddHours(1), default));
        Assert.Equal(1L, await database.GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(expires, (await ledger.GetSubscriptionAsync(user)).SandboxExpiresAtUtc);
    }

    [SandboxSubscriptionNativeFact]
    public async Task ExpiredUnencumberedFreeRecordIsRetiredButItsAuditAndOtherAccountsRemain()
    {
        var expired = Guid.NewGuid().ToString("D"); var active = Guid.NewGuid().ToString("D"); var ordinary = Guid.NewGuid().ToString("D");
        await ledger.CreateSandboxSubscriptionAsync(expired, clock.Now, clock.Now.AddHours(1), default);
        await ledger.CreateSandboxSubscriptionAsync(active, clock.Now, clock.Now.AddHours(24), default);
        await ledger.SaveSubscriptionAsync(new SubscriptionRecord { UserId = ordinary, PlanId = "free", Status = "active" });
        clock.Now = clock.Now.AddHours(2);
        var denied = await Assert.ThrowsAsync<SubscriptionUsageLimitException>(() => ledger.AuthorizeAsync(new SubscriptionUsageEvent {
            OperationId = Guid.NewGuid().ToString("D"), UserId = expired, ConsumingService = "WEB4", Endpoint = "POST /api/data/query-cql",
            MeterCategory = "api.request", Status = "authorized", RequestedUnits = 1, ReservedCostUsd = 0,
            RequestFingerprint = new string('b', 64), AuthorizedAtUtc = clock.Now, ExpiresAtUtc = clock.Now.AddMinutes(15)
        }, 0, default));
        Assert.Equal("INACTIVE_SUBSCRIPTION", denied.Code); // Expiry applies even before physical cleanup.
        Assert.Equal(new SandboxSubscriptionSweep(1, 0), await ledger.RetireSandboxSubscriptionsAsync(50, default));
        Assert.Null(await ledger.GetSubscriptionAsync(expired));
        Assert.NotNull(await ledger.GetSubscriptionAsync(active)); Assert.NotNull(await ledger.GetSubscriptionAsync(ordinary));
        var audit = await database.GetCollection<BsonDocument>("subscription_billing_audit").Find(new BsonDocument("UserId", expired)).ToListAsync();
        Assert.Equal(new[] { "sandbox-free-plan-activation", "sandbox-free-plan-retirement" }, audit.Select(entry => entry["Kind"].AsString).Order().ToArray());
        Assert.Equal(new SandboxSubscriptionSweep(0, 0), await ledger.RetireSandboxSubscriptionsAsync(50, default));
    }

    [SandboxSubscriptionNativeFact]
    public async Task UnstartedReservationDefersRetirementUntilTheExistingExpiryProtocolResolvesIt()
        => await PendingReservation(false);

    [SandboxSubscriptionNativeFact]
    public async Task ExecutingAndRecoveryRequiredReservationsKeepTheSubscriptionForReconciliation()
        => await PendingReservation(true);

    private async Task PendingReservation(bool executing)
    {
        var user = Guid.NewGuid().ToString("D"); var operation = Guid.NewGuid().ToString("D");
        await ledger.CreateSandboxSubscriptionAsync(user, clock.Now, clock.Now.AddHours(1), default);
        await ledger.AuthorizeAsync(new SubscriptionUsageEvent {
            OperationId = operation, UserId = user, ConsumingService = "WEB4", Endpoint = "POST /api/data/query-cql",
            MeterCategory = "api.request", Status = "authorized", RequestedUnits = 1, ReservedCostUsd = 0,
            RequestFingerprint = new string('a', 64), AuthorizedAtUtc = clock.Now, ExpiresAtUtc = clock.Now.AddMinutes(15)
        }, 0, default);
        if (executing) await ledger.StartAsync(new UsageStartRequest { UserId = user, OperationId = operation, ConsumingService = "WEB4" }, default);
        clock.Now = clock.Now.AddHours(2);
        Assert.Equal(new SandboxSubscriptionSweep(0, 1), await ledger.RetireSandboxSubscriptionsAsync(50, default));
        Assert.NotNull(await ledger.GetSubscriptionAsync(user));
        await ledger.ExpireAsync(clock.Now, 50, default);
        var report = await ledger.RetireSandboxSubscriptionsAsync(50, default);
        Assert.Equal(executing ? new SandboxSubscriptionSweep(0, 1) : new SandboxSubscriptionSweep(1, 0), report);
        var persisted = await database.GetCollection<BsonDocument>("subscription_usage_events").Find(new BsonDocument("OperationId", operation)).SingleAsync();
        Assert.Equal(executing ? "recovery-required" : "expired", persisted["Status"].AsString);
    }

    [SandboxSubscriptionNativeFact]
    public async Task ChangedPaidStateIsNeverDeletedByTheDemoMarker()
    {
        var user = Guid.NewGuid().ToString("D");
        await ledger.CreateSandboxSubscriptionAsync(user, clock.Now, clock.Now.AddHours(1), default);
        // Adversarial persisted state tests ownership guards, not a Stripe payment flow.
        await database.GetCollection<BsonDocument>("subscription_records").UpdateOneAsync(new BsonDocument("_id", user),
            new BsonDocument("$set", new BsonDocument { { "PlanId", "gold" }, { "StripeCustomerId", "cus_test" }, { "StripeSubscriptionId", "sub_test" } }));
        clock.Now = clock.Now.AddHours(2);
        Assert.Equal(new SandboxSubscriptionSweep(0, 0), await ledger.RetireSandboxSubscriptionsAsync(50, default));
        Assert.Equal("sub_test", (await ledger.GetSubscriptionAsync(user)).StripeSubscriptionId);
    }

    [SandboxSubscriptionNativeFact]
    public async Task APageOfOlderUnresolvedRecordsCannotStarveLaterRetirement()
    {
        var pending = Guid.NewGuid().ToString("D"); var eligible = Guid.NewGuid().ToString("D");
        await ledger.CreateSandboxSubscriptionAsync(pending, clock.Now, clock.Now.AddMinutes(30), default);
        await ledger.CreateSandboxSubscriptionAsync(eligible, clock.Now, clock.Now.AddHours(1), default);
        await database.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument {
            { "OperationId", Guid.NewGuid().ToString("D") }, { "UserId", pending }, { "Status", "recovery-required" },
            { "RequestFingerprint", new string('a', 64) }
        });
        clock.Now = clock.Now.AddHours(2);
        Assert.Equal(new SandboxSubscriptionSweep(1, 1), await ledger.RetireSandboxSubscriptionsAsync(1, default));
        Assert.NotNull(await ledger.GetSubscriptionAsync(pending)); Assert.Null(await ledger.GetSubscriptionAsync(eligible));
    }

    [SandboxSubscriptionNativeFact]
    public async Task IndependentPreparedOperationsSettleWithDistinctPhysicalReceipts()
    {
        clock.Now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime;
        var user = Guid.NewGuid().ToString("D");
        await ledger.CreateSandboxSubscriptionAsync(user, clock.Now, clock.Now.AddHours(24), default);
        var uri = Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION")!;
        var outbox = new MongoUsageOutboxStore(uri, Database);
        await outbox.InitializeAsync(default);
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user)
        }, "controlled-native-test"));
        var subscriptions = new SubscriptionService(Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionService>.Instance, ledger, ledger);
        var authority = new LocalUsageReservationAuthority(subscriptions, ledger,
            new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = http }, (_, _) => Task.FromResult(0));
        var executor = new Web4UsageExecution(authority, outbox, new Web4UsageLedgerOptions {
            Service = "WEB4", MongoConnectionString = uri, Database = Database
        });
        var policy = new UsageEndpointPolicy {
            MeterCategory = "api.request", ReservedUnits = 1, RequiresProviderMeasurement = true,
            MaximumProviderAttempts = 1, MaximumNativeFeeCostUsd = 0, EstimatedCostUsd = 0,
            PricingCatalogueVersion = "sandbox-test-free-v1"
        };
        var type = NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS;
        var provider = new Moq.Mock<NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider>();
        provider.SetupGet(value => value.ProviderType).Returns(new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(type));
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(), new[] { type },
            new[] { new StorageQueryTariff(type, 0, policy.PricingCatalogueVersion) });
        var method = typeof(PreparedStorageExecution).GetMethod("ExecuteQueryAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.MakeGenericMethod(typeof(int));
        var receipts = new List<string>();
        for (var index = 0; index < 2; index++) {
            var handle = await executor.BeginAsync("controlled-bearer", user, "independent-key-" + index,
                new string((char)('a' + index), 64), "POST /api/data/query-cql", policy);
            var prepared = await factory.CreateAsync(http, handle, default);
            Func<Task<NextGenSoftware.OASIS.Common.OASISResult<int>>> read = () => Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<int> { Result = 1 });
            var result = await (Task<NextGenSoftware.OASIS.Common.OASISResult<int>>)method.Invoke(prepared, new object[] { provider.Object, read })!;
            Assert.False(result.IsError);
            await handle.CompleteAsync("succeeded");
            var queued = await outbox.GetAsync(handle.OperationId, default);
            var receipt = Assert.Single(queued.Receipts);
            Assert.InRange(receipt.ClientRequestId.Length, 1, 128);
            receipts.Add(receipt.ProviderRequestId);
            Assert.True((await authority.SettleUsageAsync(queued.Settlement)).Settled);
        }
        Assert.Equal(2, receipts.Distinct().Count());
        Assert.Equal(2L, await database.GetCollection<BsonDocument>("subscription_usage_receipts").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [SandboxSubscriptionNativeFact]
    public async Task UnreviewedOpeningStateCannotBeBypassedByDemoIssuance()
    {
        var guarded = new MongoSubscriptionUsageRepository(client, Database, clock, initializeEmptyLedger: false);
        var user = Guid.NewGuid().ToString("D");
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => guarded.CreateSandboxSubscriptionAsync(user, clock.Now, clock.Now.AddHours(1), default));
        Assert.Null(await guarded.GetSubscriptionAsync(user));
        Assert.Equal(0L, await database.GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }
}
