using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task AppRequestChargesUseImmutableConsentExactCapsAndSeparatePayers()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_app_charge_" + Guid.NewGuid().ToString("N");
        Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
            "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the actual WEB4 API before this case.");
        Assert.True(File.GetLastWriteTimeUtc(artifact) >= File.GetLastWriteTimeUtc(Path.Combine(root.FullName,
            "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/Subscription/MongoSubscriptionUsageRepository.Pool.cs")));
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        }
        var oldPool = Environment.GetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS");
        var oldShards = Environment.GetEnvironmentVariable("OASIS_POOL_SHARDS");
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", "true");
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
            var repositoryType = api.GetType(ns + "MongoSubscriptionUsageRepository", true)!;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["SUBSCRIPTION_MONGODB_CONNECTION_STRING"] = MongoUri,
                ["SUBSCRIPTION_MONGODB_DATABASE"] = databaseName,
                ["SUBSCRIPTION_LEDGER_INITIAL_STATE"] = "empty-new-installation"
            }).Build();
            var repository = Activator.CreateInstance(repositoryType, [config])!;
            async Task<object?> Call(string method, params object?[] args)
            {
                var task = (Task)repositoryType.GetMethod(method)!.Invoke(repository, args)!;
                await task;
                return task.GetType().GetProperty("Result")?.GetValue(task);
            }
            var recordType = api.GetType(ns + "SubscriptionRecord", true)!;
            var eventType = api.GetType(ns + "SubscriptionUsageEvent", true)!;
            void Set(object value, string property, object? content) => value.GetType().GetProperty(property)!.SetValue(value, content);
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", "16");
            Assert.True((bool)api.GetType(ns + "AppBillingConsent", true)!.GetProperty("NodeEnabled")!.GetValue(null)!);
            var db = mongo.GetDatabase(databaseName);
            var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
            var records = db.GetCollection<BsonDocument>("subscription_records");
            var buckets = db.GetCollection<BsonDocument>("subscription_usage_buckets");
            var events = db.GetCollection<BsonDocument>("subscription_usage_events");
            var audits = db.GetCollection<BsonDocument>("subscription_usage_audit");
            var billing = db.GetCollection<BsonDocument>("subscription_billing_audit");
            var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime;
            var month = now.ToString("yyyy-MM"); var day = now.ToString("yyyy-MM-dd");
            var catalogueType = api.GetType(ns + "AppOveragePrices", true)!;
            var catalogue = catalogueType.GetMethod("Load")!.Invoke(null, null)!;
            var rate = (decimal)catalogueType.GetMethod("Rate")!.Invoke(catalogue, ["bronze"])!;
            var version = (string)catalogueType.GetProperty("Version")!.GetValue(catalogue)!;
            var hash = (string)catalogueType.GetProperty("PricingSha256")!.GetValue(catalogue)!;
            var users = Enumerable.Range(0, 9).Select(_ => Guid.NewGuid().ToString("D")).ToArray();
            foreach (var user in users)
            {
                var record = Activator.CreateInstance(recordType)!;
                Set(record, "UserId", user); Set(record, "PlanId", "free"); Set(record, "Status", "active");
                Set(record, "CurrentPeriodEnd", now.AddYears(1));
                await Call("SaveSubscriptionAsync", record, CancellationToken.None);
            }
            var payer = users[8];
            // Explicit native FINANCIAL INPUT FIXTURES. No gateway substitution, real Stripe
            // identity, readiness/activation/collection or external provider fee is claimed.
            await records.UpdateOneAsync(new BsonDocument("_id", payer), new BsonDocument("$set", new BsonDocument {
                { "PlanId", "silver" }, { "StripeCustomerId", "owned-private-customer-fixture" },
                { "StripeSubscriptionId", "owned-private-subscription-fixture" }, { "AuthorizationVersion", 1L } }));
            async Task<string> App(string name, decimal settledBoundary = 0)
            {
                await Call("CreateAppAccountAsync", name, "Owned financial controls", "bronze", "native-fixture",
                    "Verify exact app ledger semantics", true, "silver", CancellationToken.None);
                await Call("SetAppOwnersAsync", name, new[] { payer }, "native-fixture", "Owned consent input fixture", CancellationToken.None);
                var consentId = Guid.NewGuid().ToString("D");
                var settingsType = api.GetType(ns + "AppPaygSettings", true)!;
                var settings = Activator.CreateInstance(settingsType)!;
                foreach (var item in new Dictionary<string, object> {
                    ["Enabled"] = true, ["MonthlyCapUsd"] = 0.5m, ["PricingVersion"] = version, ["PricingSha256"] = hash,
                    ["Currency"] = "USD", ["RatePerRequestUsd"] = rate, ["ConsentId"] = consentId, ["ConsentSchemaVersion"] = 1,
                    ["ConsentAtUtc"] = now, ["ConsentBy"] = payer, ["BillingAvatarId"] = payer,
                    ["VerifiedStripeCustomerId"] = "owned-private-customer-fixture", ["VerifiedStripeSubscriptionId"] = "owned-private-subscription-fixture",
                    ["BillingAuthorizationVersion"] = 1L, ["PaymentVerifiedAtUtc"] = now
                }) Set(settings, item.Key, item.Value);
                await apps.UpdateOneAsync(new BsonDocument("_id", name), new BsonDocument("$set", new BsonDocument("Payg", settings.ToBsonDocument(settingsType))));
                await billing.InsertOneAsync(new BsonDocument {
                    { "_id", "app-payg-consent:" + consentId }, { "Kind", "app-payg-settings-changed" }, { "UserId", "app:" + name },
                    { "OccurredAtUtc", now }, { "PayloadJson", JsonSerializer.Serialize(new { AppId = name, Actor = payer, Settings = settings }) }
                });
                foreach (var type in new[] { "month", "day" })
                    await buckets.InsertOneAsync(new BsonDocument {
                        { "_id", "app:" + name + ":" + type + ":" + (type == "month" ? month : day) }, { "UserId", "app:" + name },
                        { "PeriodType", type }, { "Period", type == "month" ? month : day }, { "Requests", type == "month" ? 300000L : 0L },
                        { "SettledAppRequestOverageUsd", new BsonDecimal128(settledBoundary) }, { "UpdatedAtUtc", now }
                    });
                return consentId;
            }
            object Event(string app, string user, string? id = null)
            {
                var value = Activator.CreateInstance(eventType)!;
                foreach (var item in new Dictionary<string, object> {
                    ["OperationId"] = id ?? Guid.NewGuid().ToString("D"), ["UserId"] = user, ["AppId"] = app, ["AppPlanId"] = "silver",
                    ["PooledAppId"] = app, ["PooledAppPlanId"] = "bronze", ["ConsumingService"] = "WEB4",
                    ["Endpoint"] = "/owned-app-charge", ["MeterCategory"] = "api.request", ["Status"] = "authorized",
                    ["RequestFingerprint"] = new string('c', 64), ["AuthorizedAtUtc"] = now, ["ExpiresAtUtc"] = now.AddMinutes(15),
                    ["PoolShardCountAtAuthorization"] = 999, ["ReservedAppRequestOverageUsd"] = 999m,
                    ["SettledAppRequestOverageUsd"] = 999m, ["ReservedRequestOverageUsd"] = 999m,
                    ["ReservedOverageUsd"] = 999m, ["PaidOveragePricingVersion"] = "caller-forged-financial-state"
                }) Set(value, item.Key, item.Value);
                return value;
            }
            async Task NoMutation(Func<Task> action, string reason)
            {
                var names = (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                async Task<string> Snapshot(string name) => new BsonArray(await db.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson();
                var before = new List<string>(); foreach (var name in names) before.Add(await Snapshot(name));
                var error = await Assert.ThrowsAnyAsync<Exception>(action);
                var code = error.GetType().GetProperty("Code")?.GetValue(error)?.ToString() ?? error.Message;
                Assert.Contains(reason, code!);
                Assert.Equal(names, (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                for (var i = 0; i < names.Length; i++) Assert.Equal(before[i], await Snapshot(names[i]));
            }
            var capApp = "owned-charge-cap";
            await App(capApp, 0.5m - rate);
            var requests = users.Take(8).Select(user => Event(capApp, user)).ToArray();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = requests.Select(async request => {
                await gate.Task;
                try { await Call("AuthorizeAsync", request, 0, CancellationToken.None); return true; }
                catch (Exception ex) when (ex.GetType().FullName == ns + "SubscriptionUsageLimitException") {
                    Assert.Equal("APP_OVERAGE_SPENDING_CAP_EXCEEDED", ex.GetType().GetProperty("Code")!.GetValue(ex)); return false;
                }
            }).ToArray();
            gate.SetResult(true); var accepted = await Task.WhenAll(tasks);
            Assert.Equal(1, accepted.Count(x => x));
            var winner = (await events.Find(new BsonDocument("PooledAppId", capApp)).ToListAsync()).Single();
            Assert.Equal(rate, winner["ReservedAppRequestOverageUsd"].ToDecimal());
            Assert.Equal(0m, winner["ReservedOverageUsd"].ToDecimal());
            Assert.True(winner["PaidOveragePricingVersion"].IsBsonNull);
            Assert.Equal(0m, winner["SettledAppRequestOverageUsd"].ToDecimal());
            Assert.Equal(16, winner["PoolShardCountAtAuthorization"].AsInt32);
            var capRows = await buckets.Find(new BsonDocument { { "UserId", new BsonRegularExpression("^app:" + capApp + "(#|$)") }, { "PeriodType", "month" } }).ToListAsync();
            Assert.Equal(0.5m, capRows.Sum(x => x.GetValue("ReservedAppRequestOverageUsd", 0).ToDecimal() + x.GetValue("SettledAppRequestOverageUsd", 0).ToDecimal()));
            Assert.Equal(1, (int)(await Call("ExpireAsync", now.AddHours(1), 16, CancellationToken.None))!);
            Console.WriteLine("App charge native verified: eight actual concurrent reservations/one remaining monetary slot on16 shards, admitted1/refused7, total exposure exactly0.50; caller fee/proof state ignored, expiry releases only its original fee. Seeded monetary boundary is not payment evidence.");
            const string flowApp = "owned-charge-flow";
            var consent = await App(flowApp);
            var values = Enumerable.Range(0, 4).Select(_ => Event(flowApp, users[0])).ToArray();
            foreach (var value in values) await Call("AuthorizeAsync", value, 0, CancellationToken.None);
            var flow = await events.Find(new BsonDocument("PooledAppId", flowApp)).Sort(new BsonDocument("OperationId", 1)).ToListAsync();
            Assert.Equal(4, flow.Count);
            foreach (var row in flow) {
                Assert.Equal(consent, row["AppRequestChargeTerms"]["ConsentId"].AsString);
                Assert.Equal(rate, row["AppRequestChargeTerms"]["RatePerRequestUsd"].ToDecimal());
                Assert.Equal(0.5m, row["AppRequestChargeTerms"]["MonthlyCapUsd"].ToDecimal());
                Assert.DoesNotContain("owned-private", row.ToJson());
            }
            var ids = values.Select(x => (string)eventType.GetProperty("OperationId")!.GetValue(x)!).ToArray();
            UsageStartRequest Start(string id, string user) => new() { OperationId = id, UserId = user, ConsumingService = "WEB4" };
            async Task<UsageSettlementRequest> Measurement(string id, string user, bool failed = false)
            {
                var native = db.GetCollection<BsonDocument>("owned_native_charge_measurements"); var doc = ObjectId.GenerateNewId();
                await native.InsertOneAsync(new BsonDocument { { "_id", doc }, { "OperationId", id } });
                if (failed) { var ex = await Assert.ThrowsAsync<MongoWriteException>(() => native.InsertOneAsync(new BsonDocument("_id", doc))); Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError.Category); }
                var receipt = new UsageProviderReceipt { Provider = "MongoDBOASIS", ProviderRequestId = "owned-native:" + doc,
                    ClientRequestId = id, Units = failed ? 0 : 1, ActualCostUsd = 0m,
                    CostSource = "owned-local-no-provider-fee", PricingCatalogueVersion = "owned-local-zero-cost" };
                return new UsageSettlementRequest { OperationId = id, UserId = user, ConsumingService = "WEB4", Outcome = failed ? "failed" : "succeeded",
                    Provider = receipt.Provider, ProviderRequestId = receipt.ProviderRequestId, Units = receipt.Units, ActualCostUsd = 0m,
                    CostSource = receipt.CostSource, PricingCatalogueVersion = receipt.PricingCatalogueVersion, ProviderReceipts = new() { receipt } };
            }
            await Call("UpdateAppAccountAsync", flowApp, "silver", "disabled", "native-fixture", "Existing liability retains old terms", null, null, CancellationToken.None);
            await apps.UpdateOneAsync(new BsonDocument("_id", flowApp), new BsonDocument("$set", new BsonDocument {
                { "Payg.Enabled", false }, { "Payg.MonthlyCapUsd", new BsonDecimal128(1m) }, { "Payg.RatePerRequestUsd", new BsonDecimal128(rate / 2) },
                { "OwnerAvatarIds", new BsonArray { users[1] } }, { "Payg.ConsentId", Guid.NewGuid().ToString("D") } }));
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", "1");
            foreach (var id in ids.Take(3)) await Call("StartAsync", Start(id, users[0]), CancellationToken.None);
            var success = await Measurement(ids[0], users[0]); var failed = await Measurement(ids[1], users[0], true); var late = await Measurement(ids[2], users[0]);
            await Call("SettleAsync", users[0], success, CancellationToken.None);
            await Call("SettleAsync", users[0], failed, CancellationToken.None);
            Assert.Equal(2, (int)(await Call("ExpireAsync", now.AddHours(1), 16, CancellationToken.None))!);
            var pool = await Call("GetAppPoolUsageAsync", flowApp, CancellationToken.None);
            var aggregate = pool!.GetType().GetProperty("Usage")!.GetValue(pool)!;
            Assert.Equal(rate, aggregate.GetType().GetProperty("ReservedAppRequestOverageUsd")!.GetValue(aggregate));
            Assert.Equal(rate, aggregate.GetType().GetProperty("SettledAppRequestOverageUsd")!.GetValue(aggregate));
            await Call("SettleAsync", users[0], late, CancellationToken.None);
            var correctionType = api.GetType(ns + "UsageCorrectionRequest", true)!;
            object Correction(decimal delta, string? id = null) {
                var value = Activator.CreateInstance(correctionType)!;
                Set(value, "CorrectionId", id ?? Guid.NewGuid().ToString("D")); Set(value, "OperationId", ids[0]);
                Set(value, "CorrectionKind", "billing-adjustment"); Set(value, "Reason", "Owned request charge credit");
                Set(value, "EvidenceReference", "owned-native-charge-control"); Set(value, "AppRequestChargeDeltaUsd", delta); return value;
            }
            var credit = Correction(-rate); await Call("CorrectAsync", "native-fixture", credit, CancellationToken.None);
            await Call("CorrectAsync", "native-fixture", credit, CancellationToken.None);
            await NoMutation(async () => await Call("CorrectAsync", "native-fixture", Correction(-rate), CancellationToken.None), "APP_CHARGE_CORRECTION_INVALID");
            await Call("CorrectAsync", "native-fixture", Correction(rate), CancellationToken.None);
            var check = await Call("CheckAppChargeProjectionAsync", flowApp, month, CancellationToken.None);
            Assert.True((bool)check!.GetType().GetProperty("Balanced")!.GetValue(check)!);
            Assert.Equal(rate * 2, check.GetType().GetProperty("ActualSettledUsd")!.GetValue(check));
            Assert.Equal(0m, check.GetType().GetProperty("ActualReservedUsd")!.GetValue(check));
            var beforeReplay = await audits.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            await Call("SettleAsync", users[0], late, CancellationToken.None);
            Assert.Equal(beforeReplay, await audits.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var userBuckets = await buckets.Find(new BsonDocument("UserId", users[0])).ToListAsync();
            Assert.All(userBuckets, row => { Assert.Equal(0m, row.GetValue("ReservedAppRequestOverageUsd", 0).ToDecimal()); Assert.Equal(0m, row.GetValue("SettledAppRequestOverageUsd", 0).ToDecimal()); });
            Console.WriteLine("App charge native verified: original consent/rate/cap retained after current app owner/plan/cap/rate/consent changes and disabled preference; success bills once, failure/unstarted expiry release, unknown execution retains then settles; credits/reversal/replays conserve exact separate app money; native monetary projection balanced, private payer proof absent from events.");
            // An app-paid request cannot also consume the avatar's request-fee budget.
            // Existing personal exposure leaves less than one request fee available, so an
            // early personal-cap check would incorrectly refuse this company-paid operation.
            const string doubleApp = "owned-charge-single-payer"; await App(doubleApp);
            var personalPricingType = api.GetType(ns + "PaidOveragePricing", true)!;
            var personalPricing = personalPricingType.GetMethod("Load")!.Invoke(null, null)!;
            var personalUser = users[7];
            await records.UpdateOneAsync(new BsonDocument("_id", personalUser), new BsonDocument("$set", new BsonDocument {
                { "PlanId", "bronze" }, { "PayAsYouGoEnabled", true }, { "PaidOverageTermsVersion", (string)personalPricingType.GetProperty("Version")!.GetValue(personalPricing)! },
                { "PaidOverageTermsSha256", (string)personalPricingType.GetProperty("PricingSha256")!.GetValue(personalPricing)! },
                { "PaidOverageTermsAcceptedAtUtc", now }, { "MonthlyOverageLimitUsd", new BsonDecimal128(0.5m) },
                { "StripeCustomerId", "owned-personal-customer-fixture" }, { "StripeSubscriptionId", "owned-personal-subscription-fixture" } }));
            var personalBucketId = personalUser + ":month:" + month;
            await buckets.ReplaceOneAsync(new BsonDocument("_id", personalBucketId), new BsonDocument {
                { "_id", personalBucketId }, { "UserId", personalUser }, { "PeriodType", "month" }, { "Period", month }, { "Requests", 3000000L },
                { "ReservedOverageUsd", new BsonDecimal128(0.4995m) }, { "UpdatedAtUtc", now }
            }, new ReplaceOptions { IsUpsert = true });
            var doubleRequest = Event(doubleApp, personalUser);
            var doubleResult = await Call("AuthorizeAsync", doubleRequest, 0, CancellationToken.None);
            var doubleEvent = doubleResult!.GetType().GetField("Item1")!.GetValue(doubleResult)!;
            Assert.Equal(0m, eventType.GetProperty("ReservedRequestOverageUsd")!.GetValue(doubleEvent));
            Assert.Equal(0m, eventType.GetProperty("ReservedOverageUsd")!.GetValue(doubleEvent));
            Assert.Equal(rate, eventType.GetProperty("ReservedAppRequestOverageUsd")!.GetValue(doubleEvent));
            Assert.Equal(999m, eventType.GetProperty("ReservedAppRequestOverageUsd")!.GetValue(doubleRequest));
            var doubleId = (string)eventType.GetProperty("OperationId")!.GetValue(doubleEvent)!;
            await Call("StartAsync", Start(doubleId, personalUser), CancellationToken.None);
            await Call("SettleAsync", personalUser, await Measurement(doubleId, personalUser), CancellationToken.None);
            Assert.Equal(0.4995m, (await buckets.Find(new BsonDocument("_id", personalBucketId)).SingleAsync())["ReservedOverageUsd"].ToDecimal());
            var doubleCheck = await Call("CheckAppChargeProjectionAsync", doubleApp, month, CancellationToken.None);
            Assert.True((bool)doubleCheck!.GetType().GetProperty("Balanced")!.GetValue(doubleCheck)!);
            Assert.Equal(rate, doubleCheck.GetType().GetProperty("ActualSettledUsd")!.GetValue(doubleCheck));
            Console.WriteLine("App charge native verified: simultaneous app/personal consent charges the extra request only to the app; personal request fee and new exposure0, prior0.4995 personal exposure unchanged; original caller envelope remains unmodified and app monetary projection balances.");
            // Invalid current billing evidence must never create new liability.
            const string guards = "owned-charge-guards"; var guardConsent = await App(guards);
            var savedEvidence = await billing.Find(new BsonDocument("_id", "app-payg-consent:" + guardConsent)).SingleAsync();
            await billing.DeleteOneAsync(new BsonDocument("_id", savedEvidence["_id"]));
            await NoMutation(async () => await Call("AuthorizeAsync", Event(guards, users[1]), 0, CancellationToken.None), "APP_BILLING_CONSENT_REQUIRED");
            await billing.InsertOneAsync(savedEvidence);
            await apps.UpdateOneAsync(new BsonDocument("_id", guards), new BsonDocument("$set", new BsonDocument("Payg.MonthlyCapUsd", new BsonDecimal128(1m))));
            await NoMutation(async () => await Call("AuthorizeAsync", Event(guards, users[1]), 0, CancellationToken.None), "APP_BILLING_CONSENT_CONFLICT");
            await apps.UpdateOneAsync(new BsonDocument("_id", guards), new BsonDocument("$set", new BsonDocument("Payg.MonthlyCapUsd", new BsonDecimal128(0.5m))));
            await records.UpdateOneAsync(new BsonDocument("_id", payer), new BsonDocument("$set", new BsonDocument("StripeCustomerId", "owned-changed-payer")));
            await NoMutation(async () => await Call("AuthorizeAsync", Event(guards, users[1]), 0, CancellationToken.None), "APP_BILLING_PAYER_CHANGED");
            await records.UpdateOneAsync(new BsonDocument("_id", payer), new BsonDocument("$set", new BsonDocument("StripeCustomerId", "owned-private-customer-fixture")));
            var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
            const string failedStatementId = "owned-failed-app-statement";
            await statements.InsertOneAsync(new BsonDocument { { "_id", failedStatementId }, { "UserId", "app:" + guards }, { "PaymentFailed", true }, { "State", "open" } });
            await NoMutation(async () => await Call("AuthorizeAsync", Event(guards, users[1]), 0, CancellationToken.None), "APP_OVERAGE_PAYMENT_BLOCKED");
            await statements.DeleteOneAsync(new BsonDocument("_id", failedStatementId));
            // Canonical invoice INPUT FIXTURES test the ledger mapping only, without a Stripe call.
            const string mappedId = "owned-app-invoice-mapping";
            await statements.InsertOneAsync(new BsonDocument { { "_id", mappedId }, { "UserId", "app:" + guards },
                { "AppId", guards }, { "PayerAvatarId", payer }, { "Scope", databaseName }, { "Month", month },
                { "AmountMinorUsd", 1L }, { "State", "pending" }, { "CreatedAtUtc", now } });
            var invoiceType = api.GetType(ns + "PaidOverageInvoiceResult", true)!;
            object Invoice(string state, long paid) => Activator.CreateInstance(invoiceType, ["in_owned_app_charge_fixture", state, null, 1L, paid, true])!;
            await Call("RecordInvoiceAsync", mappedId, null, Invoice("open", 0), "owned-app-invoice-failure", "owned-canonical-input-1", CancellationToken.None);
            Assert.True((await apps.Find(new BsonDocument("_id", guards)).SingleAsync())["AppOveragePaymentBlocked"].AsBoolean);
            Assert.False((await records.Find(new BsonDocument("_id", payer)).SingleAsync()).GetValue("OveragePaymentBlocked", false).AsBoolean);
            await NoMutation(async () => await Call("AuthorizeAsync", Event(guards, users[1]), 0, CancellationToken.None), "APP_OVERAGE_PAYMENT_BLOCKED");
            await Call("RecordInvoiceAsync", mappedId, null, Invoice("paid", 1), "owned-app-invoice-paid", "owned-canonical-input-2", CancellationToken.None);
            await Call("RecordInvoiceAsync", mappedId, null, Invoice("paid", 1), "owned-app-invoice-paid", "owned-canonical-input-2", CancellationToken.None);
            Assert.False((await apps.Find(new BsonDocument("_id", guards)).SingleAsync())["AppOveragePaymentBlocked"].AsBoolean);
            var order = await db.GetCollection<BsonDocument>("subscription_orders").Find(new BsonDocument("StripeInvoiceId", "in_owned_app_charge_fixture")).SingleAsync();
            Assert.Equal(payer, order["UserId"].AsString);
            Assert.Contains(guards, order["Description"].AsString);
            var statementType = api.GetType(ns + "PaidOverageStatement", true)!;
            var metadataStatement = Activator.CreateInstance(statementType)!;
            Set(metadataStatement, "Id", mappedId); Set(metadataStatement, "UserId", "app:" + guards);
            Set(metadataStatement, "AppId", guards); Set(metadataStatement, "PayerAvatarId", payer);
            Set(metadataStatement, "Scope", databaseName); Set(metadataStatement, "Month", month);
            var metadataMethod = api.GetType(ns + "StripePaidOverageGateway", true)!.GetMethod("IdentityMetadata", BindingFlags.NonPublic | BindingFlags.Static)!;
            var metadata = (Dictionary<string, string>)metadataMethod.Invoke(null, [metadataStatement])!;
            Assert.Equal(payer, metadata["oasis_avatar_id"]); Assert.Equal(guards, metadata["oasis_app_id"]);
            Assert.Equal("app:" + guards, metadata["oasis_ledger_subject"]);
            Assert.Equal(databaseName, metadata["oasis_ledger_scope"]); Assert.Equal("app-request-overages-usd-v1", metadata["oasis_billing_kind"]);
            Set(metadataStatement, "UserId", payer); Set(metadataStatement, "AppId", null); Set(metadataStatement, "PayerAvatarId", null); Set(metadataStatement, "Scope", "owned-personal-scope");
            metadata = (Dictionary<string, string>)metadataMethod.Invoke(null, [metadataStatement])!;
            Assert.Equal(payer, metadata["oasis_avatar_id"]); Assert.False(metadata.ContainsKey("oasis_app_id"));
            Assert.False(metadata.ContainsKey("oasis_ledger_subject")); Assert.Equal(5, metadata.Count);
            Console.WriteLine("App charge native verified: canonical invoice input failure fences the app, not the payer's unrelated personal account; paid/replay unblocks that app once and attributes its one order to the original payer GUID. This is NOT Stripe collection/webhook acceptance.");
            var dayId = "app:" + guards + ":day:" + day;
            await buckets.UpdateOneAsync(new BsonDocument("_id", dayId), new BsonDocument("$set", new BsonDocument("Requests", 10000L)));
            await NoMutation(async () => await Call("AuthorizeAsync", Event(guards, users[1]), 0, CancellationToken.None), "APP_DAILY_CALL_LIMIT_EXCEEDED");
            await buckets.UpdateOneAsync(new BsonDocument("_id", dayId), new BsonDocument("$set", new BsonDocument { { "Requests", 0L }, { "SettledUnits", 250000L } }));
            var tokens = Event(guards, users[1]); Set(tokens, "MeterCategory", "ai.tokens"); Set(tokens, "RequestedUnits", 1L);
            await NoMutation(async () => await Call("AuthorizeAsync", tokens, 0, CancellationToken.None), "APP_DAILY_TOKEN_LIMIT_EXCEEDED");
            await buckets.UpdateOneAsync(new BsonDocument("_id", dayId), new BsonDocument("$set", new BsonDocument("SettledUnits", 0L)));
            await buckets.UpdateOneAsync(new BsonDocument("_id", "app:" + guards + ":month:" + month), new BsonDocument("$set", new BsonDocument("ReservedCostUsd", new BsonDecimal128(10m))));
            var budget = Event(guards, users[1]); Set(budget, "ReservedCostUsd", 0.01m);
            await NoMutation(async () => await Call("AuthorizeAsync", budget, 0, CancellationToken.None), "APP_MONTHLY_BUDGET_EXCEEDED");
            Console.WriteLine("App charge native verified: missing/changed consent proof and changed payer refuse without any collection mutation; app daily call/token/provider-budget limits remain enforced while monthly request overages are enabled. No gateway/Stripe activation, external fee, statement allocation or collection acceptance.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", oldPool);
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", oldShards);
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_app_charge_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned app charge database absence and process settings restoration verified.");
        }
    }
}
