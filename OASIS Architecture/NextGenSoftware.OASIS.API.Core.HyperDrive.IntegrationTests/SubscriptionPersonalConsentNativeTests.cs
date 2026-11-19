using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task PersonalConsentBindsPositiveExposureAndPreservesOriginalEvidence()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_personal_consent_" + Guid.NewGuid().ToString("N");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
            "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the actual API before this case.");
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
            var repositoryType = api.GetType(ns + "MongoSubscriptionUsageRepository", true)!;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["SUBSCRIPTION_MONGODB_CONNECTION_STRING"] = MongoUri, ["SUBSCRIPTION_MONGODB_DATABASE"] = databaseName,
                ["SUBSCRIPTION_LEDGER_INITIAL_STATE"] = "empty-new-installation"
            }).Build();
            var repository = Activator.CreateInstance(repositoryType, [config])!;
            async Task<object?> Call(string method, params object?[] args)
            {
                var task = (Task)repositoryType.GetMethod(method)!.Invoke(repository, args)!;
                await task;
                return task.GetType().GetProperty("Result")?.GetValue(task);
            }
            void Set(object value, string property, object? content) => value.GetType().GetProperty(property)!.SetValue(value, content);
            var db = mongo.GetDatabase(databaseName);
            var records = db.GetCollection<BsonDocument>("subscription_records");
            var billing = db.GetCollection<BsonDocument>("subscription_billing_audit");
            var events = db.GetCollection<BsonDocument>("subscription_usage_events");
            var user = Guid.NewGuid().ToString("D");
            var now = DateTime.UtcNow;
            var month = now.ToString("yyyy-MM");
            var recordType = api.GetType(ns + "SubscriptionRecord", true)!;
            var record = Activator.CreateInstance(recordType)!;
            Set(record, "UserId", user); Set(record, "PlanId", "free"); Set(record, "Status", "active");
            Set(record, "CurrentPeriodEnd", now.AddYears(1));
            await Call("SaveSubscriptionAsync", record, CancellationToken.None);
            // Native original verification/account INPUT fixtures only; no remote Stripe verification is claimed.
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument {
                { "PlanId", "bronze" }, { "StripeCustomerId", "owned-personal-consent-customer-a" },
                { "StripeSubscriptionId", "owned-personal-consent-sub-a" }
            }));
            var pricingType = api.GetType(ns + "PaidOveragePricing", true)!;
            var prices = pricingType.GetMethod("Load")!.Invoke(null, null)!;
            var version = (string)pricingType.GetProperty("Version")!.GetValue(prices)!;
            var hash = (string)pricingType.GetProperty("PricingSha256")!.GetValue(prices)!;
            var snapshotType = api.GetType(ns + "PersonalBillingPayerSnapshot", true)!;
            async Task<object> Snapshot(DateTime? time = null) => snapshotType.GetMethod("AfterVerification")!.Invoke(null,
                [await Call("GetSubscriptionAsync", user, CancellationToken.None), time ?? DateTime.UtcNow])!;
            Task Consent(object snapshot, decimal cap) => Call("SetPaidOverageConsentAsync", user, snapshot, version, hash, cap, CancellationToken.None);
            var consentType = api.GetType(ns + "PersonalChargeConsent", true)!;
            async Task<bool> HasBoundTerms() => (bool)consentType.GetMethod("HasBoundTerms")!.Invoke(null,
                [await Call("GetSubscriptionAsync", user, CancellationToken.None)])!;
            var eventType = api.GetType(ns + "SubscriptionUsageEvent", true)!;
            object Request()
            {
                var value = Activator.CreateInstance(eventType)!;
                foreach (var field in new Dictionary<string, object> {
                    ["OperationId"] = Guid.NewGuid().ToString("D"), ["UserId"] = user, ["ConsumingService"] = "WEB4",
                    ["Endpoint"] = "/owned-personal-consent", ["MeterCategory"] = "api.request", ["Status"] = "authorized",
                    ["RequestedUnits"] = 1L, ["RequestFingerprint"] = new string('b', 64),
                    ["AuthorizedAtUtc"] = DateTime.UtcNow, ["ExpiresAtUtc"] = DateTime.UtcNow.AddMinutes(15)
                }) Set(value, field.Key, field.Value);
                return value;
            }
            async Task NoMutation(Func<Task> action, string expected)
            {
                var names = (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x).ToArray();
                async Task<string> State(string name) => new BsonArray(await db.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson();
                var before = new List<string>(); foreach (var name in names) before.Add(await State(name));
                var error = await Assert.ThrowsAnyAsync<Exception>(action);
                Assert.Contains(expected, error.GetType().GetProperty("Code")?.GetValue(error)?.ToString() ?? error.Message);
                Assert.Equal(names, (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x).ToArray());
                for (var i = 0; i < names.Length; i++) Assert.Equal(before[i], await State(names[i]));
            }
            var verifiedBeforeUsage = await Snapshot();
            var included = Request();
            await Call("AuthorizeAsync", included, 0, CancellationToken.None);
            await Consent(verifiedBeforeUsage, 2m); // Ordinary authorization changes must not make payer verification stale.
            Assert.True(await HasBoundTerms());
            var stored = await records.Find(new BsonDocument("_id", user)).SingleAsync();
            var consentA = stored["PaidOverageConsentId"].AsString;
            Assert.Equal(1, stored["PaidOverageConsentSchemaVersion"].ToInt32());
            Assert.Equal(stored["PaidOveragePreferenceVersion"], stored["PaidOverageConsentPreferenceVersion"]);
            Assert.True((await events.Find(new BsonDocument("OperationId", eventType.GetProperty("OperationId")!.GetValue(included)!.ToString())).SingleAsync())
                .GetValue("PersonalOverageChargeTerms", BsonNull.Value).IsBsonNull);
            var evidenceA = await billing.Find(new BsonDocument("_id", "personal-overage-consent:" + consentA)).SingleAsync();
            using (var proof = JsonDocument.Parse(evidenceA["PayloadJson"].AsString))
                Assert.Equal("owned-personal-consent-customer-a", proof.RootElement.GetProperty("VerifiedPayer").GetProperty("CustomerId").GetString());
            var bucketId = user + ":month:" + month;
            await db.GetCollection<BsonDocument>("subscription_usage_buckets").UpdateOneAsync(new BsonDocument("_id", bucketId),
                new BsonDocument("$set", new BsonDocument("Requests", 300000L)));
            var paidA = Request();
            var fakeTerms = Activator.CreateInstance(api.GetType(ns + "PersonalOverageChargeTerms", true)!)!;
            Set(fakeTerms, "ConsentId", Guid.NewGuid().ToString("D")); Set(fakeTerms, "MonthlyCapUsd", 999m);
            Set(paidA, "PersonalOverageChargeTerms", fakeTerms);
            await Call("AuthorizeAsync", paidA, 0, CancellationToken.None);
            var operationA = eventType.GetProperty("OperationId")!.GetValue(paidA)!.ToString()!;
            var rowA = await events.Find(new BsonDocument("OperationId", operationA)).SingleAsync();
            Assert.Equal(consentA, rowA["PersonalOverageChargeTerms"]["ConsentId"].AsString);
            Assert.Equal(2m, rowA["PersonalOverageChargeTerms"]["MonthlyCapUsd"].ToDecimal());
            Assert.Equal(0.001m, rowA["ReservedRequestOverageUsd"].ToDecimal());
            Assert.Equal(999m, fakeTerms.GetType().GetProperty("MonthlyCapUsd")!.GetValue(fakeTerms));
            Assert.DoesNotContain("owned-personal-consent-customer", rowA.ToJson());
            Assert.DoesNotContain("owned-personal-consent-sub", rowA.ToJson());
            var beforeOptOut = await Snapshot();
            await Call("SetPayAsYouGoAsync", user, false, CancellationToken.None);
            Assert.False(await HasBoundTerms());
            await NoMutation(() => Consent(beforeOptOut, 2m), "verified paid subscription changed");
            Assert.Equal(evidenceA.ToJson(), (await billing.Find(new BsonDocument("_id", evidenceA["_id"])).SingleAsync()).ToJson());
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument {
                { "StripeCustomerId", "owned-personal-consent-customer-b" }, { "StripeSubscriptionId", "owned-personal-consent-sub-b" }
            }));
            await Consent(await Snapshot(), 2.5m);
            Assert.True(await HasBoundTerms());
            stored = await records.Find(new BsonDocument("_id", user)).SingleAsync();
            var consentB = stored["PaidOverageConsentId"].AsString;
            Assert.NotEqual(consentA, consentB);
            var paidB = Request(); await Call("AuthorizeAsync", paidB, 0, CancellationToken.None);
            var rowB = await events.Find(new BsonDocument("OperationId", eventType.GetProperty("OperationId")!.GetValue(paidB)!.ToString())).SingleAsync();
            Assert.Equal(consentB, rowB["PersonalOverageChargeTerms"]["ConsentId"].AsString);
            var evidenceB = await billing.Find(new BsonDocument("_id", "personal-overage-consent:" + consentB)).SingleAsync();
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument("MonthlyOverageLimitUsd", new Decimal128(2.51m))));
            await NoMutation(async () => await Call("AuthorizeAsync", Request(), 0, CancellationToken.None), "PERSONAL_OVERAGE_CONSENT_CONFLICT");
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument("MonthlyOverageLimitUsd", new Decimal128(2.5m))));
            await billing.DeleteOneAsync(new BsonDocument("_id", evidenceB["_id"]));
            await NoMutation(async () => await Call("AuthorizeAsync", Request(), 0, CancellationToken.None), "PERSONAL_OVERAGE_CONSENT_REQUIRED");
            await billing.InsertOneAsync(evidenceB);
            var oldPair = await Snapshot();
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument("StripeCustomerId", "owned-personal-consent-customer-c")));
            await NoMutation(() => Consent(oldPair, 2.5m), "verified paid subscription changed");
            await NoMutation(async () => await Call("AuthorizeAsync", Request(), 0, CancellationToken.None), "PERSONAL_OVERAGE_PAYER_CHANGED");
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument("StripeCustomerId", "owned-personal-consent-customer-b")));
            await NoMutation(async () => await Consent(await Snapshot(DateTime.UtcNow.AddMinutes(-6)), 2.5m), "verified paid subscription changed");
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument("PaidOverageConsentSchemaVersion", 0)));
            await NoMutation(async () => await Call("AuthorizeAsync", Request(), 0, CancellationToken.None), "PERSONAL_OVERAGE_CONSENT_REQUIRED");
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument {
                { "PaidOverageConsentSchemaVersion", 1 }, { "PlanId", "gold" }, { "MonthlyOverageLimitUsd", new Decimal128(0.5m) }
            }));
            Assert.False(await HasBoundTerms());
            await Call("StartAsync", new UsageStartRequest { UserId = user, OperationId = operationA, ConsumingService = "WEB4" }, CancellationToken.None);
            var native = db.GetCollection<BsonDocument>("owned_personal_consent_receipts");
            var nativeId = ObjectId.GenerateNewId();
            await native.InsertOneAsync(new BsonDocument { { "_id", nativeId }, { "OperationId", operationA } });
            Assert.Equal(operationA, (await native.Find(new BsonDocument("_id", nativeId)).SingleAsync())["OperationId"].AsString);
            var receipt = new UsageProviderReceipt { Provider = "MongoDBOASIS", ProviderRequestId = "owned-native:" + nativeId,
                ClientRequestId = operationA, Units = 1, ActualCostUsd = 0,
                CostSource = "owned-local-no-provider-fee", PricingCatalogueVersion = "owned-local-zero-cost" };
            await Call("SettleAsync", user, new UsageSettlementRequest { UserId = user, OperationId = operationA, ConsumingService = "WEB4",
                Outcome = "succeeded", Provider = receipt.Provider, ProviderRequestId = receipt.ProviderRequestId, Units = 1, ActualCostUsd = 0,
                CostSource = receipt.CostSource, PricingCatalogueVersion = receipt.PricingCatalogueVersion, ProviderReceipts = [receipt] }, CancellationToken.None);
            rowA = await events.Find(new BsonDocument("OperationId", operationA)).SingleAsync();
            Assert.Equal(consentA, rowA["PersonalOverageChargeTerms"]["ConsentId"].AsString);
            Assert.Equal("bronze", rowA["PersonalOverageChargeTerms"]["PlanId"].AsString);
            Assert.Equal(2m, rowA["PersonalOverageChargeTerms"]["MonthlyCapUsd"].ToDecimal());
            Assert.Equal(0.001m, rowA["SettledRequestOverageUsd"].ToDecimal());
            Assert.Equal(2L, await billing.CountDocumentsAsync(new BsonDocument("Kind", "overage-consent")));
            Console.WriteLine("Personal consent native verified: deterministic protected original customer/subscription/terms evidence; preference epoch survives ordinary authorization but rejects stale opt-out/identity/expiry verification; caller proof ignored; legacy/missing/changed proof refuses without mutation. Original customer-A consent/rate/cap/plan survives customer-B consent and current gold/cap changes during real native acknowledged settlement. No private routing in public events. Original verification/account INPUT fixtures only; no genuine Stripe readiness, new immutable statement/correction allocation, HTTP/browser or hosted acceptance.");
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_personal_consent_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned personal consent database absence verified.");
        }
    }
}
