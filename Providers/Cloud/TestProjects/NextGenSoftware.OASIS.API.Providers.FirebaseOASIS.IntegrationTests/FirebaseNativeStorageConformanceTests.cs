using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class FirebaseNativeStorageConformanceTests
{
    private const string DatabaseNamespace = "demo-oasis-provider-conformance";

    [TestMethod]
    public async Task AvatarWriteAcknowledgementAgainstRealtimeDatabase()
    {
        if (Environment.GetEnvironmentVariable("OASIS_FIREBASE_ACKNOWLEDGEMENT") != "1")
            Assert.Inconclusive("Enable only for an isolated Firebase Realtime Database emulator.");

        var endpoint = GetLoopbackEndpoint();
        var fixtureNamespace = $"demo-oasis-ack-{Guid.NewGuid():N}";
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var provider = new FirebaseOASIS(endpoint.AbsoluteUri, null, http, fixtureNamespace);
        var avatar = new Avatar { Username = "acknowledgement", Email = "ack@example.invalid", FirstName = "Ack" };
        var saved = await provider.SaveAvatarAsync(avatar);
        Assert.IsTrue(saved.IsSaved && !saved.IsError, saved.Message);
        Assert.AreNotEqual(Guid.Empty, avatar.Id);

        var loaded = await provider.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(avatar.Id, loaded.Result?.Id);
        Assert.AreEqual(avatar.Username, loaded.Result?.Username);

        var deleted = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
        Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
        using var remaining = JsonDocument.Parse(await http.GetStringAsync(new Uri(endpoint, $".json?ns={fixtureNamespace}")));
        Assert.AreEqual(JsonValueKind.Null, remaining.RootElement.ValueKind);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageAgainstRealtimeDatabase(bool sync)
    {
        if (Environment.GetEnvironmentVariable("OASIS_FIREBASE_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Enable only for an isolated Firebase Realtime Database emulator.");

        var endpoint = GetLoopbackEndpoint();

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

        var provider = new FirebaseOASIS(endpoint.AbsoluteUri, null, http, DatabaseNamespace);
        var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(reportPath);
        var result = await CoreStorageConformance.RunAsync(provider, nameof(FirebaseOASIS), "native", sync,
            report => report.Succeeded ? Task.CompletedTask : checkpoint.WriteAsync(report),
            (id, _) => FirebaseConformanceCleanup.RemoveHolonTombstoneAsync(http, Node($"oasis_holons/{id:D}"), id),
            expectedDefaultDeletion: "soft");

        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
        using var final = await Read("");
        Assert.AreEqual(JsonValueKind.Null, final.RootElement.ValueKind, "Fixture namespace must be empty after cleanup.");
        await checkpoint.WriteAsync(result);
    }

    [TestMethod]
    public async Task HolonGraphReadAgainstRealtimeDatabase()
    {
        if (Environment.GetEnvironmentVariable("OASIS_FIREBASE_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Enable only for an isolated Firebase Realtime Database emulator.");

        var endpoint = GetLoopbackEndpoint();
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var provider = new FirebaseOASIS(endpoint.AbsoluteUri, null, http, DatabaseNamespace);
        var root = new Holon { Id = Guid.NewGuid(), Name = "graph root" };
        var child = new Holon { Id = Guid.NewGuid(), Name = "graph child" };
        var grandchild = new Holon { Id = Guid.NewGuid(), Name = "graph grandchild" };
        child.Children.Add(grandchild);
        root.Children.Add(child);

        try
        {
            var saved = await provider.SaveHolonAsync(root);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.AreEqual(3, saved.SavedCount);

            var rootOnly = provider.LoadHolon(root.Id, loadChildren: false);
            Assert.IsFalse(rootOnly.IsError, rootOnly.Message);
            Assert.AreEqual(0, rootOnly.Result.Children.Count);

            var immediate = await provider.LoadHolonAsync(root.Id, recursive: false);
            Assert.IsFalse(immediate.IsError, immediate.Message);
            Assert.AreEqual(child.Id, immediate.Result.Children.Single().Id);
            Assert.AreEqual(0, immediate.Result.Children.Single().Children.Count);

            var clipped = await provider.LoadHolonAsync(root.Id, maxChildDepth: 1);
            Assert.IsFalse(clipped.IsError, clipped.Message);
            Assert.AreEqual(child.Id, clipped.Result.Children.Single().Id);
            Assert.AreEqual(0, clipped.Result.Children.Single().Children.Count);

            var expanded = provider.LoadHolon(root.Id.ToString("D"));
            Assert.IsFalse(expanded.IsError, expanded.Message);
            Assert.AreEqual(grandchild.Id, expanded.Result.Children.Single().Children.Single().Id);

            var removed = await provider.DeleteHolonAsync(grandchild.Id);
            Assert.IsTrue(removed.IsDeleted && !removed.IsError, removed.Message);
            var live = await provider.LoadHolonAsync(root.Id);
            Assert.IsFalse(live.IsError, live.Message);
            Assert.AreEqual(0, live.Result.Children.Single().Children.Count);
        }
        finally
        {
            foreach (var id in new[] { grandchild.Id, child.Id, root.Id })
            {
                using var response = await http.DeleteAsync(
                    new Uri(endpoint, $"oasis_holons/{id:D}.json?ns={DatabaseNamespace}"));
                response.EnsureSuccessStatusCode();
            }
        }
    }

    private static Uri GetLoopbackEndpoint()
    {
        var url = Environment.GetEnvironmentVariable("FIREBASEOASIS_DATABASEURL");
        Assert.IsTrue(Uri.TryCreate(url, UriKind.Absolute, out var endpoint) &&
            endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp &&
            endpoint.AbsolutePath == "/" && endpoint.UserInfo.Length == 0 &&
            endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
        return endpoint!;
    }
}
