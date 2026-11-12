using System.Text.Json;
using InfluxDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class InfluxDBNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstInfluxDb()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_INFLUXDB_REPORT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            Assert.Inconclusive("Requires the disposable InfluxDB CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("owned-ci-18086", Environment.GetEnvironmentVariable("OASIS_INFLUXDB_SHARED_FIXTURE"));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-influxdb.");
        var mode = Environment.GetEnvironmentVariable("OASIS_INFLUXDB_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        var token = Environment.GetEnvironmentVariable("OASIS_INFLUXDB_NATIVE_TOKEN");
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));

        const string origin = "http://127.0.0.1:18086";
        const string org = "oasis-native";
        const string bucket = "oasis-native";
        const string source = "from(bucket: \"oasis-native\") |> range(start: 0)";
        using var native = InfluxDBClientFactory.Create(origin, token!.ToCharArray());
        var query = native.GetQueryApi();
        var delete = native.GetDeleteApi();
        var provider = new InfluxDBOASIS(origin, token, org, bucket);
        var reportPath = Path.Combine(directory, $"storage-{mode}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        async Task AssertEmptyBucketAsync()
        {
            var tables = await query.QueryAsync(source + " |> limit(n: 1)", org);
            Assert.IsFalse(tables.SelectMany(table => table.Records).Any(), "The native fixture bucket must be empty.");
        }

        static string ExactHolon(Guid id)
            => $" |> filter(fn: (r) => r._measurement == \"oasis_holons\" and r.id == \"{id:D}\")";

        async Task<bool> ObserveTombstone(Guid id, Guid owner, string name)
        {
            var tables = await query.QueryAsync(source + ExactHolon(id) +
                " |> filter(fn: (r) => r._field == \"data_json\") |> group() |> sort(columns: [\"_time\"], desc: true) |> limit(n: 1)", org);
            var record = tables.SelectMany(table => table.Records).SingleOrDefault();
            if (record == null)
                return false;

            using var data = JsonDocument.Parse((string)record.GetValue());
            var stored = data.RootElement;
            Assert.AreEqual(id, stored.GetProperty("id").GetGuid());
            Assert.AreEqual(owner, stored.GetProperty("createdByAvatarId").GetGuid());
            Assert.AreEqual(name, stored.GetProperty("name").GetString());
            Assert.IsTrue(stored.GetProperty("isDeleted").GetBoolean());
            return true;
        }

        await AssertEmptyBucketAsync();
        var report = await CoreStorageConformance.RunAsync(provider, nameof(InfluxDBOASIS), "native", mode == "sync",
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            cleanupSoftDeletedFixture: async (id, _) =>
            {
                await delete.Delete(DateTime.UnixEpoch, DateTime.UtcNow,
                    $"_measurement=\"oasis_holons\" AND id=\"{id:D}\"", bucket, org);
                var remaining = await query.QueryAsync(source + ExactHolon(id) + " |> limit(n: 1)", org);
                Assert.IsFalse(remaining.SelectMany(table => table.Records).Any());
                return new OASISResult<bool> { Result = true, IsDeleted = true, DeletedCount = 1 };
            },
            expectedDefaultDeletion: "soft", observeNativeTombstone: ObserveTombstone);

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        await AssertEmptyBucketAsync();
    }
}
