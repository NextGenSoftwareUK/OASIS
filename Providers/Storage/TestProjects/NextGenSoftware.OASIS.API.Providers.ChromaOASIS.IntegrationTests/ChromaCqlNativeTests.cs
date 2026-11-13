using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class ChromaCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);
    private sealed class OriginalWithObsoleteField : Holon { public string ObsoleteField { get; set; } = "removed by full replacement"; }

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_CHROMA_CQL_STATE")
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
        using var native = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:18002"), Timeout = TimeSpan.FromSeconds(15) };
        async Task<JsonDocument> Get(string path)
        {
            using var response = await native.GetAsync(path);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
        using var collectionReceipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "oasis_holons.ack.json")));
        var collectionId = collectionReceipt.RootElement.GetProperty("id").GetString()!;
        const string collections = "/api/v2/tenants/default_tenant/databases/default_database/collections";
        var collectionPath = collections + "/" + collectionId;
        using var rest = new HttpClient();
        var provider = new ChromaOASIS(rest, "http://127.0.0.1:18002");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        Seed seed;
        if (!File.Exists(manifest))
        {
            using var absent = await Get(collectionPath + "/count");
            Assert.AreEqual(0, absent.RootElement.GetInt32(), "Never adopt existing records.");
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
            using var indices = await Get(collections);
            CollectionAssert.AreEquivalent(new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" }, indices.RootElement.EnumerateArray().Select(v => v.GetProperty("name").GetString()).ToArray());
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        string SchemaHash(JsonElement value)
        {
            using var bytes = new MemoryStream();
            using (var writer = new Utf8JsonWriter(bytes))
            {
                void Write(JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        writer.WriteStartObject();
                        foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                        { writer.WritePropertyName(property.Name); Write(property.Value); }
                        writer.WriteEndObject();
                    }
                    else if (element.ValueKind == JsonValueKind.Array)
                    { writer.WriteStartArray(); foreach (var item in element.EnumerateArray()) Write(item); writer.WriteEndArray(); }
                    else element.WriteTo(writer);
                }
                Write(value);
            }
            return Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
        }
        var schemaFile = Path.Combine(state, "source-schema.json");
        async Task<string[]> Snapshot()
        {
            using var count = await Get(collectionPath + "/count");
            Assert.AreEqual(3, count.RootElement.GetInt32(), "Only the original three records may exist.");
            using var response = await native.PostAsJsonAsync(collectionPath + "/get",
                new { ids = seed.Ids.Select(id => id.ToString("D")).ToArray(), include = new[] { "documents" }, limit = 4, offset = 0 });
            response.EnsureSuccessStatusCode();
            using var records = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var keys = records.RootElement.GetProperty("ids");
            var documents = records.RootElement.GetProperty("documents");
            Assert.AreEqual(3, keys.GetArrayLength());
            Assert.AreEqual(3, documents.GetArrayLength());
            var rows = new List<string>();
            for (var index = 0; index < 3; index++)
            {
                var id = keys[index].GetGuid();
                Assert.IsTrue(seed.Ids.Contains(id));
                var json = documents[index].GetString()!;
                using var payload = JsonDocument.Parse(json);
                Assert.AreEqual(id, payload.RootElement.GetProperty("Id").GetGuid());
                if (id == seed.Ids[0]) Assert.IsFalse(payload.RootElement.TryGetProperty("ObsoleteField", out _));
                rows.Add(id.ToString("D") + "|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            }
            Assert.AreEqual(3, rows.Distinct().Count());
            rows.Sort(StringComparer.Ordinal);
            using var schema = await Get(collections + "/oasis_holons");
            Assert.AreEqual(collectionId, schema.RootElement.GetProperty("id").GetString());
            if (!File.Exists(baseline) && !File.Exists(schemaFile))
            {
                using var evidence = new FileStream(schemaFile, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                evidence.Write(Encoding.UTF8.GetBytes(schema.RootElement.GetRawText())); evidence.Flush(true);
            }
            rows.Insert(0, "canonical-schema|" + SchemaHash(schema.RootElement));
            return rows.ToArray();
        }
        var before = await Snapshot();
        if (File.Exists(baseline))
        {
            var original = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(baseline))!;
            var rawSchema = await File.ReadAllTextAsync(schemaFile);
            Assert.AreEqual(original[0], "native-schema|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSchema))),
                "Original schema preimage must match the retained original baseline.");
            using var originalSchema = JsonDocument.Parse(rawSchema);
            Assert.AreEqual("canonical-schema|" + SchemaHash(originalSchema.RootElement), before[0],
                "Schema values and arrays must match; only JSON object property order is immaterial.");
            CollectionAssert.AreEqual(original.Skip(1).ToArray(), before.Skip(1).ToArray());
        }
        else
        {
            var initial = before.ToArray();
            initial[0] = "native-schema|" + Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(schemaFile)));
            using var file = new FileStream(baseline, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, initial); file.Flush(true);
        }
        CosmicQueryPlan<Holon> Plan(Guid[] ids, bool paging = false)
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChromaOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.ChromaOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.ChromaOASIS,
                new[] { new CosmicQueryTarget(ProviderType.ChromaOASIS, provider.QueryCapabilities) });
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
        CollectionAssert.AreEqual(before, await Snapshot(), "Native documents/collection identity/schema must remain unchanged.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
