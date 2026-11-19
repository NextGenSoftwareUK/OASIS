using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task PooledAdmissionIsExactAcrossShardsAndRejectsStaleAppState()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_pool_" + Guid.NewGuid().ToString("N");
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
            var users = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid().ToString("D")).ToArray();
            foreach (var user in users)
            {
                var record = Activator.CreateInstance(recordType)!;
                Set(record, "UserId", user); Set(record, "PlanId", "free"); Set(record, "Status", "active");
                Set(record, "CurrentPeriodEnd", DateTime.UtcNow.AddYears(1));
                await Call("SaveSubscriptionAsync", record, CancellationToken.None);
            }
            var policyProvider = Activator.CreateInstance(api.GetType(ns + "SubscriptionUsagePolicyProvider", true)!)!;
            var policy = policyProvider.GetType().GetMethod("GetPolicy")!.Invoke(policyProvider, ["bronze", 0])!;
            var limit = (int)policy.GetType().GetProperty("MonthlyRequestLimit")!.GetValue(policy)!;
            var db = mongo.GetDatabase(databaseName);
            var buckets = db.GetCollection<BsonDocument>("subscription_usage_buckets");
            var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
            var events = db.GetCollection<BsonDocument>("subscription_usage_events");
            var audit = db.GetCollection<BsonDocument>("subscription_usage_audit");
            object Event(string app, string user, string id)
            {
                var value = Activator.CreateInstance(eventType)!;
                var now = DateTime.UtcNow;
                foreach (var item in new Dictionary<string, object> {
                    ["OperationId"] = id, ["UserId"] = user, ["AppId"] = app, ["AppPlanId"] = "silver",
                    ["PooledAppId"] = app, ["PooledAppPlanId"] = "bronze", ["ConsumingService"] = "WEB4",
                    ["Endpoint"] = "/owned-pool-admission", ["MeterCategory"] = "api.request", ["Status"] = "authorized",
                    ["RequestFingerprint"] = new string('a', 64), ["AuthorizedAtUtc"] = now, ["ExpiresAtUtc"] = now.AddMinutes(15)
                }) Set(value, item.Key, item.Value);
                return value;
            }
            async Task Refused(string code, object value)
            {
                var error = await Assert.ThrowsAnyAsync<Exception>(async () => await Call("AuthorizeAsync", value, 0, CancellationToken.None));
                Assert.Equal(ns + "SubscriptionUsageLimitException", error.GetType().FullName);
                Assert.Equal(code, error.GetType().GetProperty("Code")!.GetValue(error));
            }
            foreach (var shards in new[] { 1, 2, 16 })
            {
                Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", shards.ToString());
                var app = "owned-pool-" + shards;
                await Call("CreateAppAccountAsync", app, "Owned exact admission", "bronze", "native-fixture",
                    "Verify exact concurrent quota admission", true, "silver", CancellationToken.None);
                // Seed a documented read boundary, not fabricated execution or payment evidence.
                // Even the one-shard writer must read all historical shards.
                var now = DateTime.UtcNow;
                var month = now.ToString("yyyy-MM");
                for (var shard = 0; shard < 16; shard++)
                {
                    var subject = "app:" + app + (shard == 0 ? "" : "#" + shard);
                    await buckets.InsertOneAsync(new BsonDocument {
                        { "_id", subject + ":month:" + month }, { "UserId", subject }, { "PeriodType", "month" }, { "Period", month },
                        { "Requests", (limit - 1) / 16 + (shard < (limit - 1) % 16 ? 1 : 0) }, { "UpdatedAtUtc", now }
                    });
                }
                var ids = new List<string>();
                var targets = new HashSet<string>();
                while (ids.Count < Math.Min(shards, 8))
                {
                    var id = Guid.NewGuid().ToString("D");
                    var subject = (string)repositoryType.GetMethod("PoolWriteSubject")!.Invoke(null, [app, id, shards])!;
                    if (targets.Add(subject)) ids.Add(id);
                }
                while (ids.Count < 8) ids.Add(Guid.NewGuid().ToString("D"));
                var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var started = System.Diagnostics.Stopwatch.StartNew();
                var values = ids.Select((id, i) => Event(app, users[i], id)).ToArray();
                var writers = values.Select(async value => {
                    await gate.Task;
                    try { await Call("AuthorizeAsync", value, 0, CancellationToken.None); return true; }
                    catch (Exception ex) when (ex.GetType().FullName == ns + "SubscriptionUsageLimitException") {
                        Assert.Equal("APP_MONTHLY_REQUEST_LIMIT_EXCEEDED", ex.GetType().GetProperty("Code")!.GetValue(ex));
                        return false;
                    }
                }).ToArray();
                gate.SetResult(true);
                var accepted = await Task.WhenAll(writers);
                Assert.Equal(1, accepted.Count(x => x));
                var admitted = Array.FindIndex(accepted, x => x);
                Assert.Equal(shards, int.Parse(Environment.GetEnvironmentVariable("OASIS_POOL_SHARDS")!));
                Assert.Equal(1L, await events.CountDocumentsAsync(new BsonDocument("PooledAppId", app)));
                var stored = await apps.Find(new BsonDocument("_id", app)).SingleAsync();
                Assert.Equal(1L, stored["AuthorizationVersion"].ToInt64());
                var poolRows = await buckets.Find(new BsonDocument { { "PeriodType", "month" },
                    { "UserId", new BsonRegularExpression("^app:" + app + "(#|$)") } }).ToListAsync();
                Assert.Equal((long)limit, poolRows.Sum(x => x["Requests"].ToInt64()));
                Assert.Equal(shards == 16 ? 8 : shards, targets.Count);
                // Replaying a committed reservation cannot take another quota slot or app fence.
                var replay = await Call("AuthorizeAsync", values[admitted], 0, CancellationToken.None);
                var replayEvent = replay!.GetType().GetField("Item1")!.GetValue(replay)!;
                Assert.True((bool)eventType.GetProperty("AlreadyAuthorized")!.GetValue(replayEvent)!);
                var foreignReplay = await Assert.ThrowsAnyAsync<Exception>(async () => await Call("AuthorizeAsync",
                    Event("owned-other-app", users[admitted], ids[admitted]), 0, CancellationToken.None));
                Assert.Equal(ns + "SubscriptionUsageConflictException", foreignReplay.GetType().FullName);
                var beforeEvents = await events.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
                var beforeAudit = await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
                await apps.UpdateOneAsync(new BsonDocument("_id", app), new BsonDocument("$set", new BsonDocument("Status", "disabled")));
                await Refused("APP_ACCOUNT_INACTIVE", Event(app, users[0], Guid.NewGuid().ToString("D")));
                await apps.UpdateOneAsync(new BsonDocument("_id", app), new BsonDocument("$set", new BsonDocument { { "Status", "active" }, { "PlanId", "silver" } }));
                await Refused("APP_ACCOUNT_CHANGED", Event(app, users[0], Guid.NewGuid().ToString("D")));
                await apps.UpdateOneAsync(new BsonDocument("_id", app), new BsonDocument("$set", new BsonDocument { { "PlanId", "bronze" }, { "PerUserPlanId", "gold" } }));
                await Refused("APP_ACCOUNT_CHANGED", Event(app, users[0], Guid.NewGuid().ToString("D")));
                await apps.UpdateOneAsync(new BsonDocument("_id", app), new BsonDocument("$set", new BsonDocument("PerUserPlanId", "silver")));
                Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", "false");
                await Refused("APP_POOL_NOT_ACTIVE", Event(app, users[0], Guid.NewGuid().ToString("D")));
                Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", "true");
                await Refused("APP_ACCOUNT_NOT_FOUND", Event("owned-missing-pool", users[0], Guid.NewGuid().ToString("D")));
                Assert.Equal(beforeEvents, await events.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                Assert.Equal(beforeAudit, await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                stored = await apps.Find(new BsonDocument("_id", app)).SingleAsync();
                Assert.Equal(1L, stored["AuthorizationVersion"].ToInt64());
                Console.WriteLine($"Pool admission native verified: shards={shards}, concurrent=8, admitted=1, refused=7, aggregate={limit}, appFence=1; replay and stale/inactive/missing/flag refusals do not write usage; elapsedMs={started.ElapsedMilliseconds} (isolated correctness case, not throughput benchmark).");
            }
            // App overage work must retain the ordinary per-user monthly/daily/token/budget limits.
            // Use actual policy values and native boundary buckets; no substituted policy or provider.
            const string guardApp = "owned-pool-guards";
            await Call("CreateAppAccountAsync", guardApp, "Owned admission guards", "bronze", "native-fixture",
                "Verify independent per-user restrictions", true, "silver", CancellationToken.None);
            var personalPolicy = policyProvider.GetType().GetMethod("GetPolicy")!.Invoke(policyProvider, ["silver", 0])!;
            long Limit(string name) => Convert.ToInt64(personalPolicy.GetType().GetProperty(name)!.GetValue(personalPolicy));
            var beforeGuardsEvents = await events.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            var beforeGuardsAudit = await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            foreach (var code in new[] { "MONTHLY_REQUEST_LIMIT_EXCEEDED", "DAILY_CALL_LIMIT_EXCEEDED", "DAILY_TOKEN_LIMIT_EXCEEDED", "MONTHLY_BUDGET_EXCEEDED" })
            {
                var actor = Guid.NewGuid().ToString("D");
                var record = Activator.CreateInstance(recordType)!;
                Set(record, "UserId", actor); Set(record, "PlanId", "free"); Set(record, "Status", "active");
                Set(record, "CurrentPeriodEnd", DateTime.UtcNow.AddYears(1));
                await Call("SaveSubscriptionAsync", record, CancellationToken.None);
                foreach (var type in new[] { "month", "day" })
                {
                    var period = DateTime.UtcNow.ToString(type == "month" ? "yyyy-MM" : "yyyy-MM-dd");
                    await buckets.InsertOneAsync(new BsonDocument {
                        { "_id", actor + ":" + type + ":" + period }, { "UserId", actor }, { "PeriodType", type }, { "Period", period },
                        { "Requests", type == "month" && code == "MONTHLY_REQUEST_LIMIT_EXCEEDED" ? Limit("MonthlyRequestLimit") :
                            type == "day" && code == "DAILY_CALL_LIMIT_EXCEEDED" ? Limit("DailyCallLimit") : 0 },
                        { "SettledUnits", type == "day" && code == "DAILY_TOKEN_LIMIT_EXCEEDED" ? Limit("DailyTokenLimit") : 0 },
                        { "SettledCostUsd", new BsonDecimal128(type == "month" && code == "MONTHLY_BUDGET_EXCEEDED" ?
                            (decimal)personalPolicy.GetType().GetProperty("MonthlyBudgetUsd")!.GetValue(personalPolicy)! : 0m) },
                        { "UpdatedAtUtc", DateTime.UtcNow }
                    });
                }
                var value = Event(guardApp, actor, Guid.NewGuid().ToString("D"));
                if (code == "DAILY_TOKEN_LIMIT_EXCEEDED") { Set(value, "MeterCategory", "ai.tokens"); Set(value, "RequestedUnits", 1L); }
                if (code == "MONTHLY_BUDGET_EXCEEDED") Set(value, "ReservedCostUsd", 1m);
                await Refused(code, value);
            }
            Assert.Equal(beforeGuardsEvents, await events.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(beforeGuardsAudit, await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var guardAccount = await apps.Find(new BsonDocument("_id", guardApp)).SingleAsync();
            Assert.Equal(0L, guardAccount["AuthorizationVersion"].ToInt64());
            Console.WriteLine("Pool admission native verified: independent per-user monthly/daily/token/budget refusals preserve events/audit and app authorization version; no app charging or Stripe acceptance.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", oldPool);
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", oldShards);
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_pool_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned pool fixture absence and process settings restoration verified.");
        }
    }
}
