using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Fact]
    public async Task InspectOriginalReplicaLifecycleFailureWithoutMutatingStores()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var state = Path.Combine(root.FullName,"LocalState/COSMIC/OwnedRoutingCurrent/mongo-replica-lifecycle-failure.json");
        Assert.False(File.Exists(state), "Preserve the existing original failure observation.");
        var client = new MongoClient(uri);
        var snapshots = new BsonArray();
        foreach (var name in new[] { "cosmic_committed_replica_source_fixture", "cosmic_committed_replica_target_fixture" })
        {
            var database = client.GetDatabase(name);
            using var listed = await database.ListCollectionsAsync();
            var collections = (await listed.ToListAsync()).OrderBy(row => row["name"].AsString, StringComparer.Ordinal).ToArray();
            Assert.Contains(collections, row => row["name"] == "CosmicOwnedFixture");
            var captured = new BsonArray();
            foreach (var metadata in collections)
            {
                var collection = metadata["name"].AsString;
                var rows = await database.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).Limit(129).ToListAsync();
                Assert.True(rows.Count <= 128, "Preserve an unexpectedly large fixture instead of treating it as disposable.");
                captured.Add(new BsonDocument { { "metadata", metadata }, { "rows", new BsonArray(rows) } });
                output.WriteLine($"{name}/{collection}: {rows.Count} rows");
            }
            snapshots.Add(new BsonDocument { { "database", name }, { "collections", captured } });
        }
        var evidence = new BsonDocument { { "kind", "owned-replica-failpoint-setup-failure-v1" }, { "databases", snapshots } };
        using (var stream = new FileStream(state, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream)) { await writer.WriteAsync(evidence.ToJson()); await writer.FlushAsync(); stream.Flush(true); }
    }
    [Fact]
    public async Task ReconcileInspectedReplicaLifecycleSetupFailureWithoutReplayingWrites()
    {
        const string expectedDigest = "13BD5E08EA2260AF089B6FB86C752F89A8D648467055A25EA2707D3AE9A80C7E";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var path = Path.Combine(root.FullName,"LocalState/COSMIC/OwnedRoutingCurrent/mongo-replica-lifecycle-failure.json");
        var bytes = await File.ReadAllBytesAsync(path);
        Assert.Equal(expectedDigest, Convert.ToHexString(SHA256.HashData(bytes)));
        var original = BsonDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        Assert.Equal("owned-replica-failpoint-setup-failure-v1", original["kind"].AsString);
        var databases = original["databases"].AsBsonArray.Select(value => value.AsBsonDocument).ToArray();
        Assert.Equal(new[] { "cosmic_committed_replica_source_fixture", "cosmic_committed_replica_target_fixture" },
            databases.Select(value => value["database"].AsString));
        var client = new MongoClient(uri);
        // Verify every collection UUID/document before the first destructive action.
        // The hash pins the inspected original outcome, not merely a database name.
        for (var index = 0; index < databases.Length; index++)
        {
            var snapshot = databases[index]; var database = client.GetDatabase(snapshot["database"].AsString);
            var collections = snapshot["collections"].AsBsonArray.Select(value => value.AsBsonDocument).ToArray();
            using var listed = await database.ListCollectionsAsync();
            var current = (await listed.ToListAsync()).OrderBy(value => value["name"].AsString, StringComparer.Ordinal).ToArray();
            Assert.Equal(collections.Select(value => value["metadata"]["name"].AsString), current.Select(value => value["name"].AsString));
            foreach (var captured in collections)
            {
                var metadata = captured["metadata"].AsBsonDocument; var name = metadata["name"].AsString;
                Assert.Equal(metadata.ToJson(), current.Single(value => value["name"] == name).ToJson());
                var rows = await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(129).ToListAsync();
                string[] Sorted(System.Collections.Generic.IEnumerable<BsonDocument> values)
                    => values.OrderBy(value => value.GetValue("_id", BsonNull.Value).ToJson(), StringComparer.Ordinal).Select(value => value.ToJson()).ToArray();
                Assert.Equal(Sorted(captured["rows"].AsBsonArray.Select(value => value.AsBsonDocument)), Sorted(rows));
                var expected = name switch {
                    "Holon" => index == 0 ? 4 : 2,
                    "CosmicWriteReceipt" or "CosmicOperationFence" => index == 0 ? 3 : 2,
                    "CosmicReplicaHead" => 2,
                    "Avatar" or "AvatarDetail" or "CosmicHolonOutbox" or "CosmicOwnedFixture" => 0,
                    _ => throw new InvalidDataException("Unexpected collection; preserve the fixture.")
                };
                Assert.Equal(expected, rows.Count);
                if (name == "CosmicWriteReceipt")
                foreach (var row in rows)
                {
                    var envelope = JObject.Parse(row["ResultJson"].AsString);
                    Assert.Equal(row["_id"].AsString, envelope.Value<string>("operation"));
                    Assert.Equal(row["Fingerprint"].AsString, envelope.Value<string>("intent"));
                    Assert.NotEmpty(ConditionalHolonReceipt.Read(row["ResultJson"].AsString));
                }
            }
        }
        var journal = path + ".cleanup.json";
        Assert.False(File.Exists(journal), "Preserve an earlier cleanup outcome.");
        async Task Record(string phase) => await File.WriteAllTextAsync(journal,
            new BsonDocument { { "originalDigest", expectedDigest }, { "phase", phase } }.ToJson());
        await Record("verified-before-source-drop");
        await client.DropDatabaseAsync(databases[0]["database"].AsString);
        await Record("source-dropped-before-target-drop");
        await client.DropDatabaseAsync(databases[1]["database"].AsString);
        using var remaining = await client.ListDatabaseNamesAsync();
        var names = await remaining.ToListAsync();
        foreach (var snapshot in databases) Assert.DoesNotContain(snapshot["database"].AsString, names);
        await Record("completed");
        // Retain the inspected snapshot and completed cleanup receipt; never source-resave.
    }

}
