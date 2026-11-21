using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class MeilisearchNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstMeilisearch()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_MEILISEARCH_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable Meilisearch CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-17701", Environment.GetEnvironmentVariable("OASIS_MEILISEARCH_SHARED_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-meilisearch.");
        var mode = Environment.GetEnvironmentVariable("OASIS_MEILISEARCH_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:17701";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        var key = Environment.GetEnvironmentVariable("MEILISEARCH_API_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        var provider = new MeilisearchOASIS(client, origin, key!);
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/version"));
        Assert.AreEqual("1.56.0", identity.RootElement.GetProperty("pkgVersion").GetString());
        using var empty = JsonDocument.Parse(await client.GetStringAsync("/indexes?limit=1"));
        Assert.AreEqual(0, empty.RootElement.GetProperty("total").GetInt32(), "The fixture must have no pre-existing indexes.");

        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            using var response = await client.GetAsync($"/indexes/oasis_holons/documents/{id:D}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            using var record = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var source = record.RootElement;
            Assert.AreEqual(id, source.GetProperty("id").GetGuid());
            Assert.AreEqual(owner, source.GetProperty("createdByAvatarId").GetGuid());
            Assert.AreEqual(name, source.GetProperty("name").GetString());
            Assert.IsTrue(source.GetProperty("isDeleted").GetBoolean());
            return true;
        }

        var report = await CoreStorageConformance.RunAsync(provider, nameof(MeilisearchOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: async (id, _) =>
            {
                var deleted = await provider.DeleteHolonSoftAsync(id, softDelete: false);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsTrue(deleted.IsDeleted, deleted.Message);
                Assert.AreEqual(1, deleted.DeletedCount);
                using var absent = await client.GetAsync($"/indexes/oasis_holons/documents/{id:D}");
                Assert.AreEqual(HttpStatusCode.NotFound, absent.StatusCode);
                using var error = JsonDocument.Parse(await absent.Content.ReadAsStringAsync());
                Assert.AreEqual("document_not_found", error.RootElement.GetProperty("code").GetString());
                return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
            },
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        foreach (var index in new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" })
        {
            using var count = JsonDocument.Parse(await client.GetStringAsync($"/indexes/{index}/stats"));
            Assert.AreEqual(0L, count.RootElement.GetProperty("numberOfDocuments").GetInt64(), $"Native index {index} must be empty.");
        }
    }
}
