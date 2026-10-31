using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MemcachedOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class MemcachedCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, Guid DeletedId);
    private sealed record NativeValue(ulong Cas, string Hash);

    private static async Task<NativeValue?> Inspect(string key)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 11239, timeout.Token);
        await using var stream = client.GetStream();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var header = new byte[24]; header[0] = 0x80;
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(2), checked((ushort)keyBytes.Length));
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), (uint)keyBytes.Length);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(12), 1234);
        await stream.WriteAsync(header, timeout.Token);
        await stream.WriteAsync(keyBytes, timeout.Token);
        await stream.ReadExactlyAsync(header, timeout.Token);
        Assert.AreEqual((byte)0x81, header[0]); Assert.AreEqual((byte)0, header[1]);
        Assert.AreEqual(1234u, BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(12)));
        var status = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(6));
        var length = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8));
        Assert.IsTrue(length <= 2 * 1024 * 1024, "Bounded native inspection required.");
        var body = new byte[(int)length];
        await stream.ReadExactlyAsync(body, timeout.Token);
        if (status == 1) return null; // Native KEY_ENOENT, not a transport failure.
        Assert.AreEqual((ushort)0, status);
        var offset = header[4] + BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(2));
        Assert.IsTrue(offset <= body.Length);
        return new(BinaryPrimitives.ReadUInt64BigEndian(header.AsSpan(16)), Convert.ToHexString(SHA256.HashData(body.AsSpan(offset))));
    }

    internal static async Task AssertActiveGenerationUnchanged(string state)
    {
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "active-generation-cql.json")));
        var keys = snapshot.RootElement.GetProperty("keys").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var expected = snapshot.RootElement.GetProperty("before").EnumerateArray().ToArray();
        Assert.AreEqual(5, keys.Length);
        for (var i = 0; i < keys.Length; i++)
        {
            var observed = await Inspect(keys[i]);
            Assert.IsNotNull(observed, "Original active cache generation must remain present.");
            Assert.AreEqual(expected[i].GetProperty("Cas").GetUInt64(), observed.Cas);
            Assert.AreEqual(expected[i].GetProperty("Hash").GetString(), observed.Hash);
        }
    }

    [TestMethod]
    public async Task NativeVolatileGenerationFiltersWithoutChangingBytesOrCas()
    {
        Assert.IsTrue(OperatingSystem.IsLinux());
        var state = Environment.GetEnvironmentVariable("OASIS_MEMCACHED_CQL_STATE")!;
        Assert.AreEqual("/var/tmp/oasis-memcached-cql", state);
        Assert.IsTrue(File.Exists(Path.Combine(state, "active-generation.pending.json")));
        using var sourceLease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(Path.Combine(state, "demo-seed.json")))!;
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        var all = seed.Ids.Append(seed.DeletedId).ToArray();
        var keys = all.Select(id => "holon:" + id.ToString("D")).Append("holons:index").ToArray();
        foreach (var key in keys) Assert.IsNull(await Inspect(key), "Never overwrite an occupied native fixture key.");
        var provider = new MemcachedOASIS("127.0.0.1", 11239);
        var acknowledged = new List<Guid>();
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var names = new[] { "Espresso", "Latte", "Tea", "Deleted source" };
            for (var i = 0; i < all.Length; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = all[i], Name = names[i], CreatedByAvatarId = seed.Actor,
                    DeletedDate = i == 3 ? DateTime.UtcNow : DateTime.MinValue }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved); Assert.AreEqual(1, saved.SavedCount);
                acknowledged.Add(all[i]);
                if (i < 3)
                {
                    var loaded = await provider.LoadHolonAsync(all[i], loadChildren: false);
                    Assert.IsFalse(loaded.IsError, loaded.Message);
                    Assert.AreEqual(names[i], loaded.Result.Name);
                    Assert.AreEqual(seed.Actor, loaded.Result.CreatedByAvatarId);
                }
            }
            async Task<NativeValue[]> Snapshot()
            {
                var values = new List<NativeValue>();
                foreach (var key in keys)
                {
                    var value = await Inspect(key);
                    Assert.IsNotNull(value, "Original active-generation record/index disappeared.");
                    values.Add(value);
                }
                return values.ToArray();
            }
            var before = await Snapshot();
            CosmicQueryPlan<Holon> Plan(Guid[] ids, bool page = false)
            {
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MemcachedOASIS).Result)
                    .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.MemcachedOASIS, ids).Result).Result
                    .Where(row => ids.Contains(row.Id)).Result;
                if (page) query = query.Skip(1).Result.Take(1).Result;
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.MemcachedOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.MemcachedOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
            }
            foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() }, new[] { seed.DeletedId } })
            {
                var result = await provider.ExecuteCosmicQueryAsync(Plan(ids));
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(row => row.Id).ToArray());
            }
            var page = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids, true));
            Assert.IsFalse(page.IsError, page.Message);
            Assert.AreEqual(seed.Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First(), page.Result.Single().Id);
            var budget = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
            Assert.AreEqual("CQL_QUERY_LIMIT", budget.ErrorCode); Assert.IsNull(budget.Result);
            CollectionAssert.AreEqual(before, await Snapshot(), "Queries must preserve native record/index values and CAS versions.");
            using var receipt = new FileStream(Path.Combine(state, "active-generation-cql.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(receipt, new { acknowledged, keys, before, verified = true }); receipt.Flush(true);
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
