using System.Text.Json;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests;

#pragma warning disable CS0618 // The Desktop adapter is obsolete but still shipped; it must keep the canonical storage contract.
[Collection(HolochainConductorCollection.Name)]
public class HoloOASISDesktopNativeStorageConformanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HoloCoreStorageConformance_DesktopAdapterPersistsOnOfficialHolochainConductor(bool sync)
    {
        var adminUri = Environment.GetEnvironmentVariable("OASIS_HOLOCHAIN_ADMIN_URI")
            ?? throw new InvalidOperationException("Configure the dedicated conductor admin endpoint.");
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE")
            ?? throw new InvalidOperationException("Configure the disposable native storage fixture directory.");
        var reportPath = Path.Combine(Path.GetFullPath(fixture), sync ? "sync.json" : "async.json");
        Assert.False(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Use a fresh fixture directory for each conductor run.");

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(report));
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }

        var provider = new global::NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop.HoloOASIS(adminUri);
        var report = await CoreStorageConformance.RunAsync(provider, "HoloOASIS.Desktop", "native", sync, Checkpoint);
        Assert.True(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} [{report.FailureContract}]");
        Assert.Equal(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        Assert.True(report.FixtureCleanupComplete);
    }
}
#pragma warning restore CS0618
