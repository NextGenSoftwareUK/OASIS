using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;


namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class MilvusNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstMilvus()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_MILVUS_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable Milvus CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-milvus.");
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        var mode = Environment.GetEnvironmentVariable("OASIS_MILVUS_CI_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        var synchronous = mode == "sync";
        var database = $"oasis_storage_{mode}";
        var provider = new MilvusOASIS("http://127.0.0.1:19531");
        var reportPath = Path.Combine(directory, $"{database}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var report = await CoreStorageConformance.RunAsync(provider, nameof(MilvusOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            }, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);

        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:19531/"), Timeout = TimeSpan.FromSeconds(15) };
        async Task<JsonElement> NativeAsync(string operation, object request)
        {
            using var response = await client.PostAsJsonAsync($"v2/vectordb/entities/{operation}", request);
            response.EnsureSuccessStatusCode();
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.AreEqual(0, payload.RootElement.GetProperty("code").GetInt32(), $"Native Milvus {operation} failed.");
            return payload.RootElement.Clone();
        }

        const string holons = "oasis_holons";
        var ids = new[] { report.RootId, report.ChildId, report.ImportId }.Concat(report.BatchIds)
            .Select(id => id.ToString("D")).ToArray();
        var stored = await NativeAsync("get", new { collectionName = holons, id = ids, outputFields = new[] { "id", "doc" }, consistencyLevel = "Strong" });
        var documents = stored.GetProperty("data").EnumerateArray().ToArray();
        CollectionAssert.AreEquivalent(ids, documents.Select(row => row.GetProperty("id").GetString()).ToArray());
        foreach (var row in documents)
        {
            using var record = JsonDocument.Parse(row.GetProperty("doc").GetString()!);
            Assert.AreNotEqual(DateTime.MinValue, record.RootElement.GetProperty("DeletedDate").GetDateTime());
        }
        await NativeAsync("delete", new { collectionName = holons, filter = $"id in {JsonSerializer.Serialize(ids)}" });
        foreach (var collection in new[] { holons, "oasis_avatars", "oasis_avatar_details" })
        {
            var remaining = await NativeAsync("query", new
            {
                collectionName = collection, filter = "id != \"\"", outputFields = new[] { "id" },
                consistencyLevel = "Strong", limit = 1
            });
            Assert.AreEqual(0, remaining.GetProperty("data").GetArrayLength(), $"Native collection {collection} must be empty.");
        }
    }
}
