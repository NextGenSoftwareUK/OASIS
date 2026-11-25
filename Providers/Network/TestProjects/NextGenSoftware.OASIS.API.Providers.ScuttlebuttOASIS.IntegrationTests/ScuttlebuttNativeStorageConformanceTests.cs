using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS.IntegrationTests;

/// <summary>Runs the shared core storage suite against the Scuttlebutt storage service (ssb-db2 feed), one feed per mode.</summary>
public sealed class ScuttlebuttNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [Theory, InlineData(false), InlineData(true)]
    public async Task CoreStorageSuiteAgainstScuttlebutt(bool synchronous)
    {
        // The fixture is mandatory: this project only runs in the disposable native CI/local fixture.
        var directory = Path.GetFullPath(Env("OASIS_NATIVE_STORAGE_FIXTURE"));
        Assert.Equal(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        Assert.StartsWith("oasis-native.", Path.GetFileName(directory));
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.False(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var service = Env(synchronous ? "OASIS_SSB_SYNC_SERVICE" : "OASIS_SSB_ASYNC_SERVICE");
        var provider = new ScuttlebuttOASIS(service, Env("OASIS_SSB_STORAGE_TOKEN"), RandomNumberGenerator.GetBytes(32));
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });

        Assert.True(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.True(report.FixtureCleanupComplete);
        Assert.Equal(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
