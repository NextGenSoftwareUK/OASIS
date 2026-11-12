using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class ElasticsearchNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstElasticsearch()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_ELASTICSEARCH_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable Elasticsearch CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-19211", Environment.GetEnvironmentVariable("OASIS_ELASTICSEARCH_DOCUMENT_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-elasticsearch.");
        var mode = Environment.GetEnvironmentVariable("OASIS_ELASTICSEARCH_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:19211";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        using var product = await client.GetAsync("/");
        product.EnsureSuccessStatusCode();
        Assert.IsTrue(product.Headers.TryGetValues("X-Elastic-Product", out var values));
        CollectionAssert.Contains(values!.ToArray(), "Elasticsearch");
        using var identity = JsonDocument.Parse(await product.Content.ReadAsStringAsync());
        Assert.AreEqual("9.5.5", identity.RootElement.GetProperty("version").GetProperty("number").GetString());
        using var empty = JsonDocument.Parse(await client.GetStringAsync("/_cat/indices/oasis-*?format=json"));
        Assert.AreEqual(0, empty.RootElement.GetArrayLength(), "The fixture must have no pre-existing OASIS indices.");

        var provider = new ElasticsearchOASIS(client, origin);
        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            using var response = await client.GetAsync($"/oasis-holons/_doc/{id:D}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            using var record = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var stored = record.RootElement.GetProperty("_source");
            Assert.IsTrue(stored.GetProperty("isDeleted").GetBoolean());
            using var payload = JsonDocument.Parse(stored.GetProperty("dataJson").GetString()!);
            var source = payload.RootElement;
            Assert.AreEqual(id, source.GetProperty("id").GetGuid());
            Assert.AreEqual(owner, source.GetProperty("createdByAvatarId").GetGuid());
            Assert.AreEqual(name, source.GetProperty("name").GetString());
            Assert.IsTrue(source.GetProperty("isDeleted").GetBoolean());
            return true;
        }

        var report = await CoreStorageConformance.RunAsync(provider, nameof(ElasticsearchOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: async (id, _) =>
            {
                using var response = await client.DeleteAsync($"/oasis-holons/_doc/{id:D}?refresh=wait_for");
                response.EnsureSuccessStatusCode();
                using var receipt = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var native = receipt.RootElement;
                Assert.AreEqual(id.ToString("D"), native.GetProperty("_id").GetString());
                Assert.AreEqual("oasis-holons", native.GetProperty("_index").GetString());
                Assert.AreEqual("deleted", native.GetProperty("result").GetString());
                Assert.AreEqual(0, native.GetProperty("_shards").GetProperty("failed").GetInt32());
                return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
            },
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        foreach (var index in new[] { "oasis-holons", "oasis-avatars", "oasis-avatar-details" })
        {
            using var count = JsonDocument.Parse(await client.GetStringAsync($"/{index}/_count"));
            Assert.AreEqual(0L, count.RootElement.GetProperty("count").GetInt64(), $"Native index {index} must be empty.");
        }
    }
}
