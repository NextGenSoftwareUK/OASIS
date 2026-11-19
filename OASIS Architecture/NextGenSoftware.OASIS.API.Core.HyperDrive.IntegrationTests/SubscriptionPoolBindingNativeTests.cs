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
    public async Task PooledMovementsKeepOriginalShardAcrossSettingsAndRepositoryRestart()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_pool_binding_" + Guid.NewGuid().ToString("N");
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
            var user = Guid.NewGuid().ToString("D");
            var record = Activator.CreateInstance(recordType)!;
            Set(record, "UserId", user); Set(record, "PlanId", "free"); Set(record, "Status", "active");
            Set(record, "CurrentPeriodEnd", DateTime.UtcNow.AddYears(1));
            await Call("SaveSubscriptionAsync", record, CancellationToken.None);
            var db = mongo.GetDatabase(databaseName);
            var events = db.GetCollection<BsonDocument>("subscription_usage_events");
            var audit = db.GetCollection<BsonDocument>("subscription_usage_audit");
            var buckets = db.GetCollection<BsonDocument>("subscription_usage_buckets");
            var native = db.GetCollection<BsonDocument>("owned_provider_measurements");
            string Route(string app, string id, int count) => (string)repositoryType.GetMethod("PoolWriteSubject")!.Invoke(null, [app, id, count])!;
            object Event(string app, string id, decimal reserve, bool tokens = false)
            {
                var value = Activator.CreateInstance(eventType)!;
                var now = DateTime.UtcNow;
                foreach (var item in new Dictionary<string, object> {
                    ["OperationId"] = id, ["UserId"] = user, ["AppId"] = app, ["AppPlanId"] = "silver",
                    ["PooledAppId"] = app, ["PooledAppPlanId"] = "bronze", ["ConsumingService"] = "WEB4",
                    ["Endpoint"] = "/owned-pool-binding", ["MeterCategory"] = tokens ? "ai.tokens" : "api.request", ["Status"] = "authorized",
                    ["RequestedUnits"] = tokens ? 10L : 0L, ["ReservedCostUsd"] = reserve,
                    ["RequestFingerprint"] = new string('b', 64), ["AuthorizedAtUtc"] = now, ["ExpiresAtUtc"] = now.AddMinutes(15),
                    // A caller cannot select the accounting route; the repository must replace this.
                    ["PoolShardCountAtAuthorization"] = 999
                }) Set(value, item.Key, item.Value);
                return value;
            }
            UsageStartRequest Start(string id) => new() { OperationId = id, UserId = user, ConsumingService = "WEB4" };
            async Task<UsageSettlementRequest> NativeWrite(string id, bool failed = false)
            {
                var documentId = ObjectId.GenerateNewId();
                await native.InsertOneAsync(new BsonDocument { { "_id", documentId }, { "OperationId", id } });
                if (failed)
                {
                    var error = await Assert.ThrowsAsync<MongoWriteException>(() => native.InsertOneAsync(
                        new BsonDocument { { "_id", documentId }, { "OperationId", id } }));
                    Assert.Equal(ServerErrorCategory.DuplicateKey, error.WriteError.Category);
                }
                Assert.Equal(id, (await native.Find(new BsonDocument("_id", documentId)).SingleAsync())["OperationId"].AsString);
                // This owned local MongoDB has no monetary provider fee. These are actual insert/error
                // acknowledgements, not invented paid receipts or acceptance of external charge pricing.
                var receipt = new UsageProviderReceipt {
                    Provider = "MongoDBOASIS", ProviderRequestId = "owned-native:" + documentId + (failed ? ":duplicate-key" : ":insert"),
                    ClientRequestId = id, Units = failed ? 0 : 1, ActualCostUsd = 0m,
                    CostSource = "owned-local-no-provider-fee", PricingCatalogueVersion = "owned-local-zero-cost"
                };
                return new UsageSettlementRequest {
                    OperationId = id, UserId = user, ConsumingService = "WEB4", Provider = receipt.Provider,
                    ProviderRequestId = receipt.ProviderRequestId, Outcome = failed ? "failed" : "succeeded", Units = receipt.Units,
                    ActualCostUsd = 0m, CostSource = receipt.CostSource, PricingCatalogueVersion = receipt.PricingCatalogueVersion,
                    ProviderReceipts = new() { receipt }
                };
            }
            async Task AssertNoMutation(Func<Task> attempt, string code)
            {
                async Task<string> Snapshot(string name) => new BsonArray(await db.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson();
                var collections = (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                var before = new List<string>();
                foreach (var name in collections) before.Add(await Snapshot(name));
                var error = await Assert.ThrowsAnyAsync<Exception>(attempt);
                Assert.Equal(ns + "SubscriptionUsageConflictException", error.GetType().FullName);
                Assert.StartsWith(code, error.Message);
                Assert.Equal(collections, (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                for (var i = 0; i < collections.Length; i++) Assert.Equal(before[i], await Snapshot(collections[i]));
            }
            foreach (var layout in new[] { (Before: 1, After: 16), (Before: 2, After: 16), (Before: 16, After: 1) })
            {
                Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", "true");
                Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", layout.Before.ToString());
                var app = "owned-binding-" + layout.Before;
                await Call("CreateAppAccountAsync", app, "Owned binding", "bronze", "native-fixture",
                    "Verify immutable operation routing", true, "silver", CancellationToken.None);
                var original = "app:" + app + (layout.Before == 1 ? "" : "#1");
                var ids = new List<string>();
                for (var counter = 0; ids.Count < 4 && counter < 4096; counter++)
                {
                    var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(app + ":" + counter)).AsSpan(0, 16)).ToString("D");
                    if (Route(app, id, layout.Before) == original && Route(app, id, layout.After) != original) ids.Add(id);
                }
                Assert.Equal(4, ids.Count);
                var values = new[] { Event(app, ids[0], 0.02m), Event(app, ids[1], 0.03m),
                    Event(app, ids[2], 0.05m, true), Event(app, ids[3], 0.04m, true) };
                foreach (var value in values) await Call("AuthorizeAsync", value, 0, CancellationToken.None);
                foreach (var id in ids)
                {
                    var saved = await events.Find(new BsonDocument("OperationId", id)).SingleAsync();
                    Assert.Equal(layout.Before, saved["PoolShardCountAtAuthorization"].AsInt32);
                    var authorized = await audit.Find(new BsonDocument("_id", id + ":authorized")).SingleAsync();
                    using var payload = JsonDocument.Parse(authorized["PayloadJson"].AsString);
                    Assert.Equal(layout.Before, payload.RootElement.GetProperty("PoolShardCountAtAuthorization").GetInt32());
                }
                Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", layout.After.ToString());
                // Reconstruct the actual repository from the shipped configuration contract.
                // No old in-memory event/repository can supply the routing input after this point.
                repository = Activator.CreateInstance(repositoryType, [config])!;
                var replay = await Call("AuthorizeAsync", values[0], 0, CancellationToken.None);
                var replayEvent = replay!.GetType().GetField("Item1")!.GetValue(replay)!;
                Assert.Equal(layout.Before, eventType.GetProperty("PoolShardCountAtAuthorization")!.GetValue(replayEvent));
                await Call("UpdateAppAccountAsync", app, "silver", "disabled", "native-fixture",
                    "Existing reservations retain original accounting", null, null, CancellationToken.None);
                Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", "false");
                foreach (var id in ids.Take(3)) await Call("StartAsync", Start(id), CancellationToken.None);
                var success = await NativeWrite(ids[0]);
                var failed = await NativeWrite(ids[1], true);
                var late = await NativeWrite(ids[2]);
                await Call("SettleAsync", user, success, CancellationToken.None);
                await Call("SettleAsync", user, failed, CancellationToken.None);
                // An explicit owned ledger adjustment tests signed movements; it is not a provider fee.
                var correctionType = api.GetType(ns + "UsageCorrectionRequest", true)!;
                object Correction(decimal cost, string id)
                {
                    var value = Activator.CreateInstance(correctionType)!;
                    Set(value, "CorrectionKind", "billing-adjustment"); Set(value, "CorrectionId", id);
                    Set(value, "OperationId", ids[0]); Set(value, "Reason", "Owned ledger routing boundary adjustment");
                    Set(value, "EvidenceReference", "owned-native-accounting-test:" + app); Set(value, "CostDeltaUsd", cost);
                    return value;
                }
                var debit = Correction(0.001m, Guid.NewGuid().ToString("D"));
                await Call("CorrectAsync", "native-fixture", debit, CancellationToken.None);
                await Call("CorrectAsync", "native-fixture", debit, CancellationToken.None);
                foreach (var row in await buckets.Find(new BsonDocument("UserId", original)).ToListAsync())
                    Assert.Equal(0.001m, row["SettledCostUsd"].ToDecimal());
                await Call("CorrectAsync", "native-fixture", Correction(-0.001m, Guid.NewGuid().ToString("D")), CancellationToken.None);
                Assert.Equal(2, (int)(await Call("ExpireAsync", DateTime.UtcNow.AddHours(1), 16, CancellationToken.None))!);
                var unstarted = await events.Find(new BsonDocument("OperationId", ids[3])).SingleAsync();
                var unknown = await events.Find(new BsonDocument("OperationId", ids[2])).SingleAsync();
                Assert.Equal("expired", unstarted["Status"].AsString);
                Assert.Equal("recovery-required", unknown["Status"].AsString);
                foreach (var row in await buckets.Find(new BsonDocument("UserId", original)).ToListAsync())
                {
                    Assert.Equal(3L, row["Requests"].ToInt64());
                    Assert.Equal(0.05m, row["ReservedCostUsd"].ToDecimal());
                    Assert.Equal(10L, row["ReservedUnits"].ToInt64());
                }
                await Call("SettleAsync", user, late, CancellationToken.None);
                var auditCount = await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
                var settledReplay = await Call("SettleAsync", user, late, CancellationToken.None);
                Assert.True((bool)settledReplay!.GetType().GetField("Item3")!.GetValue(settledReplay)!);
                Assert.Equal(0, (int)(await Call("ExpireAsync", DateTime.UtcNow.AddHours(1), 16, CancellationToken.None))!);
                Assert.Equal(auditCount, await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                var rows = await buckets.Find(new BsonDocument("UserId", new BsonRegularExpression("^app:" + app + "(#|$)"))).ToListAsync();
                Assert.Equal(2, rows.Count);
                foreach (var row in rows)
                {
                    Assert.Equal(original, row["UserId"].AsString);
                    Assert.Equal(3L, row["Requests"].ToInt64());
                    Assert.Equal(0m, row["ReservedCostUsd"].ToDecimal()); Assert.Equal(0m, row["SettledCostUsd"].ToDecimal());
                    Assert.Equal(0L, row["ReservedUnits"].ToInt64());
                }
                Console.WriteLine($"Pool binding native verified: {layout.Before}->{layout.After}, original={original}, server-selected/audited binding, new repository, disabled/changed app, success/failure, signed adjustment/replay, unstarted expiry release and unknown-execution retention/late settlement; original month/day only, requests3, reserved/cost0. Local native Mongo acknowledgements, not external fee/Stripe acceptance.");
            }
            // The new server-owned field must not attach a personal reservation to an app pool.
            var personalId = Guid.NewGuid().ToString("D");
            var personal = Event("owned-unused-app", personalId, 0.01m);
            foreach (var property in new[] { "AppId", "AppPlanId", "PooledAppId", "PooledAppPlanId" }) Set(personal, property, null);
            await Call("AuthorizeAsync", personal, 0, CancellationToken.None);
            var personalRecord = await events.Find(new BsonDocument("OperationId", personalId)).SingleAsync();
            Assert.True(personalRecord["PoolShardCountAtAuthorization"].IsBsonNull);
            var poolRowsBeforePersonal = new BsonArray(await buckets.Find(new BsonDocument("UserId", new BsonRegularExpression("^app:")))
                .Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson();
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", "2");
            await Call("StartAsync", Start(personalId), CancellationToken.None);
            await Call("SettleAsync", user, await NativeWrite(personalId), CancellationToken.None);
            Assert.Equal(poolRowsBeforePersonal, new BsonArray(await buckets.Find(new BsonDocument("UserId", new BsonRegularExpression("^app:")))
                .Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson());
            Console.WriteLine("Pool binding native verified: personal reservation clears caller routing input and starts/settles with no pool bucket mutation.");
            // A missing/invalid persisted binding must never be guessed from the current setting.
            // This owned projection-fault fixture is not a claimed migration of historical production data.
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", "true");
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", "16");
            const string faultApp = "owned-binding-fault";
            await Call("CreateAppAccountAsync", faultApp, "Owned missing binding", "bronze", "native-fixture",
                "Verify missing binding refuses before execution", true, "silver", CancellationToken.None);
            var faultId = Guid.NewGuid().ToString("D");
            await Call("AuthorizeAsync", Event(faultApp, faultId, 0.01m), 0, CancellationToken.None);
            await events.UpdateOneAsync(new BsonDocument("OperationId", faultId), new BsonDocument("$unset", new BsonDocument("PoolShardCountAtAuthorization", "")));
            await AssertNoMutation(async () => await Call("StartAsync", Start(faultId), CancellationToken.None), "POOL_SHARD_BINDING_REQUIRED:");
            await AssertNoMutation(async () => await Call("ExpireAsync", DateTime.UtcNow.AddDays(1), 16, CancellationToken.None), "POOL_SHARD_BINDING_REQUIRED:");
            foreach (var invalid in new[] { 0, 17 })
            {
                await events.UpdateOneAsync(new BsonDocument("OperationId", faultId), new BsonDocument("$set", new BsonDocument("PoolShardCountAtAuthorization", invalid)));
                await AssertNoMutation(async () => await Call("StartAsync", Start(faultId), CancellationToken.None), "POOL_SHARD_BINDING_REQUIRED:");
            }
            await events.UpdateOneAsync(new BsonDocument("OperationId", faultId), new BsonDocument("$set", new BsonDocument {
                { "Status", "executing" }, { "PoolShardCountAtAuthorization", BsonNull.Value } }));
            var faultReceipt = await NativeWrite(faultId);
            await AssertNoMutation(async () => await Call("SettleAsync", user, faultReceipt, CancellationToken.None), "POOL_SHARD_BINDING_REQUIRED:");
            await events.UpdateOneAsync(new BsonDocument("OperationId", faultId), new BsonDocument("$set", new BsonDocument {
                { "Status", "authorized" }, { "PoolShardCountAtAuthorization", 16 }, { "AppId", "owned-other-app" } }));
            await AssertNoMutation(async () => await Call("StartAsync", Start(faultId), CancellationToken.None), "POOL_SHARD_BINDING_INVALID:");
            Console.WriteLine("Pool binding native verified: missing/zero/out-of-range/mismatched projection refuses execution; missing-binding expiry/settlement rolls back all records/audits/receipts/buckets. Historical binding migration and monetary app charging remain OPEN.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", oldPool);
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", oldShards);
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_pool_binding_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned pool binding database absence and process settings restoration verified.");
        }
    }
}
