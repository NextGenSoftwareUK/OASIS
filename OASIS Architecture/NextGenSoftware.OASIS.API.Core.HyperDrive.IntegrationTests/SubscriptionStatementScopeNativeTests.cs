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
    public async Task OverageCollectionRejectsForeignScopeAndEmptyPayersBeforeMutation()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_statement_scope_" + Guid.NewGuid().ToString("N");
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
            var statementType = api.GetType(ns + "PaidOverageStatement", true)!;
            var invoiceType = api.GetType(ns + "PaidOverageInvoiceResult", true)!;
            var gatewayType = api.GetType(ns + "StripePaidOverageGateway", true)!;
            var gateway = Activator.CreateInstance(gatewayType, [config])!;
            var collectorType = api.GetType(ns + "PaidOverageBillingService", true)!;
            var logger = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>).MakeGenericType(collectorType)
                .GetField("Instance")!.GetValue(null)!;
            var collector = Activator.CreateInstance(collectorType, [repository, gateway, logger, TimeProvider.System])!;
            var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
            async Task<object?> Collect(object statement)
            {
                var task = (Task)collectorType.GetMethod("CollectAsync")!.Invoke(collector, [statement, CancellationToken.None])!;
                await task;
                return task.GetType().GetProperty("Result")!.GetValue(task);
            }
            BsonValue Bson(object? value) => value switch {
                null => BsonNull.Value, decimal number => new BsonDecimal128(number),
                DateTime date => new BsonDateTime(date), string[] values => new BsonArray(values), _ => BsonValue.Create(value)
            };
            async Task<(object Model, string Id)> Seed(string subscriber, string scope, string? app = null,
                string? payer = null, long cents = 0, string? owner = null)
            {
                var id = (app == null ? "ovg_" : "ovga_") + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                var model = Activator.CreateInstance(statementType)!;
                var fields = new Dictionary<string, object?> {
                    ["Id"] = id, ["UserId"] = subscriber, ["AppId"] = app, ["PayerAvatarId"] = payer,
                    ["Scope"] = scope, ["Month"] = month, ["StripeCustomerId"] = "owned-scope-input-customer",
                    ["StripeSubscriptionId"] = "owned-scope-input-subscription", ["AmountMinorUsd"] = cents,
                    ["RequestChargesUsd"] = cents / 100m, ["AiChargesUsd"] = 0m, ["State"] = "pending",
                    ["LeaseOwner"] = owner, ["LeaseUntilUtc"] = owner == null ? null : now.AddMinutes(10),
                    ["CreatedAtUtc"] = now, ["UpdatedAtUtc"] = now, ["NextAttemptAtUtc"] = now.AddMinutes(-1),
                    ["OperationIds"] = Array.Empty<string>(), ["AdjustmentIds"] = Array.Empty<string>(),
                    ["PricingVersions"] = Array.Empty<string>(), ["AppConsentIds"] = Array.Empty<string>(),
                    ["PersonalConsentIds"] = Array.Empty<string>(), ["Attempts"] = 0, ["PaymentFailed"] = false
                };
                var document = new BsonDocument();
                foreach (var item in fields) { Set(model, item.Key, item.Value); document.Add(item.Key == "Id" ? "_id" : item.Key, Bson(item.Value)); }
                await statements.InsertOneAsync(document);
                return (model, id);
            }
            async Task<string> State()
            {
                var rows = new List<string>();
                foreach (var name in (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x))
                    rows.Add(name + ":" + new BsonArray(await db.GetCollection<BsonDocument>(name)
                        .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson());
                return string.Join("|", rows);
            }
            async Task Refuse(Func<Task> action, string code)
            {
                var before = await State();
                var error = await Assert.ThrowsAnyAsync<Exception>(action);
                Assert.Contains(code, error.Message);
                Assert.Equal(before, await State());
            }
            // Existing-liability INPUTS only. Invalid rows must never acquire a collection lease or reach payment.
            var lease = Guid.NewGuid().ToString("D");
            var foreign = await Seed(user, "owned-other-financial-scope", cents: 100, owner: lease);
            var foreignApp = await Seed("app:owned-scope-app", "owned-other-financial-scope", "owned-scope-app", user, 100, lease);
            var emptySubscriber = await Seed(Guid.Empty.ToString("D"), databaseName);
            var emptyPayer = await Seed("app:owned-scope-app", databaseName, "owned-scope-app", Guid.Empty.ToString("D"));
            var wrongSubject = await Seed("app:owned-other-app", databaseName, "owned-scope-app", user);
            var valid = await Seed(user, databaseName);
            var ready = ((System.Collections.IEnumerable)(await Call("GetReadyAsync", now, CancellationToken.None))!).Cast<object>().ToArray();
            var recent = ((System.Collections.IEnumerable)(await Call("GetStatementsAsync", user, 50, CancellationToken.None))!).Cast<object>().ToArray();
            string Id(object value) => statementType.GetProperty("Id")!.GetValue(value)!.ToString()!;
            Assert.DoesNotContain(ready, x => Id(x) == foreign.Id || Id(x) == foreignApp.Id);
            Assert.Single(recent); Assert.Equal(valid.Id, Id(recent[0]));
            foreach (var invalid in new[] { foreign, foreignApp, emptySubscriber, emptyPayer, wrongSubject }) {
                var code = invalid.Id == foreign.Id || invalid.Id == foreignApp.Id
                    ? "OVERAGE_STATEMENT_SCOPE_MISMATCH" : "OVERAGE_STATEMENT_BINDING_REQUIRED";
                await Refuse(async () => await Call("ClaimAsync", invalid.Id, Guid.NewGuid().ToString("D"), now, CancellationToken.None), code);
                await Refuse(async () => await Collect(invalid.Model), code);
                var amount = invalid.Id == foreign.Id || invalid.Id == foreignApp.Id ? 100L : 0L;
                var invoice = Activator.CreateInstance(invoiceType, [null, "paid", null, amount, amount, false])!;
                await Refuse(async () => await Call("RecordInvoiceAsync", invalid.Id, null, invoice, null, null, CancellationToken.None), code);
            }
            await Refuse(async () => await Call("GetStatementAsync", foreign.Id, CancellationToken.None), "OVERAGE_STATEMENT_SCOPE_MISMATCH");
            await Refuse(async () => await Call("AttachInvoiceAsync", foreign.Id, lease, "in_owned_scope_input", CancellationToken.None), "billing lease");
            var beforeRetry = await State();
            await Call("RetryAsync", foreign.Id, lease, "owned-scope-input", CancellationToken.None);
            Assert.Equal(beforeRetry, await State());
            // The actual shipping gateway must also reject empty identities before its zero-due short circuit.
            foreach (var invalid in new[] { emptySubscriber, emptyPayer, wrongSubject }) {
                Func<string, Task> attach = id => Call("AttachInvoiceAsync", invalid.Id, lease, id, CancellationToken.None);
                await Refuse(async () => await (Task)gatewayType.GetMethod("CollectAsync")!.Invoke(gateway,
                    [invalid.Model, attach, CancellationToken.None])!, "OVERAGE_STATEMENT_BINDING_REQUIRED");
            }
            var results = await Task.WhenAll(Collect(valid.Model), Collect(valid.Model));
            Assert.All(results, value => Assert.NotNull(value));
            var stored = await statements.Find(new BsonDocument("_id", valid.Id)).SingleAsync();
            Assert.Equal("paid", stored["State"].AsString);
            Assert.Equal(1, stored["Attempts"].ToInt32());
            Assert.True(stored["StripeInvoiceId"].IsBsonNull);
            await Collect(valid.Model);
            Assert.Equal(1, (await statements.Find(new BsonDocument("_id", valid.Id)).SingleAsync())["Attempts"].ToInt32());
            var appAccounts = db.GetCollection<BsonDocument>("subscription_app_accounts");
            await appAccounts.InsertOneAsync(new BsonDocument { { "_id", "owned-scope-app" }, { "PlanId", "silver" },
                { "Status", "active" }, { "OwnerAvatarIds", new BsonArray { user } }, { "AuthorizationVersion", 0L } });
            var validApp = await Seed("app:owned-scope-app", databaseName, "owned-scope-app", user);
            await Task.WhenAll(Collect(validApp.Model), Collect(validApp.Model));
            await Collect(validApp.Model);
            var appStored = await statements.Find(new BsonDocument("_id", validApp.Id)).SingleAsync();
            Assert.Equal("paid", appStored["State"].AsString);
            Assert.Equal(1, appStored["Attempts"].ToInt32());
            var appRecord = await appAccounts.Find(new BsonDocument("_id", "owned-scope-app")).SingleAsync();
            Assert.False(appRecord["AppOveragePaymentBlocked"].AsBoolean);
            Assert.Equal(1L, appRecord["AuthorizationVersion"].ToInt64());
            Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(0L, await events.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal("pending", (await statements.Find(new BsonDocument("_id", foreign.Id)).SingleAsync())["State"].AsString);
            Assert.Equal("pending", (await statements.Find(new BsonDocument("_id", foreignApp.Id)).SingleAsync())["State"].AsString);
            Console.WriteLine("Statement collection scope native verified: current-scope discovery/self reads; foreign personal/app scopes and empty/mismatched payer identities refuse direct claims, actual collector and invoice recording without changing any collection; foreign attach/retry cannot mutate; shipping gateway rejects invalid identities before zero-due completion; two real collectors and paid replay commit each valid personal/app native zero-due statement with one attempt and no order/operation allocation; original app blocked-state version updated once. Existing statement INPUTS only, no external Stripe/payment/readiness, hosted or full portal acceptance.");
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= Resolve;
            Assert.StartsWith("subscription_statement_scope_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned statement scope database absence verified.");
        }
    }
}
