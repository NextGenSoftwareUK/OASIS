using System.Net;
using System.Text;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.UnitTests;

[TestClass]
public sealed class MapboxServicesTests
{
    [TestMethod]
    public async Task DirectionsRequestUsesProfileGeoJsonAndToken()
    {
        var http = new CannedHandler(HttpStatusCode.OK,
            "{\"code\":\"Ok\",\"routes\":[{\"distance\":1873.4,\"duration\":402.1,\"geometry\":{\"type\":\"LineString\",\"coordinates\":[[13.38886,52.517037],[13.39,52.52],[13.397634,52.529407]]}}],\"waypoints\":[]}");
        var service = new MapboxRoutingService("pk.test", new HttpClient(http));

        var result = await service.GetRouteAsync(new Geolocation(52.517037, 13.38886), new Geolocation(52.529407, 13.397634), RoutingType.Cycling);

        Assert.IsFalse(result.IsError, result.Message);
        Uri uri = http.Requests.Single();
        StringAssert.StartsWith(uri.GetLeftPart(UriPartial.Path), "https://api.mapbox.com/directions/v5/mapbox/cycling/13.38886,52.517037;13.397634,52.529407");
        var query = HttpUtility.ParseQueryString(uri.Query);
        Assert.AreEqual("geojson", query["geometries"]);
        Assert.AreEqual("pk.test", query["access_token"]);
        Assert.AreEqual(3, result.Result.Geometry.Count);
        Assert.AreEqual(1873.4, result.Result.DistanceMetres);
        Assert.AreEqual(402.1, result.Result.DurationSeconds);
    }

    [DataTestMethod]
    [DataRow(RoutingType.Walking, "mapbox/walking")]
    [DataRow(RoutingType.Driving, "mapbox/driving")]
    [DataRow(RoutingType.Cycling, "mapbox/cycling")]
    public void TravelModesMapToMapboxProfiles(RoutingType type, string expected)
    {
        Assert.AreEqual(expected, MapboxRoutingService.Profile(type));
    }

    [TestMethod]
    public async Task NoRouteIsReportedWithMapboxCodeAndMessage()
    {
        var http = new CannedHandler(HttpStatusCode.OK,
            "{\"code\":\"NoRoute\",\"message\":\"There was no route found for the given coordinates.\",\"routes\":[]}");
        var result = await new MapboxRoutingService("pk.test", new HttpClient(http))
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Driving);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "NoRoute: There was no route found");
    }

    [TestMethod]
    public async Task InvalidTokenErrorsCarryMapboxMessage()
    {
        var http = new CannedHandler(HttpStatusCode.Unauthorized, "{\"message\":\"Not Authorized - Invalid Token\"}");
        var result = await new MapboxRoutingService("pk.bad", new HttpClient(http))
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Walking);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "HTTP 401: Not Authorized - Invalid Token");
    }

    [TestMethod]
    public async Task MissingTokenIsReportedWithoutCallingMapbox()
    {
        var http = new CannedHandler(HttpStatusCode.OK, "{}");
        var result = await new MapboxRoutingService(null, new HttpClient(http))
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Walking);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "OASIS_MAPBOX_ACCESS_TOKEN");
        Assert.AreEqual(0, http.Requests.Count);
    }

    [TestMethod]
    public async Task GeocodingV6SendsProximityLongitudeFirstAndParsesFeatures()
    {
        var http = new CannedHandler(HttpStatusCode.OK,
            "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"id\":\"dXJu\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[-77.03655,38.89768]}," +
            "\"properties\":{\"mapbox_id\":\"dXJu\",\"feature_type\":\"address\",\"name\":\"1600 Pennsylvania Avenue Northwest\",\"place_formatted\":\"Washington, District of Columbia 20500, United States\"}}],\"attribution\":\"x\"}");
        var service = new MapboxPlaceSearchService("pk.test", new HttpClient(http));

        var result = await service.SearchAsync(new MapPlaceSearchRequest("1600 pennsylvania", new Geolocation(38.9, -77.0), 25));

        Assert.IsFalse(result.IsError, result.Message);
        var query = HttpUtility.ParseQueryString(http.Requests.Single().Query);
        Assert.AreEqual("-77,38.9", query["proximity"]);
        Assert.AreEqual("10", query["limit"], "v6 forward returns at most ten results");

        MapPointOfInterest place = result.Result.Single();
        Assert.AreEqual("1600 Pennsylvania Avenue Northwest", place.Name);
        Assert.AreEqual("1600 Pennsylvania Avenue Northwest, Washington, District of Columbia 20500, United States", place.Address);
        Assert.AreEqual("address", place.Category);
        Assert.AreEqual(38.89768, place.Location.Latitude);
        Assert.AreEqual("mapbox-geocoding", place.Source);
    }

    [TestMethod]
    public void ProviderUsesTheMapboxServices()
    {
        var maps = new MapBoxOASIS("pk.test");

        Assert.IsInstanceOfType(maps.DirectionsAPI, typeof(MapboxRoutingService));
        Assert.IsInstanceOfType(maps.GeocodingProvider, typeof(MapboxPlaceSearchService));
    }

    private sealed class CannedHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public CannedHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public List<Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri);
            return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
        }
    }
}
