using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.IntegrationTests;

[TestClass]
public sealed class SharedRoutingLiveTests
{
    [DataTestMethod, DataRow(RoutingType.Driving), DataRow(RoutingType.Walking), DataRow(RoutingType.Cycling)]
    public async Task ReturnsLiveRouteGeometry(RoutingType travelMode)
    {
        if (Environment.GetEnvironmentVariable("OASIS_ROUTING_LIVE_TESTS") != "1")
            Assert.Inconclusive("Explicitly enable authenticated routing integration tests.");

        Assert.IsFalse(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OASIS_MAPBOX_ACCESS_TOKEN")),
            "Configure the provider's restricted test credential.");

        IOASISRoutingProvider provider = new MapBoxOASIS();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var origin = new RoutingCoordinate(52.517037, 13.388860);
        var destination = new RoutingCoordinate(52.529407, 13.397634);
        var route = await provider.ComputeRouteAsync(origin, destination, travelMode, timeout.Token);

        Assert.IsTrue(route.DistanceMeters > 0);
        Assert.IsTrue(route.Points.Count >= 2);
        Assert.AreEqual(origin.Latitude, route.Points[0].Latitude, 0.005);
        Assert.AreEqual(origin.Longitude, route.Points[0].Longitude, 0.005);
        Assert.AreEqual(destination.Latitude, route.Points[^1].Latitude, 0.005);
        Assert.AreEqual(destination.Longitude, route.Points[^1].Longitude, 0.005);
    }
}
