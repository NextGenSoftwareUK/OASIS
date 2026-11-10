using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class BigQueryOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_BIGQUERY_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly admit a dedicated BigQuery test dataset and report directory.");

        var project = Environment.GetEnvironmentVariable("BIGQUERY_PROJECT");
        var dataset = Environment.GetEnvironmentVariable("BIGQUERY_DATASET");
        Assert.IsFalse(string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(dataset),
            "The dedicated BigQuery project and dataset must both be configured.");
        var directory = Path.Combine(root!, synchronous ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(directory), "Preserve any existing fixture evidence.");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "report.json");
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            var pending = path + ".pending";
            await using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, report);
                stream.Flush(flushToDisk: true);
            }
            File.Move(pending, path, overwrite: true);
        }

        var provider = new BigQueryOASIS(project!, dataset!);
        var result = await CoreStorageConformance.RunAsync(provider, nameof(BigQueryOASIS), "native", synchronous, Checkpoint);
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}
