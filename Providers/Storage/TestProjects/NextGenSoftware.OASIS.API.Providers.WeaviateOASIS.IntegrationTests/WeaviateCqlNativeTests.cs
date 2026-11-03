using Weaviate.Client;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class WeaviateCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_WEAVIATE_CQL_STATE")
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
        using var native = new HttpClient { BaseAddress = new Uri("https://localhost:18080"), Timeout = TimeSpan.FromSeconds(15) };
        var apiKey = credentials.RootElement.GetProperty("apiKey").GetString()!;
        native.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        async Task<JsonDocument> Get(string path)
        {
            using var response = await native.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
        using var identity = await Get("/v1/meta");
        Assert.AreEqual("1.39.8", identity.RootElement.GetProperty("version").GetString());
        Assert.AreEqual(4194304, identity.RootElement.GetProperty("grpcMaxMessageSize").GetInt32());
        using var queryClient = await WeaviateClientBuilder.Local(
            credentials: Auth.ApiKey(apiKey), hostname: "localhost", restPort: 18080, grpcPort: 15051, useSsl: true)
            .WithoutRetries().WithQueryTimeout(TimeSpan.FromSeconds(15)).BuildAsync();
        using var rest = new HttpClient();
        var provider = new WeaviateOASIS(rest, "https://localhost:18080", apiKey, queryClient);
        Seed seed;
        if (!File.Exists(manifest))
        {
            using var absent = await Get("/v1/schema");
            Assert.IsTrue(!absent.RootElement.TryGetProperty("classes", out var classes) || classes.ValueKind == JsonValueKind.Null || classes.GetArrayLength() == 0, "Never adopt existing classes.");
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
                JsonSerializer.Serialize(receipt, new { id = seed.Ids[i], acknowledged = true }); receipt.Flush(true);
            }
            var updated = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[0], Name = "Espresso updated",
                CreatedByAvatarId = seed.Actor }, saveChildren: false);
            Assert.IsFalse(updated.IsError, updated.Message + updated.Exception);
            Assert.IsTrue(updated.IsSaved);
            using (var receipt = new FileStream(Path.Combine(state, "source-update.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(receipt, new { id = seed.Ids[0], acknowledged = true }); receipt.Flush(true); }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            using var indices = await Get("/v1/schema");
            CollectionAssert.AreEquivalent(new[] { "OasisHolon", "OasisAvatar", "OasisAvatarDetail" }, indices.RootElement.GetProperty("classes").EnumerateArray().Select(v => v.GetProperty("class").GetString()).ToArray());
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            using var all = await Get("/v1/objects?class=OasisHolon&limit=4");
            Assert.AreEqual(3, all.RootElement.GetProperty("objects").GetArrayLength(), "Only original three objects; upsert must not duplicate.");
            var rows = new List<string>();
            foreach (var id in seed.Ids)
            {
                using var record = await Get("/v1/objects/OasisHolon/"+id.ToString("D"));
                var value = record.RootElement;
                Assert.AreEqual(id, value.GetProperty("id").GetGuid());
                Assert.AreEqual("OasisHolon", value.GetProperty("class").GetString());
                var json = value.GetProperty("properties").GetRawText();
                rows.Add(id.ToString("D")+"|"+value.GetProperty("creationTimeUnix").GetInt64()+"|"+
                    value.GetProperty("lastUpdateTimeUnix").GetInt64()+"|"+
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            }
            rows.Sort(StringComparer.Ordinal);
            using var schema = await Get("/v1/schema/OasisHolon");
            rows.Insert(0, "native-schema|"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(schema.RootElement.GetRawText()))));
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.WeaviateOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.WeaviateOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.WeaviateOASIS,
                new[] { new CosmicQueryTarget(ProviderType.WeaviateOASIS, provider.QueryCapabilities) });
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
        var updatedHead = await provider.ExecuteCosmicQueryAsync(Plan(new[] { seed.Ids[0] }));
        Assert.IsFalse(updatedHead.IsError, updatedHead.Message + updatedHead.Exception);
        Assert.AreEqual("Espresso updated", updatedHead.Result.Single().Name);
        using (var disabled = await native.PostAsJsonAsync("/v1/graphql", new { query = "{ Get { OasisHolon { dataJson } } }" }))
            Assert.AreEqual(System.Net.HttpStatusCode.UnprocessableEntity, disabled.StatusCode);
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
        Assert.IsNull(limited.Result, "Budget failure must not publish partial rows.");
        CollectionAssert.AreEqual(before, await Snapshot(), "Native payloads/timestamps/schema must remain unchanged.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
