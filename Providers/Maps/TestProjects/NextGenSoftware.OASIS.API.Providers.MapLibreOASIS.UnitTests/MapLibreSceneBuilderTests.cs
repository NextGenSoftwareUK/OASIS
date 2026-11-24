using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapLibreOASIS.UnitTests;

[TestClass]
public sealed class MapLibreSceneBuilderTests
{
    private static MapLibreOASIS Started()
    {
        var maps = new MapLibreOASIS("https://osrm.example.test", "https://nominatim.example.test");
        maps.Initialize(null);
        maps.SetOrigin(new Geolocation(51.5074, -0.1278));
        return maps;
    }

    [TestMethod]
    public void ProviderUsesOpenStreetMapServices()
    {
        var maps = Started();

        Assert.AreEqual(MapProviderType.MapLibre, maps.MapProviderType);
        Assert.IsInstanceOfType(maps.DirectionsAPI, typeof(OsrmRoutingService));
        Assert.IsInstanceOfType(maps.GeocodingProvider, typeof(NominatimPlaceSearchService));
    }

    [TestMethod]
    public void PointsSourceIsGeoJsonWithLongitudeFirst()
    {
        var maps = Started();
        Assert.IsTrue(maps.DropPin(new Geolocation(51.5, -0.12), "pin"));
        Assert.IsTrue(maps.PlaceQuestOnMap("quest", 51.51, -0.13));

        using JsonDocument source = JsonDocument.Parse(maps.Scene.BuildPointsSource());
        Assert.AreEqual("geojson", source.RootElement.GetProperty("type").GetString());

        JsonElement[] features = source.RootElement.GetProperty("data").GetProperty("features").EnumerateArray().ToArray();
        Assert.AreEqual(2, features.Length);

        JsonElement pin = features.Single(f => f.GetProperty("properties").GetProperty("kind").GetString() == "pin");
        JsonElement coordinates = pin.GetProperty("geometry").GetProperty("coordinates");
        Assert.AreEqual(-0.12, coordinates[0].GetDouble());
        Assert.AreEqual(51.5, coordinates[1].GetDouble());
        Assert.IsTrue(features.Any(f => f.GetProperty("properties").GetProperty("kind").GetString() == "Quest"));
    }

    [TestMethod]
    public void EmptyRoutesSourceIsAValidFeatureCollection()
    {
        using JsonDocument source = JsonDocument.Parse(Started().Scene.BuildRoutesSource());

        Assert.AreEqual("FeatureCollection", source.RootElement.GetProperty("data").GetProperty("type").GetString());
        Assert.AreEqual(0, source.RootElement.GetProperty("data").GetProperty("features").GetArrayLength());
    }

    [TestMethod]
    public void LayersReferenceTheSourcesWithStyleSpecTypes()
    {
        using JsonDocument layers = JsonDocument.Parse(Started().Scene.BuildLayers());
        JsonElement[] all = layers.RootElement.EnumerateArray().ToArray();

        JsonElement line = all.Single(l => l.GetProperty("type").GetString() == "line");
        Assert.AreEqual(MapLibreSceneBuilder.RoutesSourceId, line.GetProperty("source").GetString());
        Assert.IsTrue(line.GetProperty("paint").TryGetProperty("line-color", out _));

        JsonElement circle = all.Single(l => l.GetProperty("type").GetString() == "circle");
        Assert.AreEqual(MapLibreSceneBuilder.PointsSourceId, circle.GetProperty("source").GetString());
        Assert.AreEqual("match", circle.GetProperty("paint").GetProperty("circle-color")[0].GetString());
    }

    [TestMethod]
    public void CameraFollowsTheSessionInLongitudeLatitudeOrder()
    {
        var maps = Started();
        Assert.IsTrue(maps.CentreMapOn(new Geolocation(48.8566, 2.3522), 12));
        Assert.IsTrue(maps.UpdateOrbitValue(45));

        using JsonDocument camera = JsonDocument.Parse(maps.Scene.BuildCamera());
        Assert.AreEqual(2.3522, camera.RootElement.GetProperty("center")[0].GetDouble());
        Assert.AreEqual(48.8566, camera.RootElement.GetProperty("center")[1].GetDouble());
        Assert.AreEqual(12, camera.RootElement.GetProperty("zoom").GetDouble());
        Assert.AreEqual(45, camera.RootElement.GetProperty("bearing").GetDouble());
    }
}
