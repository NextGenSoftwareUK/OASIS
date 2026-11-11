using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class CockroachNativeStorageConformanceTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstCockroach(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_COCKROACH_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable CockroachDB CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-cockroach.");
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        var database = synchronous ? "oasis_storage_sync" : "oasis_storage_async";
        var connectionString = $"Host=127.0.0.1;Port=26257;Username=root;Database={database};SSL Mode=Disable";
        var provider = new CockroachDBOASIS(connectionString);
        var reportPath = Path.Combine(directory, $"{database}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var report = await CoreStorageConformance.RunAsync(provider, nameof(CockroachDBOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            }, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var expectedIds = new[] { report.RootId, report.ChildId, report.ImportId }.Concat(report.BatchIds).ToArray();
        var removed = new List<Guid>();
        await using (var delete = new NpgsqlCommand(
            "DELETE FROM oasis_holons WHERE id = ANY($1) AND is_deleted RETURNING id", connection, transaction))
        {
            delete.Parameters.AddWithValue(expectedIds);
            await using var rows = await delete.ExecuteReaderAsync();
            while (await rows.ReadAsync()) removed.Add(rows.GetGuid(0));
        }
        CollectionAssert.AreEquivalent(expectedIds, removed);
        foreach (var table in new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" })
        {
            await using var count = new NpgsqlCommand($"SELECT EXISTS (SELECT 1 FROM {table})", connection, transaction);
            Assert.AreEqual(false, await count.ExecuteScalarAsync(), $"Native table {table} must be empty.");
        }
        await transaction.CommitAsync();
        Console.WriteLine($"Verified 26 {database} checks and three empty native tables.");
    }
}
