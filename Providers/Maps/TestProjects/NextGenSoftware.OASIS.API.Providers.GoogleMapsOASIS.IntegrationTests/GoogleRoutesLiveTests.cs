using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS.IntegrationTests;

[TestClass]
public sealed class GoogleRoutesLiveTests
{
    [DataTestMethod, DataRow(RoutingType.Driving), DataRow(RoutingType.Walking), DataRow(RoutingType.Cycling)]
    public async Task OfficialRoutesReturnsDistanceAndGeometry(RoutingType travelMode)
    {
        if (Environment.GetEnvironmentVariable("OASIS_GOOGLE_ROUTES_LIVE_TEST") != "1")
            Assert.Inconclusive("Explicitly enable the authenticated Google Routes request.");

        var key = Environment.GetEnvironmentVariable("OASIS_GOOGLE_ROUTES_API_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "Configure the restricted Google Routes test credential.");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        IOASISRoutingProvider provider = new GoogleMapsOASIS();
        var route = await provider.ComputeRouteAsync(
            new RoutingCoordinate(37.419734, -122.0827784),
            new RoutingCoordinate(37.417670, -122.079595), travelMode, timeout.Token);

        Assert.IsTrue(route.DistanceMeters > 0);
        Assert.IsTrue(route.Points.Count >= 2);
        Assert.AreEqual(37.419734, route.Points[0].Latitude, 0.01);
        Assert.AreEqual(-122.0827784, route.Points[0].Longitude, 0.01);
        Assert.AreEqual(37.417670, route.Points[^1].Latitude, 0.01);
        Assert.AreEqual(-122.079595, route.Points[^1].Longitude, 0.01);
    }
}
