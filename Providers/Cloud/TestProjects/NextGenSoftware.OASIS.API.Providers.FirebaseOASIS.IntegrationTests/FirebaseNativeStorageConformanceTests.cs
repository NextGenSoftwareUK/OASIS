using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class FirebaseNativeStorageConformanceTests
{
    private const string DatabaseNamespace = "demo-oasis-provider-conformance";

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageAgainstRealtimeDatabase(bool sync)
    {
        if (Environment.GetEnvironmentVariable("OASIS_FIREBASE_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Enable only for an isolated Firebase Realtime Database emulator.");

        var url = Environment.GetEnvironmentVariable("FIREBASEOASIS_DATABASEURL");
        Assert.IsTrue(Uri.TryCreate(url, UriKind.Absolute, out var endpoint) &&
            endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp &&
            endpoint.AbsolutePath == "/" && endpoint.UserInfo.Length == 0 &&
            endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);

        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository != null && !Directory.Exists(Path.Combine(repository.FullName, "Scripts", "TestHosts")))
            repository = repository.Parent;
        Assert.IsNotNull(repository);

        var root = Path.Combine(repository.FullName, "TestResults", "ProviderConformanceCurrent", "FirebaseOASIS");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var reportPath = Path.Combine(root, sync ? "sync.json" : "async.json");
        var otherPath = Path.Combine(root, sync ? "async.json" : "sync.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next") || File.Exists(otherPath + ".next"));
        if (File.Exists(otherPath))
        {
            await using var other = File.OpenRead(otherPath);
            var prior = await JsonSerializer.DeserializeAsync<CoreStorageConformance.Report>(other);
            Assert.IsTrue(prior?.FixtureCleanupComplete == true, "Reconcile the other mode first.");
        }

        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        Uri Node(string path) => new(endpoint!, $"{path}.json?ns={DatabaseNamespace}");
        async Task<JsonDocument> Read(string path)
        {
            using var response = await http.GetAsync(Node(path));
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        using (var initial = await Read(""))
            Assert.AreEqual(JsonValueKind.Null, initial.RootElement.ValueKind, "Use an empty dedicated namespace.");

        async Task<OASISResult<bool>> HardClean(Guid id, bool _)
        {
            var path = $"oasis_holons/{id:D}";
            using var stored = await Read(path);
            Assert.IsTrue(stored.RootElement.TryGetProperty("id", out var storedId) &&
                storedId.TryGetGuid(out var actual) && actual == id);
            using var deleted = await http.DeleteAsync(Node(path));
            deleted.EnsureSuccessStatusCode();
            using var remaining = await Read(path);
            Assert.AreEqual(JsonValueKind.Null, remaining.RootElement.ValueKind);
            return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
        }

        var provider = new FirebaseOASIS(url!, null, http, DatabaseNamespace);
        var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(reportPath);
        var result = await CoreStorageConformance.RunAsync(provider, nameof(FirebaseOASIS), "native", sync,
            report => report.Succeeded ? Task.CompletedTask : checkpoint.WriteAsync(report), HardClean,
            expectedDefaultDeletion: "soft");

        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
        using var final = await Read("");
        Assert.AreEqual(JsonValueKind.Null, final.RootElement.ValueKind, "Fixture namespace must be empty after cleanup.");
        await checkpoint.WriteAsync(result);
    }
}
