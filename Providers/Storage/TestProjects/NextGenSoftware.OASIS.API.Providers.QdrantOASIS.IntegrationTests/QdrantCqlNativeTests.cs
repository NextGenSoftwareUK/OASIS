using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class QdrantCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_QDRANT_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        Assert.IsTrue(Directory.Exists(state) && File.Exists(Path.Combine(state, "environment.json")), "Original admitted environment required; never create a source from a test.");
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay.");
        var baseline = Path.Combine(state, "source-snapshot.json");
        if (File.Exists(manifest))
            Assert.IsTrue(File.Exists(baseline), "Original snapshot missing; never bless replacement state.");
        else
            Assert.IsFalse(File.Exists(baseline), "Orphan original snapshot; reconcile without reseeding.");
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "credentials.json")));
        Assert.IsTrue(OperatingSystem.IsLinux());
        Assert.AreEqual(Path.Combine(state, "ca.pem"), Environment.GetEnvironmentVariable("SSL_CERT_FILE"), "Private CA must be process-scoped; never bypass TLS validation.");
        using var native = new HttpClient { BaseAddress = new Uri("https://localhost:16333"), Timeout = TimeSpan.FromSeconds(15) };
        var apiKey = credentials.RootElement.GetProperty("apiKey").GetString()!;
        native.DefaultRequestHeaders.Add("api-key", apiKey);
        async Task<JsonDocument> Get(string path)
        {
            using var response = await native.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
        using var identity = await Get("/");
        Assert.AreEqual("1.16.3", identity.RootElement.GetProperty("version").GetString());
        using var peerReceipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "native-peer.ack.json")));
        using var raft = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "data/raft_state.json")));
        Assert.AreEqual(peerReceipt.RootElement.GetProperty("peerId").GetString(),
            raft.RootElement.GetProperty("this_peer_id").GetUInt64().ToString(System.Globalization.CultureInfo.InvariantCulture));
        var provider = new QdrantOASIS("https://localhost:16333", apiKey: apiKey);
        Seed seed;
        if (!File.Exists(manifest))
        {
            using var absent = await Get("/collections");
            Assert.AreEqual(0, absent.RootElement.GetProperty("result").GetProperty("collections").GetArrayLength(), "Never adopt existing collections.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved, "Source write requires acknowledgement.");
                using var receipt = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                var operationId = saved.MetaData["Qdrant.OperationId"];
                Assert.IsTrue(ulong.TryParse(operationId, out _));
                JsonSerializer.Serialize(receipt, new { id = seed.Ids[i], operationId, acknowledged = true }); receipt.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            using var indices = await Get("/collections");
            CollectionAssert.AreEquivalent(new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" }, indices.RootElement.GetProperty("result").GetProperty("collections").EnumerateArray().Select(v => v.GetProperty("name").GetString()).ToArray());
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            using (var countResponse = await native.PostAsJsonAsync("/collections/oasis_holons/points/count", new { exact = true }))
            {
                countResponse.EnsureSuccessStatusCode();
                using var count = JsonDocument.Parse(await countResponse.Content.ReadAsStringAsync());
                Assert.AreEqual(3L, count.RootElement.GetProperty("result").GetProperty("count").GetInt64());
            }
            using var response = await native.PostAsJsonAsync("/collections/oasis_holons/points", new { ids = seed.Ids, with_payload = true, with_vector = true });
            response.EnsureSuccessStatusCode();
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var records = payload.RootElement.GetProperty("result");
            Assert.AreEqual(3, records.GetArrayLength());
            var rows = new List<string>();
            foreach (var record in records.EnumerateArray())
            {
                var id = record.GetProperty("id").GetGuid();
                Assert.IsTrue(seed.Ids.Contains(id));
                var json = record.GetProperty("payload").GetRawText();
                var vector = record.GetProperty("vector");
                Assert.AreEqual(128, vector.GetArrayLength());
                Assert.IsTrue(vector.EnumerateArray().All(v => v.GetDouble() == 0));
                rows.Add(id.ToString("D")+"|"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))+
                    "|"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(vector.GetRawText()))));
            }
            rows.Sort(StringComparer.Ordinal);
            var config = await File.ReadAllBytesAsync(Path.Combine(state, "data/collections/oasis_holons/config.json"));
            rows.Insert(0, "native-config|"+Convert.ToHexString(SHA256.HashData(config)));
            return rows.ToArray();
        }
        var before = await Snapshot();
        if (File.Exists(baseline))
            CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(baseline))!, before);
        else
        {
            using var file = new FileStream(baseline, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, before); file.Flush(true);
        }
        CosmicQueryPlan<Holon> Plan(Guid[] ids, bool paging = false)
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.QdrantOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.QdrantOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.QdrantOASIS,
                new[] { new CosmicQueryTarget(ProviderType.QdrantOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            return plan.Result;
        }
        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
        {
            var result = await provider.ExecuteCosmicQueryAsync(Plan(ids));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(r => r.Id).ToArray());
        }
        var page = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids, true));
        Assert.IsFalse(page.IsError, page.Message);
        Assert.AreEqual(seed.Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First(), page.Result.Single().Id);
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
        Assert.IsNull(limited.Result, "Budget failure must not publish partial rows.");
        CollectionAssert.AreEqual(before, await Snapshot(), "Native payloads/vectors/configuration must remain unchanged.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
