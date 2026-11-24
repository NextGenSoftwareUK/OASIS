using System.Net;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapPlatforms.UnitTests;

[TestClass]
public sealed class OpenStreetMapServicesTests
{
    private const string SelfHosted = "https://osm.example.test";

    [TestMethod]
    public async Task OsrmUsesTheProfileAndParsesGeoJsonGeometry()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "{\"code\":\"Ok\",\"routes\":[{\"distance\":1500.5,\"duration\":300,\"geometry\":{\"type\":\"LineString\",\"coordinates\":[[13.38886,52.517037],[13.397634,52.529407]]}}]}");
        var service = new OsrmRoutingService(SelfHosted, http.Client());

        var result = await service.GetRouteAsync(new Geolocation(52.517037, 13.38886), new Geolocation(52.529407, 13.397634), RoutingType.Cycling);

        Assert.IsFalse(result.IsError, result.Message);
        StringAssert.StartsWith(http.Requests.Single().RequestUri.ToString(), SelfHosted + "/route/v1/cycling/13.38886,52.517037;13.397634,52.529407?");
        Assert.AreEqual(52.517037, result.Result.Geometry[0].Latitude);
        Assert.AreEqual(1500.5, result.Result.DistanceMetres);
    }

    [TestMethod]
    public async Task OsrmNoRouteIsAnError()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.BadRequest,
            "{\"code\":\"NoRoute\",\"message\":\"Impossible route between points\"}");
        var result = await new OsrmRoutingService(SelfHosted, http.Client())
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Walking);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "Impossible route between points");
    }

    [TestMethod]
    public void OsrmRequiresHttps()
    {
        Assert.ThrowsException<ArgumentException>(() => new OsrmRoutingService("http://insecure.example.test"));
    }

    [TestMethod]
    public async Task NominatimBiasesSearchesAndParsesStringCoordinates()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "[{\"osm_type\":\"way\",\"osm_id\":123,\"lat\":\"51.5007\",\"lon\":\"-0.1246\",\"name\":\"Big Ben\",\"display_name\":\"Big Ben, Westminster, London\",\"type\":\"clock\"}]");
        var service = new NominatimPlaceSearchService(SelfHosted, http.Client());

        var result = await service.SearchAsync(new MapPlaceSearchRequest("big ben", new Geolocation(51.5, -0.12), 3));

        Assert.IsFalse(result.IsError, result.Message);
        var query = HttpUtility.ParseQueryString(http.Requests.Single().RequestUri.Query);
        Assert.AreEqual("jsonv2", query["format"]);
        Assert.AreEqual("3", query["limit"]);
        Assert.IsNotNull(query["viewbox"]);
        Assert.AreEqual("Big Ben", result.Result.Single().Name);
        Assert.AreEqual("way/123", result.Result.Single().Id);
    }

    [TestMethod]
    public async Task OversizedResponsesAreRejected()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK, "[" + new string(' ', 5 * 1024 * 1024) + "]");
        var result = await new NominatimPlaceSearchService(SelfHosted, http.Client()).SearchAsync(new MapPlaceSearchRequest("x"));

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "exceeds");
    }

    [TestMethod]
    public async Task NonJsonResponsesAreRejected()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK, "<html></html>", "text/html");
        var result = await new NominatimPlaceSearchService(SelfHosted, http.Client()).SearchAsync(new MapPlaceSearchRequest("x"));

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "instead of JSON");
    }
}
