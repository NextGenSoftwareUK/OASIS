using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.KeyDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class KeyDBCqlNativeTests
{
    private const string PrivateMarker = "cosmic-keydb-native-private-evidence";
    private sealed record Seed(Guid Actor, Guid[] Ids, Guid DeletedId);

    internal static ConfigurationOptions Options() => new()
    {
        EndPoints = { "127.0.0.1:16390" },
        Password = Environment.GetEnvironmentVariable("OASIS_KEYDB_NATIVE_PASSWORD")
            ?? throw new InvalidOperationException("Original protected native KeyDB credential required."),
        DefaultDatabase = 0, AllowAdmin = true, AbortOnConnectFail = true, ConnectRetry = 0,
        BacklogPolicy = BacklogPolicy.FailFast, ConnectTimeout = 3000, SyncTimeout = 3000, AsyncTimeout = 3000
    };

    internal static KeyDBOASIS Create() => new(Options().ToString(includePassword: true),
        () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(KeyDBOASIS.StorageKeyEnvironmentVariable));

    private static async Task<string[]> Snapshot(ConnectionMultiplexer native)
    {
        var values = new List<string>();
        var server = native.GetServer("127.0.0.1", 16390);
        var info = await server.InfoAsync("server");
        var fields = info.SelectMany(group => group).ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.AreEqual("6.3.4", fields["redis_version"]);
        Assert.AreEqual("/var/tmp/oasis-runtimes/keydb-6.3.4/src/keydb-server", fields["executable"]);
        var state = Environment.GetEnvironmentVariable("OASIS_KEYDB_CQL_STATE")!;
        using var owned = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "active-process.json")));
        Assert.AreEqual(owned.RootElement.GetProperty("pid").GetInt32().ToString(), fields["process_id"],
            "Actual source must be the hash-admitted native KeyDB process, not another compatible server.");
        await foreach (var key in server.KeysAsync(0, pageSize: 100))
        {
            Assert.IsTrue(values.Count < 100, "Source integrity inspection must remain bounded.");
            var dump = await native.GetDatabase(0).KeyDumpAsync(key);
            Assert.IsNotNull(dump);
            Assert.IsFalse(key.ToString().Contains(PrivateMarker));
            Assert.IsFalse(Encoding.UTF8.GetString(dump).Contains(PrivateMarker), "Private content must be encrypted natively.");
            values.Add(key + "|" + Convert.ToHexString(SHA256.HashData(dump)));
        }
        Assert.IsTrue(values.Count > 0, "Original encrypted source must remain present.");
        return values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    internal static async Task AssertSourceUnchanged(string state)
    {
        using var native = await ConnectionMultiplexer.ConnectAsync(Options());
        var expected = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(Path.Combine(state, "source-snapshot.json")))!;
        CollectionAssert.AreEqual(expected, await Snapshot(native), "Preserve original native ciphertext across queries/restart.");
    }

    [TestMethod]
    public async Task NativeEncryptedKeyDBFiltersPagesAndPreservesOriginalCiphertext()
    {
        Assert.IsTrue(OperatingSystem.IsLinux());
        var state = Environment.GetEnvironmentVariable("OASIS_KEYDB_CQL_STATE")!;
        Assert.AreEqual("/var/tmp/oasis-keydb-cql", state);
        Assert.IsTrue(File.Exists(Path.Combine(state, "active-process.json")), "Explicit owned native runner required.");
        using var sourceLease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain native writes; never replay them.");
        using var native = await ConnectionMultiplexer.ConnectAsync(Options());
        using var provider = Create();
        Seed seed;

        // Create the synthetic source exactly once, only in an empty dedicated database.
        if (!File.Exists(manifest))
        {
            Assert.AreEqual(0L, await native.GetServer("127.0.0.1", 16390).DatabaseSizeAsync(0));
            seed = new(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), Guid.NewGuid());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message + active.Exception);
            var names = new[] { "Espresso", "Latte", "Tea", "Deleted source" };
            var ids = seed.Ids.Append(seed.DeletedId).ToArray();
            for (var i = 0; i < ids.Length; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon
                {
                    Id = ids[i], Name = names[i], Description = PrivateMarker,
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true
                }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved); Assert.AreEqual(1, saved.SavedCount);
                using var ack = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(ack, new { id = ids[i], acknowledged = true }); ack.Flush(true);
            }
            var deleted = await provider.DeleteHolonSoftAsync(seed.DeletedId);
            Assert.IsFalse(deleted.IsError, deleted.Message + deleted.Exception); Assert.IsTrue(deleted.Result);
            using (var ack = new FileStream(Path.Combine(state, "source-delete.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(ack, new { id = seed.DeletedId, acknowledged = true }); ack.Flush(true); }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.IsTrue(await native.GetServer("127.0.0.1", 16390).DatabaseSizeAsync(0) > 0,
                "Missing original source must not be reseeded.");
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message + active.Exception);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        Assert.IsFalse(seed.Ids.Contains(seed.DeletedId));
        var before = await Snapshot(native);
        var baseline = Path.Combine(state, "source-snapshot.json");
        if (File.Exists(baseline))
            CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(baseline))!, before);
        else
        {
            using var file = new FileStream(baseline, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, before); file.Flush(true);
        }

        CosmicQueryPlan<Holon> Plan(Guid[] ids, bool page = false)
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.KeyDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.KeyDBOASIS, ids).Result).Result
                .Where(row => ids.Contains(row.Id)).Result;
            if (page) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.KeyDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.KeyDBOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
        }

        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() }, new[] { seed.DeletedId } })
        {
            var result = await provider.ExecuteCosmicQueryAsync(Plan(ids));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(row => row.Id).ToArray());
            foreach (var row in result.Result) Assert.AreEqual(PrivateMarker, row.Description);
        }
        var page = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids.Append(seed.DeletedId).ToArray(), true));
        Assert.IsFalse(page.IsError, page.Message + page.Exception);
        Assert.AreEqual(seed.Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First(), page.Result.Single().Id,
            "Paging applies after tombstone removal.");
        var budget = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", budget.ErrorCode); Assert.IsNull(budget.Result);
        CollectionAssert.AreEqual(before, await Snapshot(native));
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
