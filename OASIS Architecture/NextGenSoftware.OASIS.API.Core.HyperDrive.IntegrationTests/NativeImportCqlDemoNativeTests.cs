using CosmicCqlDemo;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class NativeImportCqlDemoNativeTests
{
    [Fact]
    public async Task NativeSqliteDataImportsSavesAndQueriesWithStablePrivateIdentity()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var report = await NativeImportCqlDemo.RunAsync(root.FullName, Console.Out);
        Assert.Equal(3, report.Created);
        Assert.Equal(3, report.Unchanged);
        Assert.Equal(1, report.Updated);
        Assert.Equal(3, report.FirstActorRows);
        Assert.Equal(3, report.SecondActorRows);
        Assert.True(report.DistinctActorIds && report.SourcePreserved && report.Cleaned);
    }
}
