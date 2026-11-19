using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class RavenNativeStorageConformanceTests
{
    private sealed class StoredHolon
    {
        public bool IsDeleted { get; set; }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstRaven(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_RAVEN_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable RavenDB CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-raven.");
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        var database = synchronous ? "oasis_storage_sync" : "oasis_storage_async";
        var urls = new[] { "http://127.0.0.1:18003" };
        using var store = new DocumentStore { Urls = urls, Database = database }.Initialize();
        await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(database)));
        Assert.AreEqual(0L, (await store.Maintenance.SendAsync(new GetStatisticsOperation())).CountOfDocuments);
        var provider = new RavenDBOASIS(urls, database);
        var reportPath = Path.Combine(directory, $"{database}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var report = await CoreStorageConformance.RunAsync(provider, nameof(RavenDBOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            }, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);

        using var session = store.OpenAsyncSession();
        var ids = new[] { report.RootId, report.ChildId, report.ImportId }.Concat(report.BatchIds)
            .Select(id => $"OasisHolons/{id}").ToArray();
        var documents = await session.LoadAsync<StoredHolon>(ids);
        foreach (var id in ids)
        {
            Assert.IsTrue(documents[id]?.IsDeleted == true, "Expected the exact owned tombstone.");
            session.Delete(id);
        }
        await session.SaveChangesAsync();
        Assert.AreEqual(0L, (await store.Maintenance.SendAsync(new GetStatisticsOperation())).CountOfDocuments);
    }
}
