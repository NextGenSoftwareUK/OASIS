using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS.CqlTests;

[TestClass]
public class PolygonPublicCSharpDemoTests
{
    [TestMethod, TestCategory("PolygonPublicCSharpDemoNative")]
    public async Task PublishedConsoleWalkthroughUsesRealPolygonMetadataThroughManagerAndLinq()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_POLYGON_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Explicit existing public source catalogue required.");

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var output = new StringWriter();

        // Compile the published walkthrough files themselves; no copied query.
        var report = await CosmicCqlDemo.PolygonPublicWalkthrough.RunAsync(
            path, output, deadline.Token);

        Assert.AreEqual(3, report.Full);
        Assert.AreEqual(2, report.Subset);
        StringAssert.Contains(output.ToString(), "Provider: PolygonOASIS");
        StringAssert.Contains(output.ToString(), "returned holons: 3");
        StringAssert.Contains(output.ToString(), "returned holons: 2");
    }
}
