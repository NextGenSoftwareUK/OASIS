using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.API.Providers.MapPlatforms.UnitTests;

[TestClass]
public sealed class MapProviderBaseTests
{
    private static readonly Geolocation London = new(51.5074, -0.1278);

    private static TestMapProvider Started(ScriptedRouting routing = null, IMapCommandSink sink = null)
    {
        var provider = new TestMapProvider(routing ?? new ScriptedRouting());
        provider.Initialize(sink);
        provider.SetOrigin(London);
        return provider;
    }

    [TestMethod]
    public void OperationsFailUntilInitialised()
    {
        var provider = new TestMapProvider(new ScriptedRouting());

        Assert.IsFalse(provider.ZoomMapIn(1));
        StringAssert.Contains(provider.LastError, "Initialize");
    }

    [TestMethod]
    public void InitializeRejectsObjectsThatAreNotRenderers()
    {
        var provider = new TestMapProvider(new ScriptedRouting());
        Assert.ThrowsException<ArgumentException>(() => provider.Initialize("not a renderer"));
    }

    [TestMethod]
    public void PanMovesTheCentreByGroundDistance()
    {
        var provider = Started();

        Assert.IsTrue(provider.PanMapUp(1000));
        Assert.IsTrue(provider.Camera.Centre.Latitude > London.Latitude);
        Assert.AreEqual(1000, provider.CalculateDistance(London, provider.Camera.Centre), 1);
    }

    [TestMethod]
    public void ZoomIsClampedAndOrbitIsNormalised()
    {
        var provider = Started();

        Assert.IsTrue(provider.ZoomMapIn(100));
        Assert.AreEqual(MapCameraState.MaxZoom, provider.Camera.Zoom);
        Assert.IsTrue(provider.UpdateOrbitValue(-90));
        Assert.AreEqual(270, provider.Camera.Bearing, 1e-9);
    }

    [TestMethod]
    public void HeadlessCommandsAreQueuedAndDrained()
    {
        var provider = Started();
        provider.ZoomMapOut(2);
        provider.DropPin(London, "marker");

        IList<MapCommand> commands = provider.DrainCommands();
        CollectionAssert.AreEqual(new[] { MapCommandType.SetCamera, MapCommandType.DropPin }, commands.Select(c => c.CommandType).ToArray());
        Assert.AreEqual(0, provider.PendingCommandCount);
    }

    [TestMethod]
    public void BoundRendererReceivesCommandsInsteadOfTheQueue()
    {
        var sink = new RecordingSink();
        var provider = Started(sink: sink);

        Assert.IsTrue(provider.DropPin(London, "marker"));
        Assert.AreEqual(1, sink.Applied.Count);
        Assert.IsTrue(sink.Applied[0].AppliedToRenderer);
        Assert.AreEqual(0, provider.PendingCommandCount);
    }

    [TestMethod]
    public void RejectedCommandsLeaveStateUnchanged()
    {
        var provider = Started(sink: new RecordingSink { Accept = false });

        Assert.IsFalse(provider.DropPin(London, "marker"));
        Assert.AreEqual(0, provider.Pins.Count);
        StringAssert.Contains(provider.LastError, "rejected");
    }

    [TestMethod]
    public void PinsAreRemovedByTheirObject()
    {
        var provider = Started();
        provider.DropPin(London, "marker");

        Assert.IsTrue(provider.RemovePin("marker"));
        Assert.AreEqual(0, provider.Pins.Count);
        Assert.IsFalse(provider.RemovePin("marker"));
    }

    [TestMethod]
    public void PlacementsAreQueryableByKindAndDistance()
    {
        var provider = Started();
        Assert.IsTrue(provider.PlaceQuestOnMap("quest", 51.5080, -0.1280));
        Assert.IsTrue(provider.PlaceGeoNFTOnMap("nft", 48.8566, 2.3522));

        Assert.AreEqual(1, provider.GetPlacements(MapPlacementKind.Quest).Count);
        Assert.AreEqual("quest", provider.GetPlacementsNear(London, 1000).Single().Entity);
        Assert.IsFalse(provider.PlaceOAPPOnMap("oapp", 95, 0));
    }

    [TestMethod]
    public void WorldPositionsRoundTripThroughCoordinates()
    {
        var provider = Started();
        var paris = new Geolocation(48.8566, 2.3522);

        var world = (MapWorldPosition)provider.ConvertLatLongToWorldPosition(paris.Latitude, paris.Longitude);
        var back = (Geolocation)provider.ConvertWorldPositionToLatLong(world);

        Assert.AreEqual(paris.Latitude, back.Latitude, 1e-9);
        Assert.AreEqual(paris.Longitude, back.Longitude, 1e-9);
        Assert.IsTrue(world.X > 0 && world.Z < 0, "Paris is east and south of London.");
    }

    [TestMethod]
    public void DrawingAtAnOffsetNeedsAnOrigin()
    {
        var provider = new TestMapProvider(new ScriptedRouting());
        provider.Initialize(null);

        Assert.IsFalse(provider.Draw3DObjectOnMap("model", 10, 10));
        StringAssert.Contains(provider.LastError, "origin");
    }

    [TestMethod]
    public void DrawingAtAnOffsetRecordsTheProjectedLocation()
    {
        var provider = Started();

        Assert.IsTrue(provider.Draw3DObjectOnMap("model", 0, 0));
        MapCommand command = provider.DrainCommands().Single();
        Assert.AreEqual(London.Latitude, command.Location.Latitude, 1e-9);
    }

    [TestMethod]
    public void RouteBetweenEntitiesUsesTheRoutingService()
    {
        var routing = new ScriptedRouting();
        var provider = Started(routing);
        provider.DefaultRoutingType = RoutingType.Cycling;

        Assert.IsTrue(provider.CreateAndDrawRouteOnMapBetweenHolons(
            new GeoEntity { Latitude = 51.50, Longitude = -0.12 }, new GeoEntity { Latitude = 51.52, Longitude = -0.10 }));

        Assert.AreEqual(RoutingType.Cycling, routing.Calls.Single().Type);
        Assert.AreEqual(3, provider.Routes.Single().Geometry.Count);
        Assert.AreEqual(MapCommandType.DrawRoute, provider.DrainCommands().Single().CommandType);
    }

    [TestMethod]
    public void MultiLegRoutesJoinWithoutDuplicatingSharedPoints()
    {
        var provider = Started();
        var points = new[] { new Geolocation(51.50, -0.12), new Geolocation(51.51, -0.11), new Geolocation(51.52, -0.10) };

        Assert.IsTrue(provider.CreateAndDrawRouteOnMapBeweenPoints(points));

        MapRoute route = provider.Routes.Single();
        Assert.AreEqual(5, route.Geometry.Count);
        Assert.AreEqual(200, route.DistanceMetres);
        Assert.AreEqual(120, route.DurationSeconds);
    }

    [TestMethod]
    public void RoutingFailuresAreReportedNotReplacedWithStraightLines()
    {
        var provider = Started(new ScriptedRouting { Error = "vendor said no" });

        Assert.IsFalse(provider.CreateAndDrawRouteOnMapBetweenHolons(London, new Geolocation(51.52, -0.10)));
        StringAssert.Contains(provider.LastError, "vendor said no");
        Assert.AreEqual(0, provider.Routes.Count);
        Assert.AreEqual(0, provider.PendingCommandCount);
    }

    [TestMethod]
    public void EntitiesWithoutCoordinatesCannotBeRouted()
    {
        var provider = Started();
        Assert.IsFalse(provider.CreateAndDrawRouteOnMapBetweenHolons("no coordinates", London));
    }

    [TestMethod]
    public async Task SharedRoutingInterfaceThrowsWithTheVendorMessage()
    {
        IOASISRoutingProvider provider = Started(new ScriptedRouting { Error = "quota exceeded" });

        var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            provider.ComputeRouteAsync(new RoutingCoordinate(51.5, -0.1), new RoutingCoordinate(51.6, -0.2), RoutingType.Driving));
        StringAssert.Contains(error.Message, "quota exceeded");
    }

    [TestMethod]
    public async Task SharedRoutingInterfaceReturnsTheGeometry()
    {
        IOASISRoutingProvider provider = Started();

        RoutingRoute route = await provider.ComputeRouteAsync(new RoutingCoordinate(51.5, -0.1), new RoutingCoordinate(51.6, -0.2), RoutingType.Walking);
        Assert.AreEqual(100, route.DistanceMeters);
        Assert.AreEqual(3, route.Points.Count);
    }

    [TestMethod]
    public async Task OriginWaitCompletesWhenTheFirstLocationArrives()
    {
        var provider = new TestMapProvider(new ScriptedRouting());
        provider.Initialize(null);
        Task wait = provider.WaitForOriginSet();
        Assert.IsFalse(wait.IsCompleted);

        provider.SetCurrentLocation(London);
        await wait;
        Assert.AreEqual(London.Latitude, provider.GetCurrentLocation().Latitude);
    }
}
