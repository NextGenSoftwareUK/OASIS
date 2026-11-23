using System.Net;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapPlatforms.UnitTests;

[TestClass]
public sealed class HereMapsTests
{
    [TestMethod]
    public async Task RoutingRequestsPolylineAndSummaryAndJoinsSections()
    {
        string v = PolylineCodecTests.HereVector;
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "{\"routes\":[{\"sections\":[" +
            "{\"polyline\":\"" + v + "\",\"summary\":{\"length\":1206,\"duration\":243}}," +
            "{\"polyline\":\"" + v + "\",\"summary\":{\"length\":10,\"duration\":7}}]}]}");
        var service = new HereRoutingService("here-key", http.Client());

        var result = await service.GetRouteAsync(new Geolocation(50.10228, 8.69821), new Geolocation(50.09878, 8.68752), RoutingType.Walking);

        Assert.IsFalse(result.IsError, result.Message);
        var query = HttpUtility.ParseQueryString(http.Requests.Single().RequestUri.Query);
        Assert.AreEqual("pedestrian", query["transportMode"]);
        Assert.AreEqual("50.10228,8.69821", query["origin"]);
        Assert.AreEqual("polyline,summary", query["return"]);
        Assert.AreEqual("here-key", query["apiKey"]);

        Assert.AreEqual(1216, result.Result.DistanceMetres);
        Assert.AreEqual(250, result.Result.DurationSeconds);
        Assert.AreEqual(8, result.Result.Geometry.Count);
    }

    [DataTestMethod]
    [DataRow(RoutingType.Walking, "pedestrian")]
    [DataRow(RoutingType.Cycling, "bicycle")]
    [DataRow(RoutingType.Driving, "car")]
    public void TransportModesMapToHereValues(RoutingType type, string expected)
    {
        Assert.AreEqual(expected, HereRoutingService.TransportMode(type));
    }

    [TestMethod]
    public async Task RoutingErrorsCarryHereTitleAndCause()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.BadRequest,
            "{\"title\":\"Malformed request\",\"status\":400,\"code\":\"E605001\",\"cause\":\"Invalid origin\"}");
        var result = await new HereRoutingService("k", http.Client())
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Driving);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "Malformed request: Invalid origin");
    }

    [TestMethod]
    public async Task NoRouteReportsHereNotices()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "{\"routes\":[],\"notices\":[{\"title\":\"Route calculation failed: Couldn't find a route.\"}]}");
        var result = await new HereRoutingService("k", http.Client())
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Driving);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "Couldn't find a route");
    }

    [TestMethod]
    public async Task SearchNearAPointUsesDiscover()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "{\"items\":[{\"title\":\"Statue of Liberty\",\"id\":\"here:pds:place:1\",\"resultType\":\"place\",\"address\":{\"label\":\"Liberty Island\"},\"position\":{\"lat\":40.68925,\"lng\":-74.04445},\"distance\":6094,\"categories\":[{\"name\":\"Historical Monument\"}]}]}");
        var service = new HereSearchService("k", http.Client());

        var result = await service.SearchAsync(new MapPlaceSearchRequest("Statue of liberty", new Geolocation(40.7307999, -73.9973085), 2));

        Assert.IsFalse(result.IsError, result.Message);
        Uri uri = http.Requests.Single().RequestUri;
        Assert.AreEqual("discover.search.hereapi.com", uri.Host);
        Assert.AreEqual("40.7307999,-73.9973085", HttpUtility.ParseQueryString(uri.Query)["at"]);

        MapPointOfInterest place = result.Result.Single();
        Assert.AreEqual("Statue of Liberty", place.Name);
        Assert.AreEqual("Historical Monument", place.Category);
        Assert.AreEqual(6094f, place.Distance);
    }

    [TestMethod]
    public async Task SearchWithoutAPointUsesGeocode()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK, "{\"items\":[]}");
        await new HereSearchService("k", http.Client()).SearchAsync(new MapPlaceSearchRequest("Invalidenstr 117 Berlin"));

        Assert.AreEqual("geocode.search.hereapi.com", http.Requests.Single().RequestUri.Host);
    }

    [TestMethod]
    public async Task AMissingKeyIsReportedWithoutCallingHere()
    {
        var http = new FakeHttpHandler();
        var result = await new HereSearchService(null, http.Client()).SearchAsync(new MapPlaceSearchRequest("x"));

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "OASIS_HERE_ROUTING_API_KEY");
        Assert.AreEqual(0, http.Requests.Count);
    }

    [TestMethod]
    public async Task ContractSearchReportsFailureThroughLastError()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.Unauthorized,
            "{\"error\":\"Unauthorized\",\"error_description\":\"apiKey invalid\"}");
        var service = new HereSearchService("k", http.Client());

        ForwardGeocodingResponse<IPointOfInterest> response = await service.POISearchAsync(new MapPlaceSearchRequest("x"));

        Assert.IsFalse(response.Success);
        StringAssert.Contains(service.LastError, "Unauthorized: apiKey invalid");
    }
}
