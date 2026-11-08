using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Directions;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests;

[TestClass]
public class OSRMBoundedClientTests
{
    private const string Valid = "{\"code\":\"Ok\",\"routes\":[{\"distance\":12,\"duration\":4,\"geometry\":{\"type\":\"LineString\",\"coordinates\":[[13,52],[13.1,52.1]]},\"unknown\":true}],\"waypoints\":[{\"location\":[13,52]},{\"location\":[13.1,52.1]}]}";
    private sealed class Handler(string body, int status = 200) : HttpMessageHandler
    {
        public int Calls;
        public string? Url;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Calls++; Url = request.RequestUri!.AbsoluteUri;
            Assert.IsTrue(request.Headers.UserAgent.ToString().Contains("OASIS-GOMapOASIS"));
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { RequestMessage = request,
                Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
    private static Task<NextGenSoftware.OASIS.Common.OASISResult<OSRMRouteObservation>> Read(OSRMDirectionsProvider p) =>
        p.GetRouteAsync(new Geolocation(52, 13), new Geolocation(52.1, 13.1), RoutingType.Driving);

    [DataTestMethod]
    [DataRow(RoutingType.Driving, "driving")]
    [DataRow(RoutingType.Cycling, "cycling")]
    [DataRow(RoutingType.Walking, "foot")]
    public async Task ExactRequestAndCompleteObservation(RoutingType type, string profile)
    {
        using var handler = new Handler(Valid); using var client = new HttpClient(handler);
        var p = new OSRMDirectionsProvider(client);
        var result = await p.GetRouteAsync(new Geolocation(52, 13), new Geolocation(52.1, 13.1), type);
        Assert.IsFalse(result.IsError, result.Message); Assert.AreEqual(1, handler.Calls);
        StringAssert.Contains(handler.Url!, "/route/v1/" + profile + "/13,52;13.1,52.1?");
        StringAssert.Contains(handler.Url!, "alternatives=false");
        Assert.AreEqual(Valid, result.Result.RawJson); Assert.AreEqual(64, result.Result.ResponseSha256.Length);
        Assert.AreEqual(2, result.Result.Geometry.Count); Assert.AreEqual(52, result.Result.Geometry[0].Latitude);
        Assert.AreEqual(12d, result.Result.DistanceMetres);
    }

    [DataTestMethod]
    [DataRow("empty")][DataRow("missing-code")][DataRow("bad-code")][DataRow("duplicate")]
    [DataRow("empty-routes")][DataRow("two-routes")][DataRow("missing-distance")][DataRow("negative-distance")]
    [DataRow("wrong-geometry")][DataRow("one-point")][DataRow("bad-pair")][DataRow("one-waypoint")]
    public async Task MalformedNativeResponseIsAnExplicitError(string mutation)
    {
        var body = mutation switch {
            "empty" => "{}", "missing-code" => Valid.Replace("\"code\":\"Ok\",", ""),
            "bad-code" => Valid.Replace("\"Ok\"", "\"NoRoute\""),
            "duplicate" => Valid.Replace("\"code\":\"Ok\"", "\"code\":\"Ok\",\"code\":\"Ok\""),
            "empty-routes" => Valid.Replace("\"routes\":[{", "\"routes\":[],\"discard\":[{"),
            "two-routes" => Valid.Replace("\"routes\":[{", "\"routes\":[{}, {"),
            "missing-distance" => Valid.Replace("\"distance\":12,", ""),
            "negative-distance" => Valid.Replace("\"distance\":12", "\"distance\":-1"),
            "wrong-geometry" => Valid.Replace("LineString", "Point"),
            "one-point" => Valid.Replace("[[13,52],[13.1,52.1]]", "[[13,52]]"),
            "bad-pair" => Valid.Replace("[[13,52],[13.1,52.1]]", "[[13,52],[13.1]]"),
            "one-waypoint" => Valid.Replace(",{\"location\":[13.1,52.1]}", ""),
            _ => throw new ArgumentException(mutation) };
        using var handler = new Handler(body); using var client = new HttpClient(handler);
        var result = await Read(new OSRMDirectionsProvider(client));
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual(1, handler.Calls);
    }

    [TestMethod]
    public async Task OversizedBodyIsRejected()
    {
        using var handler = new Handler(new string(' ', 262145)); using var client = new HttpClient(handler);
        Assert.IsTrue((await Read(new OSRMDirectionsProvider(client))).IsError); Assert.AreEqual(1, handler.Calls);
    }
    [DataTestMethod]
    [DataRow(401)][DataRow(403)][DataRow(429)][DataRow(503)]
    public async Task HttpErrorNeverRetries(int status)
    {
        using var handler = new Handler(Valid, status); using var client = new HttpClient(handler);
        var result = await Read(new OSRMDirectionsProvider(client));
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, status.ToString()); Assert.AreEqual(1, handler.Calls);
    }
    [TestMethod]
    public async Task PrecancelledReadDoesNotDispatch()
    {
        using var handler = new Handler(Valid); using var client = new HttpClient(handler);
        using var c = new CancellationTokenSource(); c.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => new OSRMDirectionsProvider(client).GetRouteAsync(
            new Geolocation(52,13), new Geolocation(52.1,13.1), cancellationToken:c.Token));
        Assert.AreEqual(0, handler.Calls);
    }
    [DataTestMethod]
    [DataRow(double.NaN, 13d)][DataRow(91d, 13d)][DataRow(52d, 181d)]
    public async Task InvalidInputDoesNotDispatch(double latitude, double longitude)
    {
        using var handler = new Handler(Valid); using var client = new HttpClient(handler);
        var result = await new OSRMDirectionsProvider(client).GetRouteAsync(new Geolocation(latitude,longitude), new Geolocation(52,13));
        Assert.IsTrue(result.IsError); Assert.AreEqual(0,handler.Calls);
    }
    [TestMethod]
    public async Task LegacyDirectionsReportsTheSameValidationFailure()
    {
        using var handler = new Handler("{}"); using var client = new HttpClient(handler); var p = new OSRMDirectionsProvider(client);
        var route = await p.GetDirectionsAsync(new Geolocation(52,13),new Geolocation(52.1,13.1));
        Assert.AreEqual(0,route.Count); Assert.IsNotNull(p.LastError); Assert.AreEqual(1,handler.Calls);
    }
    [TestMethod]
    public async Task CredentialBearingEndpointDoesNotDispatch()
    {
        using var handler = new Handler(Valid); using var client = new HttpClient(handler);
        var result = await Read(new OSRMDirectionsProvider(client){BaseUrl="https://private:secret@example.com"});
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.Message.Contains("private:secret")); Assert.AreEqual(0,handler.Calls);
    }
}
