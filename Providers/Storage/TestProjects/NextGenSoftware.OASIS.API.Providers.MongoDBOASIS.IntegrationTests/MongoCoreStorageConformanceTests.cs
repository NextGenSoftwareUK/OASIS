using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class MongoCoreStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true), TestCategory("MongoConformanceReadOnly")]
    public async Task OriginalAvatarsReportLoadedCountsWithoutNewWrites(bool sync)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var doc = Path.Combine(root.FullName, "Docs", "WIP", "HyperDrive-Providers");
        var archive = Path.Combine(doc, "NativeEvidence", "conformance-mongodboasis-current.json");
        using var registry = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(doc, "CONFORMANCE_RESULTS.json")));
        var registered = registry.RootElement.GetProperty("results").EnumerateArray().Single(row => row.GetProperty("provider").GetString() == "MongoDBOASIS");
        Assert.AreEqual(registered.GetProperty("sha256").GetString()!.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(archive))));
        using var baseline = JsonDocument.Parse(await File.ReadAllTextAsync(archive));
        const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection, Environment.GetEnvironmentVariable("OASIS_MONGO_CONFORMANCE_URI"));
        foreach (var run in baseline.RootElement.GetProperty("runs").EnumerateArray())
        {
            Assert.AreEqual("avatar-id", run.GetProperty("CurrentCase").GetString());
            var id = run.GetProperty("OwnerId").GetGuid();
            var mode = run.GetProperty("Synchronous").GetBoolean() ? "sync" : "async";
            using var original = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "MongoDBOASIS", mode, "report.json")));
            Assert.AreEqual(id, original.RootElement.GetProperty("OwnerId").GetGuid());
            var marker = "conformance-" + id.ToString("N");
            var provider = new MongoDBOASIS(connection, "oasis_provider_conformance_" + mode);
            var activated = await provider.ActivateProviderAsync(); Assert.IsFalse(activated.IsError); Assert.IsTrue(activated.Result);
            try
            {
                var rows = sync ? new[] { provider.LoadAvatar(id), provider.LoadAvatarByUsername(marker), provider.LoadAvatarByEmail(marker + "@example.invalid") }
                    : new[] { await provider.LoadAvatarAsync(id), await provider.LoadAvatarByUsernameAsync(marker), await provider.LoadAvatarByEmailAsync(marker + "@example.invalid") };
                foreach (var row in rows)
                {
                    Assert.IsFalse(row.IsError); Assert.IsTrue(row.IsLoaded); Assert.AreEqual(1, row.LoadedCount);
                    Assert.IsNotNull(row.Result); Assert.AreEqual(id, row.Result.Id); Assert.AreEqual(marker, row.Result.Username);
                    Assert.AreEqual(marker, row.Result.FirstName); Assert.AreEqual(marker + "@example.invalid", row.Result.Email);
                }
                var missing = sync ? provider.LoadAvatar(Guid.Empty) : await provider.LoadAvatarAsync(Guid.Empty);
                Assert.IsNull(missing.Result); Assert.IsFalse(missing.IsLoaded); Assert.AreEqual(0, missing.LoadedCount);
            }
            finally { await provider.DeActivateProviderAsync(); }
        }
    }

    [DataTestMethod, DataRow(false), DataRow(true), TestCategory("MongoCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedMongo(bool sync)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var mode = sync ? "sync" : "async";
        var work = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "MongoDBOASIS", mode);
        Directory.CreateDirectory(work);
        var reportPath = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve original Mongo fixture/report before another run.");
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, report); file.Flush(true); }
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }
        MongoDBOASIS provider;
        try
        {
            const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
            if (Environment.GetEnvironmentVariable("OASIS_MONGO_CONFORMANCE_URI") != connection)
                throw new InvalidOperationException("Explicit retained local replica-set runner required; no public fallback.");
            var databaseName = "oasis_provider_conformance_" + mode;
            var client = new MongoClient(connection);
            var ready = false;
            var deadline = DateTime.UtcNow.AddSeconds(20);
            do
            {
                try
                {
                    var hello = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
                    if (hello.GetValue("setName", "").AsString != "oasisRecoveryRs")
                        throw new InvalidDataException("Retained replica-set identity differs.");
                    ready = hello.GetValue("isWritablePrimary", false).AsBoolean;
                }
                catch (MongoConnectionException) { }
                catch (TimeoutException) { }
                if (!ready) await Task.Delay(200);
            } while (!ready && DateTime.UtcNow < deadline);
            if (!ready) throw new InvalidOperationException("Retained replica set did not become writable; no replica initialization permitted.");
            // Inspect only this fixed test namespace; never clear an existing database to obtain a green run.
            using var collections = await client.GetDatabase(databaseName).ListCollectionNamesAsync();
            if ((await collections.ToListAsync()).Count != 0)
                throw new InvalidOperationException("Existing conformance namespace requires review before new writes.");
            provider = new MongoDBOASIS(connection, databaseName);
        }
        catch (Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report { Provider = "MongoDBOASIS", Mode = "native", Synchronous = sync,
                CurrentCase = "configuration", FailureType = failure.GetType().FullName! });
            Assert.Fail("Mongo configuration blocked; no storage mutation dispatched.");
            return;
        }
        var result = await CoreStorageConformance.RunAsync(provider, "MongoDBOASIS", "native", sync, Checkpoint, expectedDefaultDeletion: "absent");
        Assert.IsTrue(result.Succeeded, result.CurrentCase + ": " + result.FailureType);
    }
}
