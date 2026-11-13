using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Cassandra;

namespace NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class ScyllaNativeStorageConformanceTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstScylla(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_SCYLLA_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable ScyllaDB CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-scylla.");
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        var database = synchronous ? "oasis_storage_sync" : "oasis_storage_async";
        var provider = new ScyllaDBOASIS("127.0.0.1", port: 19043, keyspace: database);
        var reportPath = Path.Combine(directory, $"{database}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var report = await CoreStorageConformance.RunAsync(provider, nameof(ScyllaDBOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            }, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);

        using var cluster = Cluster.Builder().AddContactPoint("127.0.0.1").WithPort(19043).Build();
        using var session = cluster.Connect(database);
        var expectedIds = new[] { report.RootId, report.ChildId, report.ImportId }.Concat(report.BatchIds);
        var delete = await session.PrepareAsync("DELETE FROM oasis_holons WHERE id = ? IF is_deleted = true");
        foreach (var id in expectedIds)
        {
            var result = await session.ExecuteAsync(delete.Bind(id));
            Assert.IsTrue(result.Single().GetValue<bool>("[applied]"), "Exactly one owned tombstone must be removed.");
        }
        foreach (var table in new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" })
        {
            var result = await session.ExecuteAsync(new SimpleStatement($"SELECT COUNT(*) FROM {table}"));
            Assert.AreEqual(0L, result.Single().GetValue<long>(0), $"Native table {table} must be empty.");
        }
    }
}
