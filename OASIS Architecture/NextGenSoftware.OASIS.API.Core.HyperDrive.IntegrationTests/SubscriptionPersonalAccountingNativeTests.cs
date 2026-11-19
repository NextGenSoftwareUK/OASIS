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
    public async Task PersonalStatementsAndCorrectionsPreserveOriginalAccountsAndConserveMoney()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_personal_acct_" + Guid.NewGuid().ToString("N");
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
            var eventType = api.GetType(ns + "SubscriptionUsageEvent", true)!;
            var service = api.GetType(ns + "SubscriptionService", true)!;
            var policy = service.GetMethod("GetUsagePolicy")!.Invoke(null, ["bronze", 0])!;
            var budget = (decimal)policy.GetType().GetProperty("MonthlyBudgetUsd")!.GetValue(policy)!;
            Assert.InRange(budget, 0.02m, 999m);
            await Consent(await Snapshot(), 1000m);
            var consentA = (await records.Find(new BsonDocument("_id", user)).SingleAsync())["PaidOverageConsentId"].AsString;
            async Task<string> Execute(decimal cost)
            {
                var id = Guid.NewGuid().ToString("D");
                var value = Activator.CreateInstance(eventType)!;
                foreach (var item in new Dictionary<string, object> {
                    ["OperationId"] = id, ["UserId"] = user, ["ConsumingService"] = "WEB4", ["Endpoint"] = "/owned-personal-accounting",
                    ["MeterCategory"] = "api.request", ["Status"] = "authorized", ["RequestedUnits"] = 1L, ["ReservedCostUsd"] = cost,
                    ["RequestFingerprint"] = new string('c', 64), ["AuthorizedAtUtc"] = DateTime.UtcNow,
                    ["ExpiresAtUtc"] = DateTime.UtcNow.AddMinutes(15)
                }) Set(value, item.Key, item.Value);
                await Call("AuthorizeAsync", value, 0, CancellationToken.None);
                await Call("StartAsync", new UsageStartRequest { UserId = user, OperationId = id, ConsumingService = "WEB4" }, CancellationToken.None);
                var receipts = db.GetCollection<BsonDocument>("owned_personal_accounting_receipts");
                var nativeId = ObjectId.GenerateNewId();
                await receipts.InsertOneAsync(new BsonDocument { { "_id", nativeId }, { "OperationId", id } });
                Assert.Equal(id, (await receipts.Find(new BsonDocument("_id", nativeId)).SingleAsync())["OperationId"].AsString);
                // Cost is an explicit financial INPUT for accounting, not a claimed Mongo fee or external price measurement.
                var receipt = new UsageProviderReceipt { Provider = "MongoDBOASIS", ProviderRequestId = "owned-accounting:" + nativeId,
                    ClientRequestId = id, Units = 1, ActualCostUsd = cost, CostSource = "owned-financial-accounting-input",
                    PricingCatalogueVersion = "owned-accounting-input-v1" };
                await Call("SettleAsync", user, new UsageSettlementRequest { UserId = user, OperationId = id, ConsumingService = "WEB4",
                    Outcome = "succeeded", Provider = receipt.Provider, ProviderRequestId = receipt.ProviderRequestId, Units = 1,
                    ActualCostUsd = cost, CostSource = receipt.CostSource, PricingCatalogueVersion = receipt.PricingCatalogueVersion,
                    ProviderReceipts = [receipt] }, CancellationToken.None);
                return id;
            }
            Task<object?> Prepare(string? key = null) => Call("PrepareAsync", user, month, key ?? Guid.NewGuid().ToString("D"), CancellationToken.None, false);
            var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
            var adjustments = db.GetCollection<BsonDocument>("subscription_overage_adjustments");
            async Task<BsonDocument> Statement(object? result)
            {
                Assert.NotNull(result);
                var id = result!.GetType().GetProperty("Id")!.GetValue(result)!.ToString();
                return await statements.Find(new BsonDocument("_id", id)).SingleAsync();
            }
            var baseline = await Execute(budget);
            var a1 = await Execute(0.005m);
            var firstKey = Guid.NewGuid().ToString("D");
            var sa1 = await Statement(await Prepare(firstKey));
            Assert.Equal("owned-personal-consent-customer-a", sa1["StripeCustomerId"].AsString);
            Assert.Contains(consentA, sa1["PersonalConsentIds"].AsBsonArray.Select(x => x.AsString));
            Assert.Equal(1L, sa1["AmountMinorUsd"].ToInt64());
            Assert.Equal(sa1["_id"], (await Statement(await Prepare(firstKey)))["_id"]);
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument("StripeSubscriptionId", "owned-personal-accounting-sub-a2")));
            await Consent(await Snapshot(), 1000m);
            var a2 = await Execute(0.005m);
            var sa2 = await Statement(await Prepare());
            Assert.Equal("owned-personal-accounting-sub-a2", sa2["StripeSubscriptionId"].AsString);
            Assert.Equal(0L, sa2["AmountMinorUsd"].ToInt64());
            Assert.NotEqual(sa1["PersonalConsentIds"].ToJson(), sa2["PersonalConsentIds"].ToJson());
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument {
                { "StripeCustomerId", "owned-personal-accounting-customer-b" }, { "StripeSubscriptionId", "owned-personal-accounting-sub-b" }
            }));
            await Consent(await Snapshot(), 1000m);
            var b1 = await Execute(0.006m);
            // Current identity is intentionally unavailable. Allocation must use the protected original account.
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument {
                { "StripeCustomerId", BsonNull.Value }, { "StripeSubscriptionId", BsonNull.Value }
            }));
            var sb1 = await Statement(await Prepare());
            Assert.Equal("owned-personal-accounting-customer-b", sb1["StripeCustomerId"].AsString);
            Assert.Equal(1L, sb1["AmountMinorUsd"].ToInt64());
            var correctionType = api.GetType(ns + "UsageCorrectionRequest", true)!;
            object Correction(string id, decimal delta)
            {
                var request = Activator.CreateInstance(correctionType)!;
                Set(request, "CorrectionId", Guid.NewGuid().ToString("D")); Set(request, "OperationId", id);
                Set(request, "CorrectionKind", "billing-adjustment"); Set(request, "Reason", "Owned original-account accounting verification");
                Set(request, "EvidenceReference", "owned-accounting-input:" + id); Set(request, "CostDeltaUsd", delta);
                return request;
            }
            var change = Correction(baseline, -0.012m);
            var corrected = await Call("CorrectAsync", "owned-accounting-operator", change, CancellationToken.None);
            Assert.NotNull(corrected);
            var movements = (Array)corrected!.GetType().GetProperty("PersonalChargeMovements")!.GetValue(corrected)!;
            Assert.Equal(3, movements.Length);
            Assert.Equal(-0.012m, corrected.GetType().GetProperty("SettledOverageDeltaUsd")!.GetValue(corrected));
            var replay = await Call("CorrectAsync", "owned-accounting-operator", change, CancellationToken.None);
            Assert.Equal(corrected.GetType().GetProperty("Id")!.GetValue(corrected), replay!.GetType().GetProperty("Id")!.GetValue(replay));
            Assert.Equal(3L, await adjustments.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var credit1 = await Statement(await Prepare()); var credit2 = await Statement(await Prepare());
            Assert.Equal(-1L, credit1["AmountMinorUsd"].ToInt64()); Assert.Equal(-1L, credit2["AmountMinorUsd"].ToInt64());
            Assert.NotEqual(credit1["StripeCustomerId"], credit2["StripeCustomerId"]);
            Assert.Null(await Prepare());
            foreach (var adjustment in await adjustments.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()) {
                Assert.Equal(databaseName, adjustment["Scope"].AsString);
                Assert.Contains(adjustment["SourceOperationId"].AsString, new[] { a1, a2, b1 });
                Assert.False(adjustment["SourceStatementId"].IsBsonNull);
                Assert.False(adjustment["PersonalOverageChargeTerms"].IsBsonNull);
            }
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument {
                { "StripeCustomerId", "owned-personal-accounting-customer-b" }, { "StripeSubscriptionId", "owned-personal-accounting-sub-b" }
            }));
            var b2 = await Execute(0.003m);
            var raceCorrection = Correction(b1, -0.001m);
            await Task.WhenAll(Call("CorrectAsync", "owned-accounting-operator", raceCorrection, CancellationToken.None), Prepare());
            // Drain any credit committed after the competing allocation; bounded to the known fixture size.
            for (var i = 0; i < 3; i++) if (await Prepare() == null) break;
            var all = await statements.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var accountA = all.Where(x => x["StripeCustomerId"].AsString == "owned-personal-consent-customer-a").ToArray();
            var accountB = all.Where(x => x["StripeCustomerId"].AsString == "owned-personal-accounting-customer-b").ToArray();
            Assert.Equal(0m, accountA.Sum(x => x["RequestChargesUsd"].ToDecimal() + x["AiChargesUsd"].ToDecimal()));
            Assert.Equal(0L, accountA.Sum(x => x["AmountMinorUsd"].ToInt64()));
            Assert.Equal(0.006m, accountB.Sum(x => x["RequestChargesUsd"].ToDecimal() + x["AiChargesUsd"].ToDecimal()));
            Assert.Equal(1L, accountB.Sum(x => x["AmountMinorUsd"].ToInt64()));
            Assert.Single(all.Where(x => x["OperationIds"].AsBsonArray.Contains(b2)));
            Assert.Equal(0L, await adjustments.CountDocumentsAsync(new BsonDocument("BillingStatementId", BsonNull.Value)));
            var b3 = await Execute(0.003m);
            await Task.WhenAll(Prepare(), Prepare(), Prepare());
            Assert.Equal(1L, await statements.CountDocumentsAsync(new BsonDocument("OperationIds", b3)));
            var missing = await Execute(0.003m);
            var current = await records.Find(new BsonDocument("_id", user)).SingleAsync();
            var evidence = await billing.Find(new BsonDocument("_id", "personal-overage-consent:" + current["PaidOverageConsentId"].AsString)).SingleAsync();
            await billing.DeleteOneAsync(new BsonDocument("_id", evidence["_id"]));
            async Task<string> State() => string.Join("|", new[] {
                new BsonArray(await statements.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson(),
                new BsonArray(await events.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson(),
                new BsonArray(await adjustments.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson(),
                (await records.Find(new BsonDocument("_id", user)).SingleAsync()).ToJson()
            });
            var beforeMissing = await State();
            var missingError = await Assert.ThrowsAnyAsync<Exception>(async () => await Prepare());
            Assert.Contains("PERSONAL_OVERAGE_CONSENT_REQUIRED", missingError.GetType().GetProperty("Code")?.GetValue(missingError)?.ToString() ?? missingError.Message);
            Assert.Equal(beforeMissing, await State());
            await billing.InsertOneAsync(evidence);
            await Prepare();
            Assert.Equal(1L, await statements.CountDocumentsAsync(new BsonDocument("OperationIds", missing)));
            var aggregate = await Call("GetAggregateForPeriodAsync", user, new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);
            var charges = (decimal)aggregate!.GetType().GetProperty("SettledOverageUsd")!.GetValue(aggregate)!;
            all = await statements.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            Assert.Equal(charges, all.Sum(x => x["RequestChargesUsd"].ToDecimal() + x["AiChargesUsd"].ToDecimal()));
            Assert.DoesNotContain("owned-personal-accounting-customer", (await events.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToJson());
            Console.WriteLine("Personal original-account accounting native verified: actual authorization/start/native acknowledgement/settlement; financial cost/readiness INPUTS only; original customer survives current routing removal; subscription/consent changes round cumulatively on one customer, separate customers round independently; threshold correction attributes three original operations/accounts/statements; replay once; real correction/allocation race conserves without stranded credits; three preparers allocate once; missing proof refuses without financial mutation; monthly native statement/ledger liability conserved. Not genuine Stripe/nonzero collection/webhook/hosted or full portal acceptance.");
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_personal_acct_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned personal accounting database absence verified.");
        }
    }
}
