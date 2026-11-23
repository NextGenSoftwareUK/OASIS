using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapPlatforms.UnitTests;

[TestClass]
public sealed class GoogleMapsTests
{
    [TestMethod]
    public async Task RoutesRequestFollowsTheComputeRoutesContract()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "{\"routes\":[{\"distanceMeters\":772,\"duration\":\"165s\",\"polyline\":{\"encodedPolyline\":\"" + PolylineCodecTests.GoogleVector + "\"}}]}");
        var service = new GoogleRoutesService("test-key", http.Client());

        var result = await service.GetRouteAsync(new Geolocation(38.5, -120.2), new Geolocation(43.252, -126.453), RoutingType.Driving);

        Assert.IsFalse(result.IsError, result.Message);
        HttpRequestMessage request = http.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual(GoogleRoutesService.Endpoint, request.RequestUri.ToString());
        Assert.AreEqual("test-key", request.Headers.GetValues("X-Goog-Api-Key").Single());
        StringAssert.Contains(request.Headers.GetValues("X-Goog-FieldMask").Single(), "routes.polyline.encodedPolyline");

        using JsonDocument body = JsonDocument.Parse(http.RequestBodies.Single());
        Assert.AreEqual("DRIVE", body.RootElement.GetProperty("travelMode").GetString());
        Assert.AreEqual(38.5, body.RootElement.GetProperty("origin").GetProperty("location").GetProperty("latLng").GetProperty("latitude").GetDouble());

        Assert.AreEqual(772, result.Result.DistanceMetres);
        Assert.AreEqual(165, result.Result.DurationSeconds);
        Assert.AreEqual(3, result.Result.Geometry.Count);
    }

    [DataTestMethod]
    [DataRow(RoutingType.Walking, "WALK")]
    [DataRow(RoutingType.Cycling, "BICYCLE")]
    [DataRow(RoutingType.Driving, "DRIVE")]
    public void TravelModesMapToGoogleEnums(RoutingType type, string expected)
    {
        Assert.AreEqual(expected, GoogleRoutesService.TravelMode(type));
    }

    [TestMethod]
    public async Task GoogleErrorsCarryTheirStatusAndMessage()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.Forbidden,
            "{\"error\":{\"code\":403,\"message\":\"Routes API has not been used in project 1\",\"status\":\"PERMISSION_DENIED\"}}");

        var result = await new GoogleRoutesService("test-key", http.Client())
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Walking);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "HTTP 403");
        StringAssert.Contains(result.Message, "PERMISSION_DENIED");
    }

    [TestMethod]
    public async Task AnEmptyRoutesResponseIsAnError()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK, "{}");
        var result = await new GoogleRoutesService("k", http.Client())
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Walking);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "no route");
    }

    [TestMethod]
    public async Task AMissingKeyIsReportedWithoutCallingGoogle()
    {
        var http = new FakeHttpHandler();
        var result = await new GoogleRoutesService(null, http.Client())
            .GetRouteAsync(new Geolocation(1, 1), new Geolocation(2, 2), RoutingType.Walking);

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "OASIS_GOOGLE_ROUTES_API_KEY");
        Assert.AreEqual(0, http.Requests.Count);
    }

    [TestMethod]
    public async Task PlacesSearchSendsTextQueryAndBiasAndParsesPlaces()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "{\"places\":[{\"id\":\"p1\",\"displayName\":{\"text\":\"Cafe\"},\"formattedAddress\":\"1 High St\",\"location\":{\"latitude\":51.51,\"longitude\":-0.12},\"primaryType\":\"cafe\"}]}");
        var service = new GooglePlacesSearchService("k", http.Client());

        var result = await service.SearchAsync(new MapPlaceSearchRequest("coffee", new Geolocation(51.5, -0.12), 5) { BiasRadiusMetres = 90000 });

        Assert.IsFalse(result.IsError, result.Message);
        using JsonDocument body = JsonDocument.Parse(http.RequestBodies.Single());
        Assert.AreEqual("coffee", body.RootElement.GetProperty("textQuery").GetString());
        Assert.AreEqual(5, body.RootElement.GetProperty("pageSize").GetInt32());
        Assert.AreEqual(50000, body.RootElement.GetProperty("locationBias").GetProperty("circle").GetProperty("radius").GetDouble());

        MapPointOfInterest place = result.Result.Single();
        Assert.AreEqual("Cafe", place.Name);
        Assert.AreEqual("cafe", place.Category);
        Assert.AreEqual("google-places", place.Source);
        Assert.IsTrue(place.Distance > 1000 && place.Distance < 1200, "Distance " + place.Distance);
    }

    [TestMethod]
    public async Task PlacesWithNoMatchesReturnAnEmptyList()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK, "{}");
        var result = await new GooglePlacesSearchService("k", http.Client()).SearchAsync(new MapPlaceSearchRequest("zzzz"));

        Assert.IsFalse(result.IsError);
        Assert.AreEqual(0, result.Result.Count);
    }

    [TestMethod]
    public async Task GeocodingParsesResultsAndTreatsZeroResultsAsEmpty()
    {
        var http = new FakeHttpHandler()
            .Respond(HttpStatusCode.OK, "{\"status\":\"OK\",\"results\":[{\"place_id\":\"x\",\"formatted_address\":\"Mountain View, CA\",\"types\":[\"street_address\"],\"geometry\":{\"location\":{\"lat\":37.42,\"lng\":-122.08}}}]}")
            .Respond(HttpStatusCode.OK, "{\"status\":\"ZERO_RESULTS\",\"results\":[]}");
        var service = new GoogleGeocodingService("k", http.Client());

        var found = await service.GeocodeAsync("1600 Amphitheatre Parkway");
        var none = await service.GeocodeAsync("nowhere");

        Assert.AreEqual(37.42, found.Result.Single().Location.Latitude);
        StringAssert.Contains(http.Requests[0].RequestUri.Query, "address=1600%20Amphitheatre%20Parkway");
        Assert.IsFalse(none.IsError);
        Assert.AreEqual(0, none.Result.Count);
    }

    [TestMethod]
    public async Task GeocodingDenialIsAnError()
    {
        var http = new FakeHttpHandler().Respond(HttpStatusCode.OK,
            "{\"status\":\"REQUEST_DENIED\",\"error_message\":\"The provided API key is invalid.\",\"results\":[]}");
        var result = await new GoogleGeocodingService("k", http.Client()).GeocodeAsync("x");

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "REQUEST_DENIED: The provided API key is invalid.");
    }

    [TestMethod]
    public void ProviderExposesTheContractServices()
    {
        var maps = new GoogleMapsOASIS.GoogleMapsOASIS("k");

        Assert.AreEqual(MapProviderType.GoogleMaps, maps.MapProviderType);
        Assert.IsInstanceOfType(maps.DirectionsAPI, typeof(GoogleRoutesService));
        Assert.IsInstanceOfType(maps.GeocodingProvider, typeof(GooglePlacesSearchService));
    }
}
