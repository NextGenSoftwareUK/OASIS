using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class FirebaseStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task ShippingRealtimeDatabaseRunsSharedStorageSuite(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_FIREBASE_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Explicit isolated Realtime Database opt-in required; Firestore is not this service.");
        var endpoint = new Uri(Environment.GetEnvironmentVariable("OASIS_FIREBASE_CONFORMANCE_DATABASE_URL")
            ?? throw new InvalidOperationException("Explicit isolated database URL required."));
        Assert.IsTrue(endpoint.IsAbsoluteUri && endpoint.UserInfo == "" && endpoint.Query == ""
            && endpoint.Fragment == "" && endpoint.AbsolutePath == "/"
            && (endpoint.Scheme == "https" || endpoint.Scheme == "http" && endpoint.Host == "127.0.0.1"),
            "HTTPS database origin or explicit loopback emulator required.");
        if (!endpoint.IsLoopback)
            Assert.AreEqual("1", Environment.GetEnvironmentVariable("OASIS_FIREBASE_CONFORMANCE_ALLOW_BILLABLE_REQUESTS"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var fixtureRoot = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "FirebaseOASIS");
        var work = Path.Combine(fixtureRoot, synchronous ? "sync" : "async");
        Directory.CreateDirectory(work);
        using var lease = new FileStream(Path.Combine(fixtureRoot, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var reportPath = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve previous checkpoints and fixtures before another run.");
        var otherPath = Path.Combine(fixtureRoot, synchronous ? "async" : "sync", "report.json");
        Assert.IsFalse(File.Exists(otherPath + ".next"), "Reconcile the other mode before starting.");
        if (File.Exists(otherPath))
        {
            await using var otherReport = File.OpenRead(otherPath);
            var prior = await JsonSerializer.DeserializeAsync<CoreStorageConformance.Report>(otherReport);
            Assert.IsTrue(prior?.FixtureCleanupComplete == true, "Reconcile the other mode before starting.");
        }
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var token = Environment.GetEnvironmentVariable("OASIS_FIREBASE_CONFORMANCE_AUTH_TOKEN");
        if (!endpoint.IsLoopback)
            Assert.IsFalse(string.IsNullOrWhiteSpace(token), "A protected hosted fixture credential is required.");
        var query = string.IsNullOrEmpty(token) ? "" : "?auth=" + Uri.EscapeDataString(token);
        Uri Node(string path) => new(endpoint, path + ".json" + query);
        // The database is dedicated to these sequential fixtures. Never adopt or
        // sweep an occupied root; a failed run must remain available for inspection.
        using (var response = await http.GetAsync(Node("")))
        {
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, "Database preflight failed.");
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.IsTrue(body.RootElement.ValueKind == JsonValueKind.Null
                || body.RootElement.ValueKind == JsonValueKind.Object && !body.RootElement.EnumerateObject().Any(),
                "Dedicated Realtime Database must be empty before dispatch.");
        }
        var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(reportPath);
        var provider = new FirebaseOASIS(endpoint.AbsoluteUri, token, http);
        var result = await CoreStorageConformance.RunAsync(provider, "FirebaseOASIS", "native", synchronous,
            report => report.Succeeded ? Task.CompletedTask : checkpoint.WriteAsync(report),
            (id, _) => FirebaseConformanceCleanup.RemoveHolonTombstoneAsync(http, Node($"oasis_holons/{id:D}"), id),
            expectedDefaultDeletion: "soft",
            observeNativeTombstone: (id, ownerId, name) => FirebaseConformanceCleanup.ObserveHolonTombstoneAsync(
                http, Node($"oasis_holons/{id:D}"), id, ownerId, name));
        Assert.IsTrue(result.Succeeded, result.CurrentCase + ": " + result.FailureType + ": " + result.FailureContract);
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
        using var remaining = JsonDocument.Parse(await http.GetStringAsync(Node("")));
        Assert.AreEqual(JsonValueKind.Null, remaining.RootElement.ValueKind, "Dedicated Firebase root must be empty after cleanup.");
        await checkpoint.WriteAsync(result);
    }
}
