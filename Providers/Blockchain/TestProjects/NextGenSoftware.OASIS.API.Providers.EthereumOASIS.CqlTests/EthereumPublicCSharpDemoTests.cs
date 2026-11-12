using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class EthereumPublicCSharpDemoTests
{
    [TestMethod, TestCategory("EthereumPublicCSharpDemoNative")]
    public async Task PublishedConsoleWalkthroughUsesRealEthereumMetadataThroughManagerAndLinq()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_ETHEREUM_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Explicit existing public source catalogue required.");

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var output = new StringWriter();

        // Compile the published walkthrough files themselves; no copied query.
        var report = await CosmicCqlDemo.EthereumPublicWalkthrough.RunAsync(
            path, output, deadline.Token);

        Assert.AreEqual(3, report.Full);
        Assert.AreEqual(2, report.Subset);
        StringAssert.Contains(output.ToString(), "Provider: EthereumOASIS");
        StringAssert.Contains(output.ToString(), "returned holons: 3");
        StringAssert.Contains(output.ToString(), "returned holons: 2");
    }
}
