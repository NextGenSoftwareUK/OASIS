using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.SolrOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class SolrNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstSolr()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_SOLR_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable Solr CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-18984", Environment.GetEnvironmentVariable("OASIS_SOLR_SHARED_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-solr.");
        var mode = Environment.GetEnvironmentVariable("OASIS_SOLR_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:18984";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        var provider = new SolrOASIS(client, origin);
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/solr/admin/info/system?wt=json"));
        Assert.AreEqual("std", identity.RootElement.GetProperty("mode").GetString());
        Assert.AreEqual("10.1.0", identity.RootElement.GetProperty("lucene").GetProperty("solr-spec-version").GetString());
        var cores = new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" };
        async Task AssertEmptyCores()
        {
            foreach (var core in cores)
            {
                using var count = JsonDocument.Parse(await client.GetStringAsync($"/solr/{core}/query?q=*:*&rows=0&wt=json"));
                Assert.AreEqual(0, count.RootElement.GetProperty("responseHeader").GetProperty("status").GetInt32());
                Assert.AreEqual(0L, count.RootElement.GetProperty("response").GetProperty("numFound").GetInt64(), $"Native core {core} must be empty.");
            }
        }
        await AssertEmptyCores();

        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task<JsonElement> ReadNativeHolon(Guid id)
        {
            using var response = JsonDocument.Parse(await client.GetStringAsync($"/solr/oasis_holons/get?id={id:D}&wt=json&omitHeader=false"));
            Assert.AreEqual(0, response.RootElement.GetProperty("responseHeader").GetProperty("status").GetInt32());
            return response.RootElement.GetProperty("doc").Clone();
        }

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            var document = await ReadNativeHolon(id);
            if (document.ValueKind == JsonValueKind.Null)
                return false;
            Assert.AreEqual(id, document.GetProperty("id").GetGuid());
            Assert.IsTrue(document.GetProperty("isDeleted").GetBoolean());
            using var payload = JsonDocument.Parse(document.GetProperty("data_json").GetString()!);
            var source = payload.RootElement;
            Assert.AreEqual(id, source.GetProperty("id").GetGuid());
            Assert.AreEqual(owner, source.GetProperty("createdByAvatarId").GetGuid());
            Assert.AreEqual(name, source.GetProperty("name").GetString());
            Assert.IsTrue(source.GetProperty("isDeleted").GetBoolean());
            return true;
        }

        var report = await CoreStorageConformance.RunAsync(provider, nameof(SolrOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: async (id, _) =>
            {
                using var payload = JsonContent.Create(new { delete = new { id = id.ToString("D") } });
                using var response = await client.PostAsync("/solr/oasis_holons/update?commit=true&waitSearcher=true&wt=json&omitHeader=false", payload);
                response.EnsureSuccessStatusCode();
                using var receipt = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.AreEqual(0, receipt.RootElement.GetProperty("responseHeader").GetProperty("status").GetInt32());
                Assert.AreEqual(JsonValueKind.Null, (await ReadNativeHolon(id)).ValueKind);
                return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
            },
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        await AssertEmptyCores();
    }
}
