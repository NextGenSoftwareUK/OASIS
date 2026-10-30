using System.Text.Json;
using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

/// <summary>Read-only reconciliation of the retained, acknowledged demo cohort; never creates providers or replays writes.</summary>
public sealed class SandboxHostedReplicationProbe
{
    [SandboxHostedAccountingProbeFact]
    public async Task ObserveRetainedSourceAndDeliveryObligations()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root,
            "LocalState/COSMIC/HostedSandboxCqlCurrent/fixture.json")));
        var records = fixture.RootElement.GetProperty("records").EnumerateArray().ToArray();
        Assert.Equal(3, records.Length);
        var ids = records.Select(row => Guid.Parse(row.GetProperty("id").GetString()!).ToString("D")).ToArray();
        var operations = records.Select(row => Guid.Parse(row.GetProperty("operationId").GetString()!).ToString("D")).ToArray();
        var actor = Guid.Parse(fixture.RootElement.GetProperty("avatars")[0].GetProperty("id").GetString()!).ToString("D");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            var settings = MongoClientSettings.FromConnectionString(Environment.GetEnvironmentVariable("COSMIC_HOSTED_ACCOUNTING_URI"));
            settings.ApplicationName = "cosmic-cql-read-only-replication";
            settings.MaxConnectionPoolSize = 2;
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
            settings.SocketTimeout = TimeSpan.FromSeconds(15);
            var commands = new ConcurrentDictionary<string, int>();
            settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
                commands.AddOrUpdate(command.CommandName, 1, (_, count) => count + 1));
            var database = new MongoClient(settings).GetDatabase(Environment.GetEnvironmentVariable("COSMIC_HOSTED_ACCOUNTING_DATABASE"));

            // Exact retained identifiers and minimal projections: no payloads, keys, tokens or unrelated records.
            var sources = await database.GetCollection<BsonDocument>("Holon")
                .Find(new BsonDocument("HolonId", new BsonDocument("$in", new BsonArray(ids))))
                .Project(new BsonDocument { { "_id", 0 }, { "HolonId", 1 }, { "CreatedByAvatarId", 1 } })
                .Limit(4).ToListAsync(deadline.Token);
            Assert.True(sources.Count <= 3);
            var obligations = await database.GetCollection<BsonDocument>("CosmicHolonOutbox")
                .Find(new BsonDocument("OperationId", new BsonDocument("$in", new BsonArray(operations))))
                .Project(new BsonDocument { { "_id", 0 }, { "OperationId", 1 }, { "TargetId", 1 },
                    { "DeliveryState", 1 }, { "AttemptCount", 1 }, { "LastErrorCode", 1 } })
                .Limit(31).ToListAsync(deadline.Token);
            Assert.True(obligations.Count <= 30);
            // Ordered delivery may be held by an earlier obligation. Observe one queue head,
            // never its payload or recovery evidence, and never bypass its ordering barrier.
            var sqliteHead = await database.GetCollection<BsonDocument>("CosmicHolonOutbox")
                .Find(new BsonDocument { { "TargetId", "hyperdrive-target:SQLLiteDBOASIS" },
                    { "DeliveryState", new BsonDocument("$ne", "Completed") } })
                .Sort(new BsonDocument("Sequence", 1))
                .Project(new BsonDocument { { "_id", 0 }, { "Sequence", 1 }, { "OperationId", 1 },
                    { "DeliveryState", 1 }, { "AttemptCount", 1 }, { "LastErrorCode", 1 } })
                .Limit(1).SingleOrDefaultAsync(deadline.Token);
            var allowed = new[] { "find", "getMore", "killCursors", "hello", "isMaster", "ismaster", "saslStart", "saslContinue", "endSessions" };
            Assert.All(commands.Keys, name => Assert.Contains(name, allowed));
            var report = new
            {
                observedAtUtc = DateTime.UtcNow,
                sourceRecords = sources.Select(row => new { id = row["HolonId"].AsString,
                    ownedByFixture = row.GetValue("CreatedByAvatarId", BsonNull.Value) == new BsonString(actor) }),
                obligations = obligations.Select(row => new { operationId = row["OperationId"].AsString,
                    targetId = row["TargetId"].AsString, state = row["DeliveryState"].AsString,
                    attempts = row["AttemptCount"].ToInt32(), errorCode = row["LastErrorCode"].IsBsonNull ? null : row["LastErrorCode"].AsString }),
                mutationCommands = 0, observedCommands = commands.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value),
                sqliteQueueHead = sqliteHead == null ? null : new { sequence = sqliteHead["Sequence"].ToInt64(),
                    operationId = sqliteHead["OperationId"].AsString, state = sqliteHead["DeliveryState"].AsString,
                    attempts = sqliteHead["AttemptCount"].ToInt32(),
                    errorCode = sqliteHead["LastErrorCode"].IsBsonNull ? null : sqliteHead["LastErrorCode"].AsString },
                scope = "Source/outbox observation only; does not prove destination contents or query acceptance."
            };
            await File.WriteAllTextAsync(Path.Combine(root, "TestResults/Web4SandboxCqlCurrent/hosted-replication-readiness.json"),
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n", deadline.Token);
        }
        catch (Exception error)
        {
            throw new InvalidOperationException("Read-only replication observation failed: " + error.GetType().Name + ". No mutation attempted.");
        }
    }
}
