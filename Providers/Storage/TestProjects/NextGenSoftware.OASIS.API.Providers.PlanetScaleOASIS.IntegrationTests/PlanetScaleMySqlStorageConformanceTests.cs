using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using MySqlConnector;

namespace NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class PlanetScaleMySqlStorageConformanceTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstMySql(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_PLANETSCALE_MYSQL_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable MySQL CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-planetscale-mysql.");
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        var database = synchronous ? "oasis_storage_sync" : "oasis_storage_async";
        var connectionString = $"Server=127.0.0.1;Port=13306;User ID=oasis;Password={Environment.GetEnvironmentVariable("OASIS_PLANETSCALE_MYSQL_CI_PASSWORD")};Database={database};SslMode=Required";
        var provider = new PlanetScaleOASIS(connectionString);
        var reportPath = Path.Combine(directory, $"{database}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var report = await CoreStorageConformance.RunAsync(provider, nameof(PlanetScaleOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            }, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var expectedIds = new[] { report.RootId, report.ChildId, report.ImportId }.Concat(report.BatchIds);
        foreach (var id in expectedIds)
        {
            await using var delete = new MySqlCommand(
                "DELETE FROM oasis_holons WHERE id = @id AND is_deleted = 1", connection, transaction);
            delete.Parameters.AddWithValue("@id", id.ToString());
            Assert.AreEqual(1, await delete.ExecuteNonQueryAsync(), "Exactly one owned tombstone must be removed.");
        }
        foreach (var table in new[] { "oasis_holons", "oasis_avatars", "oasis_avatar_details" })
        {
            await using var count = new MySqlCommand($"SELECT COUNT(*) FROM {table}", connection, transaction);
            Assert.AreEqual(0L, await count.ExecuteScalarAsync(), $"Native table {table} must be empty.");
        }
        await transaction.CommitAsync();
        Console.WriteLine($"Verified 26 {database} checks and three empty native tables.");
    }
}
