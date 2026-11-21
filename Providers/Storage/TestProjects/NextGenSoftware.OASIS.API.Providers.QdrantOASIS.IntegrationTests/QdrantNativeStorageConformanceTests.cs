using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class QdrantNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstQdrant()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_QDRANT_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable Qdrant CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-16333", Environment.GetEnvironmentVariable("OASIS_QDRANT_SHARED_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-qdrant.");
        var mode = Environment.GetEnvironmentVariable("OASIS_QDRANT_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:16333";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        var provider = new QdrantOASIS(client, origin);
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/"));
        Assert.AreEqual("1.16.3", identity.RootElement.GetProperty("version").GetString());
        using var empty = JsonDocument.Parse(await client.GetStringAsync("/collections"));
        Assert.AreEqual(0, empty.RootElement.GetProperty("result").GetProperty("collections").GetArrayLength());

        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            using var response = await client.GetAsync($"/collections/oasis_holons/points/{id:D}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            using var record = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var point = record.RootElement.GetProperty("result");
            Assert.AreEqual(id, point.GetProperty("id").GetGuid());
            using var vectors = await client.PostAsJsonAsync("/collections/oasis_holons/points",
                new { ids = new[] { id.ToString("D") }, with_payload = false, with_vector = true });
            vectors.EnsureSuccessStatusCode();
            using var vectorResult = JsonDocument.Parse(await vectors.Content.ReadAsStringAsync());
            var nativePoint = vectorResult.RootElement.GetProperty("result")[0];
            Assert.IsTrue(!nativePoint.TryGetProperty("vector", out var vector) || vector.ValueKind == JsonValueKind.Null ||
                vector.ValueKind == JsonValueKind.Object && !vector.EnumerateObject().Any(), "Payload-only storage must not synthesize a vector.");
            var payload = point.GetProperty("payload");
            Assert.IsTrue(payload.GetProperty("is_deleted").GetBoolean());
            using var stored = JsonDocument.Parse(payload.GetProperty("data_json").GetString()!);
            var source = stored.RootElement;
            Assert.AreEqual(id, source.GetProperty("id").GetGuid());
            Assert.AreEqual(owner, source.GetProperty("createdByAvatarId").GetGuid());
            Assert.AreEqual(name, source.GetProperty("name").GetString());
            Assert.IsTrue(source.GetProperty("isDeleted").GetBoolean());
            return true;
        }

        var report = await CoreStorageConformance.RunAsync(provider, nameof(QdrantOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: async (id, _) =>
            {
                using var response = await client.PostAsJsonAsync("/collections/oasis_holons/points/delete?wait=true",
                    new { points = new[] { id.ToString("D") } });
                response.EnsureSuccessStatusCode();
                using var receipt = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.AreEqual("ok", receipt.RootElement.GetProperty("status").GetString());
                Assert.AreEqual("completed", receipt.RootElement.GetProperty("result").GetProperty("status").GetString());
                using var absent = await client.GetAsync($"/collections/oasis_holons/points/{id:D}");
                Assert.AreEqual(HttpStatusCode.NotFound, absent.StatusCode);
                return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
            },
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        var embeddedId = Guid.NewGuid();
        var embedding = new float[128];
        embedding[0] = 1;
        var embedded = new Holon { Id = embeddedId, Name = "Qdrant embedding preservation fixture" };
        var initial = mode == "sync" ? provider.SaveHolon(embedded, saveChildren: false)
            : await provider.SaveHolonAsync(embedded, saveChildren: false);
        Assert.IsTrue(initial.IsSaved, initial.Message);
        var invalid = await provider.SetHolonEmbeddingAsync(embeddedId, new float[128]);
        Assert.IsTrue(invalid.IsError);
        Assert.IsFalse(invalid.IsSaved);
        var indexed = await provider.SetHolonEmbeddingAsync(embeddedId, embedding);
        Assert.IsTrue(indexed.IsSaved, indexed.Message);
        Assert.AreEqual(1, indexed.SavedCount);

        embedded.Name = "Qdrant embedding preserved after payload update";
        var saved = mode == "sync" ? provider.SaveHolon(embedded, saveChildren: false)
            : await provider.SaveHolonAsync(embedded, saveChildren: false);
        Assert.IsTrue(saved.IsSaved, saved.Message);
        var nearest = await provider.SearchHolonsByEmbeddingAsync(embedding, limit: 2);
        Assert.IsTrue(nearest.IsLoaded, nearest.Message);
        Assert.AreEqual(1, nearest.LoadedCount);
        Assert.AreEqual(embeddedId, nearest.Result[0].Holon.Id);
        Assert.AreEqual(embedded.Name, nearest.Result[0].Holon.Name);
        Assert.AreEqual(1d, nearest.Result[0].Score, 0.000001d);
        using (var retrieval = await client.PostAsJsonAsync("/collections/oasis_holons/points",
            new { ids = new[] { embeddedId.ToString("D") }, with_payload = true, with_vector = true }))
        {
            retrieval.EnsureSuccessStatusCode();
            using var retrieved = JsonDocument.Parse(await retrieval.Content.ReadAsStringAsync());
            var point = retrieved.RootElement.GetProperty("result")[0];
            CollectionAssert.AreEqual(embedding, point.GetProperty("vector").EnumerateArray().Select(value => value.GetSingle()).ToArray());
            using var payload = JsonDocument.Parse(point.GetProperty("payload").GetProperty("data_json").GetString()!);
            Assert.AreEqual(embedded.Name, payload.RootElement.GetProperty("name").GetString());
        }
        var tombstone = mode == "sync" ? provider.DeleteHolon(embeddedId) : await provider.DeleteHolonAsync(embeddedId);
        Assert.IsTrue(tombstone.IsDeleted, tombstone.Message);
        var hidden = await provider.SearchHolonsByEmbeddingAsync(embedding);
        Assert.IsTrue(hidden.IsLoaded, hidden.Message);
        Assert.AreEqual(0, hidden.LoadedCount);
        using (var removed = await client.PostAsJsonAsync("/collections/oasis_holons/points/delete?wait=true",
            new { points = new[] { embeddedId.ToString("D") } }))
        {
            removed.EnsureSuccessStatusCode();
            using var receipt = JsonDocument.Parse(await removed.Content.ReadAsStringAsync());
            Assert.AreEqual("completed", receipt.RootElement.GetProperty("result").GetProperty("status").GetString());
        }

        foreach (var collection in new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" })
        {
            using var response = await client.PostAsJsonAsync($"/collections/{collection}/points/count", new { exact = true });
            response.EnsureSuccessStatusCode();
            using var count = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.AreEqual("ok", count.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(0L, count.RootElement.GetProperty("result").GetProperty("count").GetInt64(), $"Native collection {collection} must be empty.");
        }
    }
}
