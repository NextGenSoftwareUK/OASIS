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
            var readCommands = new[] { "find", "listCollections", "getMore", "killCursors", "hello", "isMaster", "ismaster", "saslStart", "saslContinue", "endSessions" };
            Assert.All(commands.Keys, name => Assert.Contains(name, readCommands));
            var snapshot = new {
                observedAtUtc = DateTime.UtcNow, project = "712f50f6-3b7b-46d4-b229-44b49922f16b",
                environment = "dev", service = "WEB4 OASIS API", database = databaseName,
                openingMarkerPresent = opening, legacyOperationsPresent = legacy,
                accountingDataPresent = presence.Values.Any(value => value), collectionPresence = presence,
                openingGuardReady = opening && !legacy, mutationCommands = 0,
                observedCommands = commands.OrderBy(command => command.Key).ToDictionary(command => command.Key, command => command.Value),
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
