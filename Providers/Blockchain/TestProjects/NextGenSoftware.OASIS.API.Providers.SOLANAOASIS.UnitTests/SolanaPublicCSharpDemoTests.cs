using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaPublicCSharpDemoTests
{
    [TestMethod, TestCategory("SolanaPublicCSharpDemoNative")]
    public async Task PublishedCSharpLinqSampleRunsVerbatimAgainstThreeAndTwoRealPublicAccounts()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_SOLANA_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Explicit retained public source configuration required.");
        var json = await System.IO.File.ReadAllTextAsync(path);
        using var configuration = JsonDocument.Parse(json);
        var addresses = configuration.RootElement.GetProperty("addresses").EnumerateArray().Select(row => row.GetString()!).ToArray();
        using var source = SolanaPublicAccountMetadataSource.FromJson(json);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        Assert.IsFalse((await source.ActivateQuerySourceAsync(deadline.Token)).IsError);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var registry = new ProviderManager(null, dna);
        var manager = new HolonManager(null, dna, registry);
        using var output = new StringWriter();
        foreach (var count in new[] { 3, 2 })
        {
            var rows = await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager, source,
                addresses.Take(count), Guid.NewGuid(), output, deadline.Token);
            Assert.AreEqual(count, rows.Count);
        }
        Assert.AreEqual(0, registry.GetAllRegisteredProviders().Count());
        StringAssert.Contains(output.ToString(), "returned holons: 3");
        StringAssert.Contains(output.ToString(), "returned holons: 2");
    }
}
