using System.Text.Json;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.TestSupport;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class GoogleCoreStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true), TestCategory("GoogleCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedFirestoreRuntime(bool sync)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var work = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "GoogleCloudOASIS", sync ? "sync" : "async");
        Directory.CreateDirectory(work);
        var reportPath = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve original Google fixture/report before another run.");
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, report); file.Flush(true); }
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }
        GoogleCloudOASIS provider;
        try
        {
            if (Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST") != "127.0.0.1:18080" ||
                Environment.GetEnvironmentVariable("OASIS_GOOGLE_CORE_SCOPE") != "oasis-google-core-conformance")
                throw new InvalidOperationException("Explicit owned local core-conformance emulator required; no hosted fallback.");
            var databaseId = sync ? "core-sync" : "core-async";
            var database = new FirestoreDbBuilder { ProjectId = "oasis-google-core-conformance", DatabaseId = databaseId,
                EmulatorDetection = EmulatorDetection.EmulatorOnly }.Build();
            await foreach (var collection in database.ListRootCollectionsAsync())
                if ((await collection.Limit(1).GetSnapshotAsync()).Count != 0)
                    throw new InvalidOperationException("Existing synthetic records require review, never clear a database to pass.");
            provider = CloudProviderFactory.CreateGoogle(new GoogleCloudOASISProviderSettings { ProjectId = "oasis-google-core-conformance",
                FirestoreDatabaseId = databaseId, EnableStorage = false, EnableFirestore = true, EnableBigQuery = false });
        }
        catch (Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report { Provider = "GoogleCloudOASIS", Mode = "native", Synchronous = sync,
                CurrentCase = "configuration", FailureType = failure.GetType().FullName! });
            Assert.Fail("Firestore emulator configuration blocked; no storage mutation dispatched."); return;
        }
        var result = await CoreStorageConformance.RunAsync(provider, "GoogleCloudOASIS", "native", sync, Checkpoint);
        Assert.IsTrue(result.Succeeded, result.CurrentCase + ": " + result.FailureType + " [" + result.FailureContract + "]");
    }
}
