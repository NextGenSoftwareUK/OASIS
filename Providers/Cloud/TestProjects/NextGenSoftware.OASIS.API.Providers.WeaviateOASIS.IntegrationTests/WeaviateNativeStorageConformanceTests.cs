using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class WeaviateNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstWeaviate()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_WEAVIATE_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable Weaviate CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-18080", Environment.GetEnvironmentVariable("OASIS_WEAVIATE_SHARED_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-weaviate.");
        var mode = Environment.GetEnvironmentVariable("OASIS_WEAVIATE_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:18080";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        var provider = new WeaviateOASIS(client, origin);
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/v1/meta"));
        Assert.AreEqual("1.39.8", identity.RootElement.GetProperty("version").GetString());
        using var empty = JsonDocument.Parse(await client.GetStringAsync("/v1/schema"));
        var collections = empty.RootElement.GetProperty("classes");
        Assert.IsTrue(collections.ValueKind == JsonValueKind.Null || collections.GetArrayLength() == 0,
            "The disposable Weaviate fixture must start with no collections.");

        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            using var response = await client.GetAsync($"/v1/objects/OasisHolon/{id:D}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            using var record = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var point = record.RootElement;
            Assert.AreEqual(id, point.GetProperty("id").GetGuid());
            var payload = point.GetProperty("properties");
            Assert.IsTrue(payload.GetProperty("isDeleted").GetBoolean());
            using var stored = JsonDocument.Parse(payload.GetProperty("dataJson").GetString()!);
            var source = stored.RootElement;
            Assert.AreEqual(id, source.GetProperty("id").GetGuid());
            Assert.AreEqual(owner, source.GetProperty("createdByAvatarId").GetGuid());
            Assert.AreEqual(name, source.GetProperty("name").GetString());
            Assert.IsTrue(source.GetProperty("isDeleted").GetBoolean());
            return true;
        }

        var report = await CoreStorageConformance.RunAsync(provider, nameof(WeaviateOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: async (id, _) =>
            {
                using var response = await client.DeleteAsync($"/v1/objects/OasisHolon/{id:D}");
                Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
                using var absent = await client.GetAsync($"/v1/objects/OasisHolon/{id:D}");
                Assert.AreEqual(HttpStatusCode.NotFound, absent.StatusCode);
                return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
            },
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);

        using var countResponse = await client.PostAsJsonAsync("/v1/graphql", new
        {
            query = "{ Aggregate { OasisAvatar { meta { count } } OasisAvatarDetail { meta { count } } OasisHolon { meta { count } } } }"
        });
        countResponse.EnsureSuccessStatusCode();
        using var counts = JsonDocument.Parse(await countResponse.Content.ReadAsStringAsync());
        Assert.IsFalse(counts.RootElement.TryGetProperty("errors", out _), counts.RootElement.GetRawText());
        var aggregate = counts.RootElement.GetProperty("data").GetProperty("Aggregate");
        foreach (var collection in new[] { "OasisAvatar", "OasisAvatarDetail", "OasisHolon" })
            Assert.AreEqual(0, aggregate.GetProperty(collection)[0].GetProperty("meta").GetProperty("count").GetInt32(), collection);
    }
}
