using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SolrOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class SolrCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);
    private sealed class OriginalWithObsoleteField : Holon { public string ObsoleteField { get; set; } = "removed by full replacement"; }

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_SOLR_CQL_STATE")
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
        using var native = new HttpClient { BaseAddress = new Uri("https://localhost:18983"), Timeout = TimeSpan.FromSeconds(15) };
        var username = credentials.RootElement.GetProperty("username").GetString()!;
        var password = credentials.RootElement.GetProperty("password").GetString()!;
        native.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(username+":"+password)));
        async Task<JsonDocument> Get(string path)
        {
            using var response = await native.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
        using var identity = await Get("/solr/admin/info/system?wt=json");
        Assert.AreEqual("10.1.0", identity.RootElement.GetProperty("lucene").GetProperty("solr-spec-version").GetString());
        using var rest = new HttpClient();
        var provider = new SolrOASIS(rest, "https://localhost:18983", username, password);
        Seed seed;
        if (!File.Exists(manifest))
        {
            using var absent = await Get("/solr/oasis_holons/select?q=*:*&rows=0&wt=json");
            Assert.AreEqual(0, absent.RootElement.GetProperty("response").GetProperty("numFound").GetInt32(), "Never adopt existing records.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                Holon original = i == 0 ? new OriginalWithObsoleteField() : new Holon();
                original.Id = seed.Ids[i]; original.Name = names[i]; original.CreatedByAvatarId = seed.Actor; original.IsNewHolon = true;
                var saved = await provider.SaveHolonAsync(original, saveChildren: false);
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
            using var indices = await Get("/solr/admin/cores?action=STATUS&wt=json");
            CollectionAssert.AreEquivalent(new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" }, indices.RootElement.GetProperty("status").EnumerateObject().Select(v => v.Name).ToArray());
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            using var all = await Get("/solr/oasis_holons/select?q=*:*&rows=4&wt=json");
            Assert.AreEqual(3, all.RootElement.GetProperty("response").GetProperty("numFound").GetInt32());
            var rows = new List<string>();
            foreach (var id in seed.Ids)
            {
                using var record = await Get("/solr/oasis_holons/get?id="+id.ToString("D")+"&wt=json");
                var value = record.RootElement.GetProperty("doc");
                Assert.AreEqual(id, value.GetProperty("id").GetGuid());
                var json = value.GetProperty("data_json").GetString()!;
                using var payload = JsonDocument.Parse(json);
                Assert.AreEqual(id, payload.RootElement.GetProperty("id").GetGuid());
                if (id == seed.Ids[0]) Assert.IsFalse(payload.RootElement.TryGetProperty("obsoleteField", out _), "Full replacement removes original field.");
                var version = value.GetProperty("_version_").GetInt64();
                Assert.IsTrue(version > 0);
                rows.Add(id.ToString("D")+"|"+version+"|"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            }
            rows.Sort(StringComparer.Ordinal);
            using var schema = await Get("/solr/oasis_holons/schema?wt=json&showDefaults=true");
            rows.Insert(0, "native-schema|"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(schema.RootElement.GetProperty("schema").GetRawText()))));
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolrOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.SolrOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.SolrOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SolrOASIS, provider.QueryCapabilities) });
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
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
        Assert.IsNull(limited.Result, "Budget failure must not publish partial rows.");
        CollectionAssert.AreEqual(before, await Snapshot(), "Native canonical payloads/versions/schema must remain unchanged.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
