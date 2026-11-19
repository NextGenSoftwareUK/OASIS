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
    public async Task PersonalCreditsRespectOriginalCapsWriteoffsAndCreditOnlyScheduling()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_personal_edges_" + Guid.NewGuid().ToString("N");
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
            async Task<string> Execute(decimal cost, decimal? reserved = null)
            {
                var id = Guid.NewGuid().ToString("D");
                var value = Activator.CreateInstance(eventType)!;
                foreach (var item in new Dictionary<string, object> {
                    ["OperationId"] = id, ["UserId"] = user, ["ConsumingService"] = "WEB4", ["Endpoint"] = "/owned-personal-accounting",
                    ["MeterCategory"] = "api.request", ["Status"] = "authorized", ["RequestedUnits"] = 1L, ["ReservedCostUsd"] = reserved ?? cost,
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
            Task<object?> Prepare(string? key = null, bool creditsOnly = false) => Call("PrepareAsync", user, month, key ?? Guid.NewGuid().ToString("D"), CancellationToken.None, creditsOnly);
            var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
            var adjustments = db.GetCollection<BsonDocument>("subscription_overage_adjustments");
            async Task<BsonDocument> Statement(object? result)
            {
                Assert.NotNull(result);
                var id = result!.GetType().GetProperty("Id")!.GetValue(result)!.ToString();
                return await statements.Find(new BsonDocument("_id", id)).SingleAsync();
            }
            await Execute(budget); // Real included-cost ledger setup before the small original paid cap.
            await Consent(await Snapshot(), 0.5m);
            var first = await Execute(0.2m, 0.3m);
            await Execute(0.3m);
            var original = await Statement(await Prepare());
            Assert.Equal(50L, original["AmountMinorUsd"].ToInt64());
            await records.UpdateOneAsync(new BsonDocument("_id", user), new BsonDocument("$set", new BsonDocument {
                { "StripeCustomerId", "owned-personal-credit-customer-b" }, { "StripeSubscriptionId", "owned-personal-credit-sub-b" }
            }));
            await Consent(await Snapshot(), 1000m);
            var correctionType = api.GetType(ns + "UsageCorrectionRequest", true)!;
            object Correction(string id, decimal delta)
            {
                var request = Activator.CreateInstance(correctionType)!;
                Set(request, "CorrectionId", Guid.NewGuid().ToString("D")); Set(request, "OperationId", id);
                Set(request, "CorrectionKind", "billing-adjustment"); Set(request, "Reason", "Owned original-cap and credit-only verification");
                Set(request, "EvidenceReference", "owned-credit-input:" + id); Set(request, "CostDeltaUsd", delta);
                return request;
            }
            async Task<object> Correct(string id, decimal delta) => (await Call("CorrectAsync", "owned-credit-operator", Correction(id, delta), CancellationToken.None))!;
            decimal Field(object value, string name) => (decimal)value.GetType().GetProperty(name)!.GetValue(value)!;
            var writeoff = await Correct(first, 0.1m);
            Assert.Equal(0m, Field(writeoff, "SettledOverageDeltaUsd"));
            Assert.Equal(0.1m, Field(writeoff, "OverageWriteOffDeltaUsd"));
            Assert.Equal(0.1m, (await events.Find(new BsonDocument("OperationId", first)).SingleAsync())["PersonalAiWriteOffUsd"].ToDecimal());
            var refund = await Correct(first, -0.05m);
            Assert.Equal(-0.05m, Field(refund, "SettledOverageDeltaUsd"));
            var pendingPositive = await Execute(0.02m);
            var scheduled = (System.Collections.IEnumerable)(await Call("GetClosedAccountsAsync", DateTime.UtcNow, CancellationToken.None))!;
            Assert.Contains(scheduled.Cast<object>(), x => x.GetType().GetProperty("UserId")!.GetValue(x)!.ToString() == user &&
                (bool)x.GetType().GetProperty("CreditsOnly")!.GetValue(x)!);
            var creditKey = Guid.NewGuid().ToString("D");
            var credit = await Statement(await Prepare(creditKey, true));
            Assert.True(credit["CreditsOnly"].AsBoolean);
            Assert.Empty(credit["OperationIds"].AsBsonArray);
            Assert.Equal(-5L, credit["AmountMinorUsd"].ToInt64());
            Assert.Equal("owned-personal-consent-customer-a", credit["StripeCustomerId"].AsString);
            Assert.True((await events.Find(new BsonDocument("OperationId", pendingPositive)).SingleAsync())["BillingStatementId"].IsBsonNull);
            async Task<string> AllState()
            {
                var names = (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x).ToArray();
                var state = new List<string>();
                foreach (var name in names) state.Add(name + ":" + new BsonArray(await db.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson());
                return string.Join("|", state);
            }
            async Task RefusesWithoutMutation(Func<Task> action, string code)
            {
                var before = await AllState();
                var error = await Assert.ThrowsAnyAsync<Exception>(action);
                Assert.Contains(code, error.GetType().GetProperty("Code")?.GetValue(error)?.ToString() ?? error.Message);
                Assert.Equal(before, await AllState());
            }
            await RefusesWithoutMutation(async () => await Prepare(creditKey, false), "PERSONAL_STATEMENT_REQUEST_CONFLICT");
            var beforeEmpty = await AllState();
            Assert.Null(await Prepare(null, true));
            Assert.Equal(beforeEmpty, await AllState());
            var positive = await Statement(await Prepare());
            Assert.Equal(2L, positive["AmountMinorUsd"].ToInt64());
            Assert.Contains(pendingPositive, positive["OperationIds"].AsBsonArray.Select(x => x.AsString));
            var reverse = await Correct(first, -0.2m);
            Assert.Equal(-0.15m, Field(reverse, "SettledOverageDeltaUsd"));
            Assert.Equal(-0.05m, Field(reverse, "OverageWriteOffDeltaUsd"));
            var restore = await Correct(first, 0.2m);
            Assert.Equal(0.18m, Field(restore, "SettledOverageDeltaUsd"));
            Assert.Equal(0.02m, Field(restore, "OverageWriteOffDeltaUsd"));
            var restored = await events.Find(new BsonDocument("OperationId", first)).SingleAsync();
            Assert.Equal(0.07m, restored["PersonalAiWriteOffUsd"].ToDecimal());
            Assert.Equal(0.18m, restored["SettledAiOverageUsd"].ToDecimal() + restored["PersonalAiCorrectionUsd"].ToDecimal());
            var secondCredit = await Statement(await Prepare(null, true));
            Assert.Equal(-15L, secondCredit["AmountMinorUsd"].ToInt64());
            var restoredCharge = await Statement(await Prepare());
            Assert.Equal(18L, restoredCharge["AmountMinorUsd"].ToInt64());
            Assert.Equal("owned-personal-consent-customer-a", restoredCharge["StripeCustomerId"].AsString);
            var zero = await Execute(0m, 0.05m);
            Assert.Null(await Prepare());
            var zeroCorrection = await Correct(zero, 0.01m);
            Assert.Equal(0.01m, Field(zeroCorrection, "SettledOverageDeltaUsd"));
            var closed = (System.Collections.IEnumerable)(await Call("GetClosedAccountsAsync", DateTime.UtcNow.AddMonths(1), CancellationToken.None))!;
            Assert.Contains(closed.Cast<object>(), x => x.GetType().GetProperty("UserId")!.GetValue(x)!.ToString() == user &&
                !(bool)x.GetType().GetProperty("CreditsOnly")!.GetValue(x)!);
            var zeroStatement = await Statement(await Prepare());
            Assert.Contains(zero, zeroStatement["OperationIds"].AsBsonArray.Select(x => x.AsString));
            var audit = db.GetCollection<BsonDocument>("subscription_usage_audit");
            var legacyCorrection = (await audit.Find(new BsonDocument("_id", writeoff.GetType().GetProperty("Id")!.GetValue(writeoff)!.ToString())).SingleAsync()).DeepClone().AsBsonDocument;
            legacyCorrection["_id"] = "owned-unattributed-correction:" + Guid.NewGuid().ToString("D");
            legacyCorrection.Remove("PersonalChargeMovements"); legacyCorrection["SettledOverageDeltaUsd"] = new Decimal128(0.01m);
            await audit.InsertOneAsync(legacyCorrection); // Explicit unsupported historical INPUT, not an adopted financial record.
            await RefusesWithoutMutation(async () => await Correct(zero, 0.001m), "PERSONAL_CORRECTION_LEGACY_REVIEW_REQUIRED");
            await audit.DeleteOneAsync(new BsonDocument("_id", legacyCorrection["_id"]));
            var legacyStatement = original.DeepClone().AsBsonDocument;
            legacyStatement["_id"] = "ovg_" + new string('d', 64); legacyStatement.Remove("PersonalConsentIds");
            var last = await Execute(0.001m);
            await statements.InsertOneAsync(legacyStatement); // Explicit unbound historical INPUT must block new allocation.
            await RefusesWithoutMutation(async () => await Prepare(), "PERSONAL_STATEMENT_LEGACY_REVIEW_REQUIRED");
            await statements.DeleteOneAsync(new BsonDocument("_id", legacyStatement["_id"]));
            await Prepare();
            Assert.Equal(1L, await statements.CountDocumentsAsync(new BsonDocument("OperationIds", last)));
            var all = await statements.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var aggregate = await Call("GetAggregateForPeriodAsync", user, new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);
            Assert.Equal(0.511m, Field(aggregate!, "SettledOverageUsd"));
            Assert.Equal(0.511m, all.Sum(x => x["RequestChargesUsd"].ToDecimal() + x["AiChargesUsd"].ToDecimal()));
            Assert.Equal(0L, await adjustments.CountDocumentsAsync(new BsonDocument("BillingStatementId", BsonNull.Value)));
            Assert.DoesNotContain(await audit.Find(new BsonDocument("Kind", "correction")).ToListAsync(), x => x["_id"] == legacyCorrection["_id"]);
            Console.WriteLine("Personal credit edges native verified: original 0.50 USD cap survives current 1000 USD/different-customer consent; positive correction writes off excess, negative correction/reversal retains attributed writeoff conservation; current-month worker discovers credit-only intent; credit-only allocation excludes positive uninvoiced usage and positive adjustments; changed-intent replay refuses and empty polling is mutation-free; zero-original-charge positive correction is discovered/allocated once; unsupported historical correction/statement inputs refuse without mutation and are removed; real repository/native ledger totals conserve. Financial cost/readiness/account INPUTS only, not genuine Stripe, external fee measurements, production worker enablement, hosted or full portal acceptance.");
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_personal_edges_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned personal credit edges database absence verified.");
        }
    }
}
