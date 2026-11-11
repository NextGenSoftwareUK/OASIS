using System.Text.Json;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests;

public sealed class AvalancheNativeStorageConformanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedStorageAgainstAvalancheGo(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_AVALANCHE_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture)) return;
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var path = Path.GetFullPath(fixture);
        Assert.StartsWith("oasis-avalanche.", Path.GetFileName(path));
        Assert.Equal(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(path));
        var mode = synchronous ? "sync" : "async";
        const string rpc = "http://127.0.0.1:19650/ext/bc/C/rpc";
        var key = Environment.GetEnvironmentVariable("OASIS_AVALANCHE_CI_KEY");
        var contract = Environment.GetEnvironmentVariable($"OASIS_AVALANCHE_CI_CONTRACT_{mode.ToUpperInvariant()}");
        Assert.False(string.IsNullOrWhiteSpace(key));
        Assert.False(string.IsNullOrWhiteSpace(contract));

        var provider = new AvalancheOASIS(rpc, key, contract, 43112);
        var reportPath = Path.Combine(path, $"{mode}.json");
        Assert.False(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var report = await CoreStorageConformance.RunAsync(provider, nameof(AvalancheOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });
        Assert.True(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.True(report.FixtureCleanupComplete);
        Assert.Equal(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
