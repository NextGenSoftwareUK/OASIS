using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class RedisCqlNativeTests
{
    internal const int Database = 11;
    private const string PrivateMarker = "cosmic-redis-native-private-evidence";
    private sealed record Seed(Guid Actor, Guid[] Ids, int Database);
    internal static ConfigurationOptions Options() => new()
    {
        EndPoints = { "127.0.0.1:18679" }, Password = Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD")
            ?? throw new InvalidOperationException("Original protected Redis credential required."),
        DefaultDatabase = Database, AllowAdmin = true, AbortOnConnectFail = true, ConnectRetry = 0,
        BacklogPolicy = BacklogPolicy.FailFast, ConnectTimeout = 3000, SyncTimeout = 3000, AsyncTimeout = 3000
    };
    internal static RedisOASIS Create() => new(Options().ToString(includePassword: true),
        () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_REDIS_STORAGE_ENCRYPTION_KEY"), "cosmic-cql/");

    [TestMethod]
    public async Task NativeEncryptedRedisFiltersThreeToTwoAndPreservesEveryDump()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_REDIS_CQL_STATE")
            ?? throw new InvalidOperationException("Explicit Redis CQL native runner required.");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay it.");
        using var native = await ConnectionMultiplexer.ConnectAsync(Options());
        var database = native.GetDatabase(Database);
        var server = native.GetServer("127.0.0.1", 18679);
        using var provider = Create();
        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.AreEqual(0L, await server.DatabaseSizeAsync(Database), "Do not adopt or flush an occupied database.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), Database);
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message + active.Exception);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    Description = PrivateMarker, CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
                using var file = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(file, new { id = seed.Ids[i], acknowledged = true }); file.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.IsTrue(await server.DatabaseSizeAsync(Database) > 0, "Original source missing; never recreate it.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(Database, seed.Database);
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            var values = new List<string>();
            await foreach (var key in server.KeysAsync(Database, pageSize: 100))
            {
                Assert.IsTrue(values.Count < 100, "Bounded source integrity inspection required.");
                var dump = await database.KeyDumpAsync(key);
                Assert.IsNotNull(dump);
                Assert.IsFalse(key.ToString().Contains(PrivateMarker));
                Assert.IsFalse(Encoding.UTF8.GetString(dump).Contains(PrivateMarker), "Native Redis must not contain plaintext private record content.");
                values.Add(key + "|" + Convert.ToHexString(SHA256.HashData(dump)));
            }
            return values.OrderBy(v => v, StringComparer.Ordinal).ToArray();
        }
        var before = await Snapshot();
        var snapshotPath = Path.Combine(state, "source-snapshot.json");
        if (File.Exists(snapshotPath))
            CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(snapshotPath))!, before,
                "Original native ciphertext must survive provider/container restart unchanged.");
        else
        {
            using var file = new FileStream(snapshotPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, before); file.Flush(true);
        }
        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RedisOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.RedisOASIS,
                    ids.Length == 1 ? ids : seed.Ids).Result).Result.Where(h => ids.Contains(h.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.RedisOASIS,
                new[] { new CosmicQueryTarget(ProviderType.RedisOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(r => r.Id).ToArray());
            foreach (var row in result.Result) Assert.AreEqual(PrivateMarker, row.Description);
        }
        CollectionAssert.AreEqual(before, await Snapshot(), "CQL must preserve exact encrypted native keys/DUMP bytes.");
    }
}
