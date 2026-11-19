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
    public async Task AppStatementsFreezePayersAllocateOnceAndRoundAcrossSubscriptions()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_app_statement_" + Guid.NewGuid().ToString("N");
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
            var db = mongo.GetDatabase(databaseName);
            var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
            var records = db.GetCollection<BsonDocument>("subscription_records");
            var events = db.GetCollection<BsonDocument>("subscription_usage_events");
            var audit = db.GetCollection<BsonDocument>("subscription_usage_audit");
            var billing = db.GetCollection<BsonDocument>("subscription_billing_audit");
            var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
            var adjustments = db.GetCollection<BsonDocument>("subscription_overage_adjustments");
            var buckets = db.GetCollection<BsonDocument>("subscription_usage_buckets");
            var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime;
            var month = now.ToString("yyyy-MM"); var day = now.ToString("yyyy-MM-dd");
            var actors = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString("D")).ToArray();
            var user = actors[0]; var payerOne = actors[1]; var payerTwo = actors[2];
            foreach (var actor in actors) {
                var account = Activator.CreateInstance(recordType)!;
                Set(account, "UserId", actor); Set(account, "PlanId", "free"); Set(account, "Status", "active");
                Set(account, "CurrentPeriodEnd", now.AddYears(1));
                await Call("SaveSubscriptionAsync", account, CancellationToken.None);
            }
            var priceType = api.GetType(ns + "AppOveragePrices", true)!;
            var prices = priceType.GetMethod("Load")!.Invoke(null, null)!;
            var rate = (decimal)priceType.GetMethod("Rate")!.Invoke(prices, ["bronze"])!;
            var version = (string)priceType.GetProperty("Version")!.GetValue(prices)!;
            var hash = (string)priceType.GetProperty("PricingSha256")!.GetValue(prices)!;
            async Task<string> Consent(string app, string payer, string customer, string subscription) {
                // Native financial INPUT FIXTURES, never genuine Stripe readiness or activation proof.
                await records.UpdateOneAsync(new BsonDocument("_id", payer), new BsonDocument("$set", new BsonDocument {
                    { "PlanId", "silver" }, { "Status", "active" }, { "StripeCustomerId", customer }, { "StripeSubscriptionId", subscription },
                    { "AuthorizationVersion", 1L }, { "OveragePaymentBlocked", false }
                }));
                await Call("SetAppOwnersAsync", app, new[] { payer }, "native-fixture", "Owned frozen statement inputs", CancellationToken.None);
                var id = Guid.NewGuid().ToString("D");
                var consentAt = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime;
                var settingsType = api.GetType(ns + "AppPaygSettings", true)!;
                var settings = Activator.CreateInstance(settingsType)!;
                foreach (var pair in new Dictionary<string, object> {
                    ["Enabled"] = true, ["MonthlyCapUsd"] = 0.5m, ["PricingVersion"] = version, ["PricingSha256"] = hash,
                    ["Currency"] = "USD", ["RatePerRequestUsd"] = rate, ["ConsentId"] = id, ["ConsentSchemaVersion"] = 1,
                    ["ConsentAtUtc"] = consentAt, ["PaymentVerifiedAtUtc"] = consentAt, ["ConsentBy"] = payer, ["BillingAvatarId"] = payer,
                    ["BillingAuthorizationVersion"] = 1L, ["VerifiedStripeCustomerId"] = customer, ["VerifiedStripeSubscriptionId"] = subscription
                }) Set(settings, pair.Key, pair.Value);
                await apps.UpdateOneAsync(new BsonDocument("_id", app), new BsonDocument("$set", new BsonDocument("Payg", settings.ToBsonDocument(settingsType))));
                await billing.InsertOneAsync(new BsonDocument { { "_id", "app-payg-consent:" + id }, { "Kind", "app-payg-settings-changed" },
                    { "UserId", "app:" + app }, { "OccurredAtUtc", consentAt }, { "PayloadJson", JsonSerializer.Serialize(new { AppId = app, Actor = payer, Settings = settings }) } });
                return id;
            }
            async Task Create(string app) {
                await Call("CreateAppAccountAsync", app, "Owned statement controls", "bronze", "native-fixture", "Verify allocation/rounding", true, "silver", CancellationToken.None);
                foreach (var type in new[] { "month", "day" }) await buckets.InsertOneAsync(new BsonDocument {
                    { "_id", "app:" + app + ":" + type + ":" + (type == "month" ? month : day) }, { "UserId", "app:" + app },
                    { "PeriodType", type }, { "Period", type == "month" ? month : day }, { "Requests", type == "month" ? 300000L : 0L }, { "UpdatedAtUtc", now }
                });
            }
            async Task<string> Operation(string app) {
                var id = Guid.NewGuid().ToString("D"); var value = Activator.CreateInstance(eventType)!;
                var authorized = DateTime.UtcNow;
                foreach (var pair in new Dictionary<string, object> {
                    ["OperationId"] = id, ["UserId"] = user, ["AppId"] = app, ["AppPlanId"] = "silver", ["PooledAppId"] = app,
                    ["PooledAppPlanId"] = "bronze", ["ConsumingService"] = "WEB4", ["Endpoint"] = "/owned-app-statement",
                    ["MeterCategory"] = "api.request", ["Status"] = "authorized", ["RequestFingerprint"] = new string('d', 64),
                    ["AuthorizedAtUtc"] = authorized, ["ExpiresAtUtc"] = now.AddMinutes(15)
                }) Set(value, pair.Key, pair.Value);
                await Call("AuthorizeAsync", value, 0, CancellationToken.None);
                await Call("StartAsync", new UsageStartRequest { OperationId = id, UserId = user, ConsumingService = "WEB4" }, CancellationToken.None);
                var native = db.GetCollection<BsonDocument>("owned_statement_measurements"); var nativeId = ObjectId.GenerateNewId();
                await native.InsertOneAsync(new BsonDocument { { "_id", nativeId }, { "OperationId", id } });
                Assert.Equal(id, (await native.Find(new BsonDocument("_id", nativeId)).SingleAsync())["OperationId"].AsString);
                var receipt = new UsageProviderReceipt { Provider = "MongoDBOASIS", ProviderRequestId = "owned-native:" + nativeId,
                    ClientRequestId = id, Units = 1, ActualCostUsd = 0, CostSource = "owned-local-no-provider-fee", PricingCatalogueVersion = "owned-local-zero-cost" };
                await Call("SettleAsync", user, new UsageSettlementRequest { OperationId = id, UserId = user, ConsumingService = "WEB4", Outcome = "succeeded",
                    Provider = receipt.Provider, ProviderRequestId = receipt.ProviderRequestId, Units = 1, ActualCostUsd = 0,
                    CostSource = receipt.CostSource, PricingCatalogueVersion = receipt.PricingCatalogueVersion, ProviderReceipts = new() { receipt } }, CancellationToken.None);
                return id;
            }
            async Task Credit(string operation, decimal delta) {
                var value = Activator.CreateInstance(api.GetType(ns + "UsageCorrectionRequest", true)!)!;
                foreach (var pair in new Dictionary<string, object> { ["OperationId"] = operation, ["CorrectionId"] = Guid.NewGuid().ToString("D"),
                    ["CorrectionKind"] = "billing-adjustment", ["Reason"] = "Owned allocation/credit boundary", ["EvidenceReference"] = "owned-statement-control", ["AppRequestChargeDeltaUsd"] = delta }) Set(value, pair.Key, pair.Value);
                await Call("CorrectAsync", "native-fixture", value, CancellationToken.None);
            }
            async Task<BsonDocument?> Prepare(string app, string key, bool creditsOnly = false) {
                var result = await Call("PrepareAsync", "app:" + app, month, key, CancellationToken.None, creditsOnly);
                if (result == null) return null;
                var id = (string)result.GetType().GetProperty("Id")!.GetValue(result)!;
                return await statements.Find(new BsonDocument("_id", id)).SingleAsync();
            }
            async Task NoMutation(Func<Task> action, string reason) {
                var names = (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                async Task<string> Snapshot(string name) => new BsonArray(await db.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson();
                var before = new List<string>(); foreach (var name in names) before.Add(await Snapshot(name));
                var error = await Assert.ThrowsAnyAsync<Exception>(action); Assert.Contains(reason, error.Message);
                Assert.Equal(names, (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                for (var i = 0; i < names.Length; i++) Assert.Equal(before[i], await Snapshot(names[i]));
            }
            var billingType = api.GetType(ns + "PaidOverageBillingService", true)!;
            var logger = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>).MakeGenericType(billingType).GetField("Instance")!.GetValue(null)!;
            var gateway = Activator.CreateInstance(api.GetType(ns + "StripePaidOverageGateway", true)!, [config])!;
            var collector = Activator.CreateInstance(billingType, [repository, gateway, logger, TimeProvider.System])!;
            async Task CollectZero(BsonDocument row) {
                Assert.Equal(0L, row["AmountMinorUsd"].ToInt64());
                var value = await Call("GetStatementAsync", row["_id"].AsString, CancellationToken.None);
                var task = (Task)billingType.GetMethod("CollectAsync")!.Invoke(collector, [value, CancellationToken.None])!; await task;
                var saved = await statements.Find(new BsonDocument("_id", row["_id"])).SingleAsync();
                Assert.Equal("paid", saved["State"].AsString); Assert.True(saved.GetValue("StripeInvoiceId", BsonNull.Value).IsBsonNull);
            }
            const string app = "owned-statement-main"; await Create(app);
            var consentA = await Consent(app, payerOne, "owned-customer-one", "owned-subscription-a");
            var firstOps = new List<string>(); for (var i = 0; i < 5; i++) firstOps.Add(await Operation(app));
            var first = (await Prepare(app, "first-five"))!;
            Assert.Equal(rate * 5, first["RequestChargesUsd"].ToDecimal()); Assert.Equal(1L, first["AmountMinorUsd"].ToInt64());
            Assert.Equal(payerOne, first["PayerAvatarId"].AsString); Assert.Equal(databaseName, first["Scope"].AsString);
            Assert.Equal(5, first["OperationIds"].AsBsonArray.Count); Assert.Equal(consentA, first["AppConsentIds"].AsBsonArray.Single().AsString);
            var consentB = await Consent(app, payerOne, "owned-customer-one", "owned-subscription-b");
            var secondOps = new List<string>(); for (var i = 0; i < 5; i++) secondOps.Add(await Operation(app));
            var second = (await Prepare(app, "second-five"))!;
            Assert.Equal(rate * 5, second["RequestChargesUsd"].ToDecimal()); Assert.Equal(0L, second["AmountMinorUsd"].ToInt64());
            Assert.Equal("owned-subscription-b", second["StripeSubscriptionId"].AsString);
            await CollectZero(second); // Actual gateway zero-due branch, no Stripe request or key.
            var pending = new[] { await Operation(app), await Operation(app) };
            await Credit(firstOps[0], -rate);
            var scheduled = (System.Collections.IEnumerable)(await Call("GetClosedAccountsAsync", now, CancellationToken.None))!;
            var scheduledApp = scheduled.Cast<object>().Single(x => (string)x.GetType().GetProperty("UserId")!.GetValue(x)! == "app:" + app);
            Assert.True((bool)scheduledApp.GetType().GetProperty("CreditsOnly")!.GetValue(scheduledApp)!);
            var credit = (await Prepare(app, "scheduled-credit", true))!;
            Assert.Equal(-rate, credit["RequestChargesUsd"].ToDecimal()); Assert.Empty(credit["OperationIds"].AsBsonArray);
            Assert.Single(credit["AdjustmentIds"].AsBsonArray); Assert.Equal(0L, credit["AmountMinorUsd"].ToInt64());
            Assert.True(credit["CreditsOnly"].AsBoolean); await CollectZero(credit);
            foreach (var id in pending) Assert.True((await events.Find(new BsonDocument("OperationId", id)).SingleAsync())["AppBillingStatementId"].IsBsonNull);
            await NoMutation(async () => await Prepare(app, "scheduled-credit", false), "APP_STATEMENT_REQUEST_CONFLICT");
            var consentC = await Consent(app, payerTwo, "owned-customer-two", "owned-subscription-c");
            var otherPayerOps = new[] { await Operation(app), await Operation(app) };
            await Call("UpdateAppAccountAsync", app, "gold", "disabled", "native-fixture", "Prior liability must retain original identity", null, null, CancellationToken.None);
            await apps.UpdateOneAsync(new BsonDocument("_id", app), new BsonDocument("$set", new BsonDocument {
                { "Payg.Enabled", false }, { "Payg.MonthlyCapUsd", new BsonDecimal128(1m) }, { "Payg.RatePerRequestUsd", new BsonDecimal128(rate / 5) },
                { "Payg.BillingAvatarId", Guid.NewGuid().ToString("D") }, { "Payg.VerifiedStripeCustomerId", "owned-current-unrelated-customer" },
                { "OwnerAvatarIds", new BsonArray() }
            }));
            await records.UpdateOneAsync(new BsonDocument("_id", payerOne), new BsonDocument("$set", new BsonDocument {
                { "PlanId", "free" }, { "Status", "cancelled" }, { "StripeCustomerId", "owned-current-unrelated-customer" }, { "StripeSubscriptionId", "owned-current-unrelated-subscription" }
            }));
            // Recreate the real repository: only durable original evidence may choose the payee.
            repository = Activator.CreateInstance(repositoryType, [config])!;
            var replayBefore = await billing.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            var replay = (await Prepare(app, "first-five"))!;
            Assert.Equal(first.ToJson(), replay.ToJson()); Assert.Equal(replayBefore, await billing.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var customerBatches = new[] { (await Prepare(app, "first-remaining-customer"))!, (await Prepare(app, "second-remaining-customer"))! };
            var oldCustomer = customerBatches.Single(x => x["PayerAvatarId"].AsString == payerOne);
            Assert.Equal("owned-customer-one", oldCustomer["StripeCustomerId"].AsString);
            Assert.Equal(rate * 2, oldCustomer["RequestChargesUsd"].ToDecimal()); Assert.Equal(0L, oldCustomer["AmountMinorUsd"].ToInt64());
            Assert.Equal(consentB, oldCustomer["AppConsentIds"].AsBsonArray.Single().AsString);
            var newCustomer = customerBatches.Single(x => x["PayerAvatarId"].AsString == payerTwo);
            Assert.Equal("owned-customer-two", newCustomer["StripeCustomerId"].AsString);
            Assert.Equal(rate * 2, newCustomer["RequestChargesUsd"].ToDecimal()); Assert.Equal(0L, newCustomer["AmountMinorUsd"].ToInt64());
            Assert.Equal(consentC, newCustomer["AppConsentIds"].AsBsonArray.Single().AsString);
            Assert.Null(await Prepare(app, "empty-after-allocation"));
            Console.WriteLine("App statement native verified: original payer/customer/subscription/consent and rates survive current owner/settings/account changes and repository recreation; five-plus-five milli-dollar batches across subscriptions round1+0 cents for the same customer; distinct customer stays separate; replay allocates once; scheduled current-month credit excludes unbilled positive operations.");
            // Seven credits reduce customer-one's cumulative0.011 to0.004, producing one cent credit.
            foreach (var id in firstOps.Skip(1).Concat(secondOps.Take(3))) await Credit(id, -rate);
            var negative = (await Prepare(app, "negative-credit", true))!;
            Assert.Equal(-rate * 7, negative["RequestChargesUsd"].ToDecimal()); Assert.Equal(-1L, negative["AmountMinorUsd"].ToInt64());
            Assert.Equal(payerOne, negative["PayerAvatarId"].AsString); Assert.Equal("owned-customer-one", negative["StripeCustomerId"].AsString);
            Assert.Equal(7, negative["AdjustmentIds"].AsBsonArray.Count); Assert.Empty(negative["OperationIds"].AsBsonArray);
            Assert.Null(await Prepare(app, "no-credit-repeat", true));
            var check = await Call("CheckAppChargeProjectionAsync", app, month, CancellationToken.None);
            Assert.True((bool)check!.GetType().GetProperty("Balanced")!.GetValue(check)!);
            var restoredOp = firstOps[1];
            var baseId = "app:" + app + ":month:" + month;
            var originalBucket = await buckets.Find(new BsonDocument("_id", baseId)).SingleAsync();
            var months = await buckets.Find(new BsonDocument { { "PeriodType", "month" }, { "UserId", new BsonRegularExpression("^app:" + app + "(#|$)") } }).ToListAsync();
            var total = months.Sum(x => x.GetValue("ReservedAppRequestOverageUsd", 0).ToDecimal() + x.GetValue("SettledAppRequestOverageUsd", 0).ToDecimal());
            // Explicit owned monetary boundary fixture, not a claimed provider fee or historical repair.
            await buckets.UpdateOneAsync(new BsonDocument("_id", baseId), new BsonDocument("$set", new BsonDocument("SettledAppRequestOverageUsd",
                new BsonDecimal128(originalBucket.GetValue("SettledAppRequestOverageUsd", 0).ToDecimal() + 0.5m - total))));
            await NoMutation(() => Credit(restoredOp, rate), "APP_CHARGE_CAP_EXCEEDED");
            await buckets.ReplaceOneAsync(new BsonDocument("_id", baseId), originalBucket);
            check = await Call("CheckAppChargeProjectionAsync", app, month, CancellationToken.None);
            Assert.True((bool)check!.GetType().GetProperty("Balanced")!.GetValue(check)!);
            Console.WriteLine("App statement native verified: post-statement credits bind original source operation/consent/payer, round cumulative negative cents once; positive credit reversal at a full original cap refuses without collection changes. Projection rechecked after owned boundary fixture restoration. No nonzero Stripe collection attempted.");
            const string zeroApp = "owned-statement-zero"; await Create(zeroApp); await Consent(zeroApp, payerOne, "owned-zero-customer", "owned-zero-subscription");
            var zeroOp = await Operation(zeroApp); await Credit(zeroOp, -rate);
            var zero = (await Prepare(zeroApp, "fully-credited"))!;
            Assert.Equal(0m, zero["RequestChargesUsd"].ToDecimal()); Assert.Equal(0L, zero["AmountMinorUsd"].ToInt64());
            Assert.Single(zero["OperationIds"].AsBsonArray); await CollectZero(zero);
            Assert.Null(await Prepare(zeroApp, "no-zero-repeat"));
            var zeroAccountBefore = (await apps.Find(new BsonDocument("_id", zeroApp)).SingleAsync()).ToJson();
            Assert.Null(await Prepare(zeroApp, "empty-poll"));
            Assert.Equal(zeroAccountBefore, (await apps.Find(new BsonDocument("_id", zeroApp)).SingleAsync()).ToJson());
            Console.WriteLine("App statement native verified: fully credited operation allocates a zero-due statement once, actual zero-amount gateway/store completes without Stripe, and empty polls make no app-version writes.");
            const string raceApp = "owned-statement-race"; await Create(raceApp); await Consent(raceApp, payerOne, "owned-race-customer", "owned-race-subscription");
            var raceOp = await Operation(raceApp);
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var preparing = Task.Run(async () => { await gate.Task; return await Prepare(raceApp, "racing-statement"); });
            var correcting = Task.Run(async () => { await gate.Task; await Credit(raceOp, -rate); });
            gate.SetResult(true); await Task.WhenAll(preparing, correcting);
            await Prepare(raceApp, "remaining-race-credit", true);
            var raceRows = await statements.Find(new BsonDocument("AppId", raceApp)).ToListAsync();
            Assert.Equal(0m, raceRows.Sum(x => x["RequestChargesUsd"].ToDecimal()));
            Assert.Equal(0L, raceRows.Sum(x => x["AmountMinorUsd"].ToInt64()));
            Assert.Equal(2L, (await events.Find(new BsonDocument("OperationId", raceOp)).SingleAsync())["AppChargeVersion"].ToInt64());
            Assert.Equal(0L, await adjustments.CountDocumentsAsync(new BsonDocument { { "UserId", "app:" + raceApp }, { "BillingStatementId", BsonNull.Value } }));
            check = await Call("CheckAppChargeProjectionAsync", raceApp, month, CancellationToken.None);
            Assert.True((bool)check!.GetType().GetProperty("Balanced")!.GetValue(check)!);
            Console.WriteLine("App statement native verified: actual concurrent correction/preparation conserves net0 in either commit order; shared operation version2, no unallocated credit, no duplicate allocation, money projection balanced.");
            const string badApp = "owned-statement-evidence"; await Create(badApp); var badConsent = await Consent(badApp, payerOne, "owned-evidence-customer", "owned-evidence-subscription");
            var badOp = await Operation(badApp);
            var evidence = await billing.Find(new BsonDocument("_id", "app-payg-consent:" + badConsent)).SingleAsync();
            await billing.DeleteOneAsync(new BsonDocument("_id", evidence["_id"]));
            await NoMutation(async () => await Prepare(badApp, "missing-proof"), "APP_STATEMENT_CONSENT_REQUIRED");
            await billing.InsertOneAsync(evidence);
            await statements.InsertOneAsync(new BsonDocument { { "_id", "owned-unbound-statement" }, { "UserId", "app:" + badApp }, { "Month", month }, { "Scope", databaseName } });
            await NoMutation(async () => await Prepare(badApp, "legacy-statement"), "APP_STATEMENT_LEGACY_REVIEW_REQUIRED");
            await statements.DeleteOneAsync(new BsonDocument("_id", "owned-unbound-statement"));
            var subject = "app:" + badApp;
            var opDocument = await events.Find(new BsonDocument("OperationId", badOp)).SingleAsync();
            await events.UpdateOneAsync(new BsonDocument("OperationId", badOp), new BsonDocument("$unset", new BsonDocument("AppRequestChargeTerms", "")));
            await NoMutation(async () => await Prepare(badApp, "missing-terms"), "APP_STATEMENT_CONSENT_REQUIRED");
            await events.ReplaceOneAsync(new BsonDocument("OperationId", badOp), opDocument);
            var concurrent = await Task.WhenAll(Enumerable.Range(0, 3).Select(i => Prepare(badApp, "concurrent-" + i)));
            Assert.Single(concurrent.Where(x => x != null));
            Assert.Equal(1L, await statements.CountDocumentsAsync(new BsonDocument("AppId", badApp)));
            Assert.Equal(1L, (await events.Find(new BsonDocument("OperationId", badOp)).SingleAsync())["AppChargeVersion"].ToInt64());
            Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Console.WriteLine("App statement native verified: missing protected evidence, missing frozen terms and unbound legacy statement refuse without collection mutation; three concurrent preparers allocate one operation once. All financial records are owned native input fixtures; no genuine Stripe readiness, positive/nonzero collection, signed webhook, hosted UI or production Startup acceptance.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", oldPool);
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", oldShards);
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_app_statement_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned app statement database absence and process settings restoration verified.");
        }
    }
}
