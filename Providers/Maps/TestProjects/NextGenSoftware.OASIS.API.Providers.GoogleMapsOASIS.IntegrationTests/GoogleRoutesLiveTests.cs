using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS.IntegrationTests;

[TestClass]
public sealed class GoogleRoutesLiveTests
{
    [TestMethod]
    public async Task OfficialRoutesReturnsDrivingDistanceAndPolyline()
    {
        if (Environment.GetEnvironmentVariable("OASIS_GOOGLE_ROUTES_LIVE_TEST") != "1")
            Assert.Inconclusive("Explicitly enable the authenticated Google Routes request.");

        var key = Environment.GetEnvironmentVariable("OASIS_GOOGLE_ROUTES_API_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "Configure the restricted Google Routes test credential.");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var provider = new GoogleMapsOASIS();
        var route = await provider.ComputeDrivingRouteAsync(
            37.419734, -122.0827784, 37.417670, -122.079595, key!, timeout.Token);

        Assert.IsFalse(route.IsError, route.Message);
        Assert.IsTrue(route.IsLoaded);
        Assert.AreEqual(1, route.LoadedCount);
        Assert.IsNotNull(route.Result);
        Assert.IsTrue(route.Result!.DistanceMeters > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(route.Result.EncodedPolyline));
    }
}
