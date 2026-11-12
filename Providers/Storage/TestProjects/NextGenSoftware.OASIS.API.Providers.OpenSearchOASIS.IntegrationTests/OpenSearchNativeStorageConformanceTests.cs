using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class OpenSearchNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstOpenSearch()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_OPENSEARCH_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable OpenSearch CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-19210", Environment.GetEnvironmentVariable("OASIS_OPENSEARCH_DOCUMENT_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-opensearch.");
        var mode = Environment.GetEnvironmentVariable("OASIS_OPENSEARCH_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:19210";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/"));
        Assert.AreEqual("opensearch", identity.RootElement.GetProperty("version").GetProperty("distribution").GetString());
        Assert.AreEqual("3.9.0", identity.RootElement.GetProperty("version").GetProperty("number").GetString());
        using var empty = JsonDocument.Parse(await client.GetStringAsync("/_cat/indices/oasis_*?format=json"));
        Assert.AreEqual(0, empty.RootElement.GetArrayLength(), "The fixture must have no pre-existing OASIS indices.");

        var provider = new OpenSearchOASIS(client, origin);
        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            using var response = await client.GetAsync($"/oasis_holons/_doc/{id:D}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            using var record = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var source = record.RootElement.GetProperty("_source");
            Assert.AreEqual(id, source.GetProperty("id").GetGuid());
            Assert.AreEqual(owner, source.GetProperty("createdByAvatarId").GetGuid());
            Assert.AreEqual(name, source.GetProperty("name").GetString());
            Assert.IsTrue(source.GetProperty("isDeleted").GetBoolean());
            return true;
        }

        var report = await CoreStorageConformance.RunAsync(provider, nameof(OpenSearchOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: (id, sync) => sync
                ? Task.FromResult(provider.DeleteHolonSoft(id, softDelete: false))
                : provider.DeleteHolonSoftAsync(id, softDelete: false),
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        foreach (var index in new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" })
        {
            using var count = JsonDocument.Parse(await client.GetStringAsync($"/{index}/_count"));
            Assert.AreEqual(0L, count.RootElement.GetProperty("count").GetInt64(), $"Native index {index} must be empty.");
        }
    }
}
