using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class TypesenseNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstTypesense()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_TYPESENSE_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable Typesense CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-18108", Environment.GetEnvironmentVariable("OASIS_TYPESENSE_SHARED_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-elasticsearch.");
        var mode = Environment.GetEnvironmentVariable("OASIS_TYPESENSE_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:18108";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        var key = Environment.GetEnvironmentVariable("TYPESENSE_API_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        var provider = new TypesenseOASIS(client, origin, key!);
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/debug"));
        Assert.AreEqual("30.2", identity.RootElement.GetProperty("version").GetString());
        using var empty = JsonDocument.Parse(await client.GetStringAsync("/collections"));
        Assert.AreEqual(0, empty.RootElement.GetArrayLength(), "The fixture must have no pre-existing collections.");

        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            using var response = await client.GetAsync($"/collections/oasis_holons/documents/{id:D}");
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

        var report = await CoreStorageConformance.RunAsync(provider, nameof(TypesenseOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: async (id, _) =>
            {
                using var response = await client.DeleteAsync($"/collections/oasis_holons/documents/{id:D}");
                response.EnsureSuccessStatusCode();
                using var receipt = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.AreEqual(id, receipt.RootElement.GetProperty("id").GetGuid());
                using var absent = await client.GetAsync($"/collections/oasis_holons/documents/{id:D}");
                Assert.AreEqual(HttpStatusCode.NotFound, absent.StatusCode);
                return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
            },
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        foreach (var collection in new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" })
        {
            using var count = JsonDocument.Parse(await client.GetStringAsync($"/collections/{collection}"));
            Assert.AreEqual(0L, count.RootElement.GetProperty("num_documents").GetInt64(), $"Native collection {collection} must be empty.");
        }
    }
}
