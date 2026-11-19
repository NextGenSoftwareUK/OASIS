using System.Text.Json;
using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

/// <summary>Explicit operator read-only probe; passing means observations were captured, not that admission is ready.</summary>
public sealed class SandboxHostedAccountingProbeFactAttribute : FactAttribute
{
    public SandboxHostedAccountingProbeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_HOSTED_ACCOUNTING_PROBE") != "oasis-dev-web4-read-only")
            Skip = "Explicit dev WEB4 read-only accounting inspection is required.";
    }
}

public class SandboxHostedAccountingReadinessProbe
{
    [SandboxHostedAccountingProbeFact]
    public async Task ObserveOpeningStateWithoutInitializingOrChangingTheLedger()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        Assert.True(File.Exists(Path.Combine(root, "Scripts", "Sandbox", "cql-free-admission.json")));
        var results = Path.Combine(root, "TestResults", "Web4SandboxCqlCurrent");
        Directory.CreateDirectory(results);
        var report = Path.Combine(results, "hosted-accounting-readiness.json");
        var uri = Environment.GetEnvironmentVariable("COSMIC_HOSTED_ACCOUNTING_URI");
        var databaseName = Environment.GetEnvironmentVariable("COSMIC_HOSTED_ACCOUNTING_DATABASE");
        Assert.False(string.IsNullOrWhiteSpace(uri));
        Assert.False(string.IsNullOrWhiteSpace(databaseName));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try {
            var settings = MongoClientSettings.FromConnectionString(uri);
            settings.ApplicationName = "cosmic-cql-read-only-readiness";
            settings.MaxConnectionPoolSize = 2;
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
            settings.ConnectTimeout = TimeSpan.FromSeconds(10);
            settings.SocketTimeout = TimeSpan.FromSeconds(15);
            var commands = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
            settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
                commands.AddOrUpdate(command.CommandName, 1, (_, count) => count + 1));
            var database = new MongoClient(settings).GetDatabase(databaseName);
            // No repository construction: its index initialization would mutate the host.
            using var listed = await database.ListCollectionNamesAsync(cancellationToken: deadline.Token);
            var names = await listed.ToListAsync(deadline.Token);
            Assert.True(names.Count <= 1000, "Accounting namespace inventory exceeds this probe's scope.");
            var presence = new SortedDictionary<string, bool>(StringComparer.Ordinal);
            foreach (var name in names.Where(name => name.StartsWith("subscription_", StringComparison.Ordinal)))
                presence[name] = await database.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Limit(1).AnyAsync(deadline.Token);
            var opening = await database.GetCollection<BsonDocument>("subscription_usage_migrations")
                .Find(new BsonDocument("_id", "v1-opening-balance")).Limit(1).AnyAsync(deadline.Token);
            var legacy = await database.GetCollection<BsonDocument>("subscription_usage_events")
                .Find(new BsonDocument("RequestFingerprint", BsonNull.Value)).Limit(1).AnyAsync(deadline.Token);
            object? providerSchema = null;
            var providerUri = Environment.GetEnvironmentVariable("COSMIC_HOSTED_PROVIDER_URI");
            if (!string.IsNullOrEmpty(providerUri)) {
                var providerSettings = MongoClientSettings.FromConnectionString(providerUri);
                providerSettings.MaxConnectionPoolSize = 2;
                providerSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
                providerSettings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
                    commands.AddOrUpdate(command.CommandName, 1, (_, count) => count + 1));
                var providerClient = new MongoClient(providerSettings);
                var providerDatabase = Environment.GetEnvironmentVariable("COSMIC_HOSTED_PROVIDER_DATABASE");
                Assert.False(string.IsNullOrWhiteSpace(providerDatabase));
                var holons = providerClient.GetDatabase(providerDatabase).GetCollection<BsonDocument>("Holon");
                var sample = await holons.Aggregate<BsonDocument>(new BsonDocument[] {
                    new("$limit", 10000), new("$group", new BsonDocument {
                        { "_id", new BsonDocument { { "idType", new BsonDocument("$type", "$HolonId") },
                            { "schema", new BsonDocument("$ifNull", new BsonArray { "$CosmicCqlMembership.schema", -1 }) } } },
                        { "count", new BsonDocument("$sum", 1) }
                    })
                }, new AggregateOptions { MaxTime = TimeSpan.FromSeconds(15), AllowDiskUse = false }, deadline.Token).ToListAsync(deadline.Token);
                string guard; string? exceptionType = null; int? nativeCode = null; string? nativeCodeName = null;
                try {
                    // Diagnostic metadata check only: invoke the production guard, never a substitute query executor.
                    var membership = typeof(NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS).Assembly
                        .GetType("NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoCqlMembership", throwOnError: true)!;
                    var valid = (BsonValue)membership.GetMethod("ValidObservation", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, null)!;
                    using var session = await providerClient.StartSessionAsync(new ClientSessionOptions { Snapshot = true, CausalConsistency = false }, deadline.Token);
                    var invalid = await holons.Aggregate<BsonDocument>(session, new BsonDocument[] {
                        new("$match", new BsonDocument("$expr", new BsonDocument("$not", new BsonArray { valid }))),
                        new("$limit", 1), new("$project", new BsonDocument("_id", 1))
                    }, new AggregateOptions { Collation = Collation.Simple, MaxTime = TimeSpan.FromSeconds(15), AllowDiskUse = false }, deadline.Token).AnyAsync(deadline.Token);
                    guard = invalid ? "invalid-membership-present" : "membership-guard-valid";
                }
                catch (Exception error) {
                    guard = "native-guard-error"; exceptionType = error.GetType().FullName;
                    if (error is MongoCommandException mongo) { nativeCode = mongo.Code; nativeCodeName = mongo.CodeName; }
                }
                object? inheritedPlanner;
                var actor = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Diagnostic template identity, never used to return business rows.
                var access = NextGenSoftware.OASIS.API.Core.Queries.CosmicQueryReadAccess.OwnerPublicOrShared(actor, Array.Empty<Guid>(), true).Result;
                var query = NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlV1Compiler.Compile(
                    new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Request { Provider = "MongoDBOASIS", Take = 3 }, actor,
                    NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS, access);
                var translatorType = typeof(NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS).Assembly
                    .GetType("NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoCqlTranslator", true)!;
                var translator = Activator.CreateInstance(translatorType, nonPublic: true);
                var command = translatorType.GetMethod("CompileExecution", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .MakeGenericMethod(typeof(NextGenSoftware.OASIS.API.Core.Holons.Holon)).Invoke(translator, new object?[] { query, 3, null })!;
                var stages = (BsonDocument[])command.GetType().GetProperty("Pipeline", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(command)!;
                try {
                    // queryPlanner validates the production compiler's bounded template, without executing or returning business rows.
                    await providerClient.GetDatabase(providerDatabase).RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "explain", new BsonDocument { { "aggregate", "Holon" }, { "pipeline", new BsonArray(stages) },
                            { "cursor", new BsonDocument() }, { "collation", new BsonDocument("locale", "simple") },
                            { "allowDiskUse", false }, { "maxTimeMS", 15000 } } }, { "verbosity", "queryPlanner" }
                    }, cancellationToken: deadline.Token);
                    inheritedPlanner = new { stageCount = stages.Length, accepted = true };
                }
                catch (MongoCommandException error) {
                    // The template has no caller filter values. Retain only its bounded server error, never the command/result envelope.
                    var message = error.Result.GetValue("errmsg", "").AsString;
                    inheritedPlanner = new { stageCount = stages.Length, accepted = false, code = error.Code,
                        codeName = error.CodeName, message = message[..Math.Min(512, message.Length)] };
                }
                providerSchema = new { database = providerDatabase, collection = "Holon", sampledRowsLimit = 10000,
                    schemaGroups = sample.Select(value => new { idType = value["_id"]["idType"].AsString,
                        schema = value["_id"]["schema"].ToString(), count = value["count"].ToInt64() }).ToArray(),
                    guard, exceptionType, nativeCode, nativeCodeName, inheritedPlanner };
            }
            var readCommands = new[] { "find", "aggregate", "explain", "listCollections", "getMore", "killCursors", "hello", "isMaster", "ismaster", "saslStart", "saslContinue", "endSessions" };
            Assert.All(commands.Keys, name => Assert.Contains(name, readCommands));
            var snapshot = new {
                observedAtUtc = DateTime.UtcNow, project = "712f50f6-3b7b-46d4-b229-44b49922f16b",
                environment = "dev", service = "WEB4 OASIS API", database = databaseName,
                openingMarkerPresent = opening, legacyOperationsPresent = legacy,
                accountingDataPresent = presence.Values.Any(value => value), collectionPresence = presence,
                openingGuardReady = opening && !legacy, mutationCommands = 0,
                observedCommands = commands.OrderBy(command => command.Key).ToDictionary(command => command.Key, command => command.Value),
                providerSchema,
                scope = "Opening guard only; not a signed migration audit, subscription grant, deployment or query proof."
            };
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }) + "\n", deadline.Token);
        }
        catch (Exception error) {
            // Do not put a connection string, credential or financial record in a test failure/log.
            throw new InvalidOperationException("Read-only hosted accounting observation failed: " + error.GetType().Name + ". No initialization or recovery was attempted.");
        }
    }
}
