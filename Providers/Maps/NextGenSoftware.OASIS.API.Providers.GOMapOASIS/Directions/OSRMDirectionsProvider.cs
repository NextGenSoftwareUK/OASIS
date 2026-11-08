using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Directions
{
    /// <summary>One bounded OSRM route request. Public demo use is non-commercial and at most one request/second.</summary>
    public class OSRMDirectionsProvider : IDirectionsAPIProvider
    {
        public const string DefaultBaseUrl = "https://router.project-osrm.org";
        private static readonly HttpClient SharedClient = new HttpClient(new SocketsHttpHandler {
            AllowAutoRedirect = false, UseCookies = false, ConnectTimeout = TimeSpan.FromSeconds(5)
        }) { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly SemaphoreSlim RequestGate = new SemaphoreSlim(1, 1);
        private static long lastRequest;
        private readonly HttpClient client;
        public string BaseUrl { get; set; } = DefaultBaseUrl;
        public string LastError { get; private set; }
        public OSRMDirectionsProvider() : this(null) { }
        public OSRMDirectionsProvider(HttpClient client) { this.client = client ?? SharedClient; }

        // The legacy interface is a projection of the same validated operation, never an alternate request path.
        public async Task<List<Geolocation>> GetDirectionsAsync(Geolocation startPoint, Geolocation endPoint,
            RoutingType routingType = RoutingType.Walking)
        {
            var result = await GetRouteAsync(startPoint, endPoint, routingType).ConfigureAwait(false);
            LastError = result.IsError ? result.Message : null;
            return result.IsError ? new List<Geolocation>() : new List<Geolocation>(result.Result.Geometry);
        }

        public async Task<OASISResult<OSRMRouteObservation>> GetRouteAsync(Geolocation startPoint, Geolocation endPoint,
            RoutingType routingType = RoutingType.Walking, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new OASISResult<OSRMRouteObservation>();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var token = deadline.Token;
            try
            {
                if (!Valid(startPoint) || !Valid(endPoint) || !Enum.IsDefined(typeof(RoutingType), routingType))
                    throw new InvalidDataException("Valid WGS84 endpoints and a supported route type are required.");
                if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var origin) || origin.Scheme != "https" ||
                    origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0 || origin.AbsolutePath != "/")
                    throw new InvalidDataException("An explicit HTTPS OSRM origin without credentials, path or query is required.");
                var profile = routingType == RoutingType.Driving ? "driving" : routingType == RoutingType.Cycling ? "cycling" : "foot";
                var url = origin.GetLeftPart(UriPartial.Authority) + "/route/v1/" + profile + "/" +
                    Coordinates(startPoint) + ";" + Coordinates(endPoint) + "?overview=full&geometries=geojson&alternatives=false&steps=false&generate_hints=false";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("OASIS-GOMapOASIS/2.0 (+https://oasisomniverse.one)");
                request.Headers.Accept.ParseAdd("application/json");
                await RequestGate.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    // A single process-wide gate covers every client instance and the complete response lifetime.
                    var remaining = TimeSpan.FromSeconds(1) - Stopwatch.GetElapsedTime(lastRequest);
                    if (lastRequest != 0 && remaining > TimeSpan.Zero)
                        await Task.Delay(remaining, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    lastRequest = Stopwatch.GetTimestamp();
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new InvalidDataException("OSRM returned HTTP " + (int)response.StatusCode + ". No retry was made.");
                    if (response.RequestMessage?.RequestUri is Uri actual && actual != request.RequestUri)
                        throw new InvalidDataException("OSRM redirects are not an admitted route source.");
                    if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("OSRM must return JSON.");
                    if (response.Content.Headers.ContentLength > 262144)
                        throw new InvalidDataException("OSRM response exceeds the 256 KiB bound.");
                    using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    using var buffer = new MemoryStream();
                    var chunk = new byte[8192]; int count;
                    while ((count = await stream.ReadAsync(chunk.AsMemory(), token).ConfigureAwait(false)) != 0)
                    {
                        if (buffer.Length + count > 262144) throw new InvalidDataException("OSRM response exceeds the 256 KiB bound.");
                        buffer.Write(chunk, 0, count);
                    }
                    var bytes = buffer.ToArray();
                    var raw = new UTF8Encoding(false, true).GetString(bytes);
                    using var document = JsonDocument.Parse(raw, new JsonDocumentOptions { MaxDepth = 32 });
                    Unique(document.RootElement);
                    var root = document.RootElement;
                    if (root.GetProperty("code").GetString() != "Ok") throw new InvalidDataException("OSRM did not return an Ok route.");
                    var routes = root.GetProperty("routes");
                    if (routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() != 1)
                        throw new InvalidDataException("Exactly one non-alternative route is required.");
                    var route = routes[0];
                    var distance = Nonnegative(route.GetProperty("distance"));
                    var duration = Nonnegative(route.GetProperty("duration"));
                    var geometry = route.GetProperty("geometry");
                    if (geometry.GetProperty("type").GetString() != "LineString") throw new InvalidDataException("OSRM route must be a GeoJSON LineString.");
                    var coordinates = geometry.GetProperty("coordinates");
                    if (coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() < 2 || coordinates.GetArrayLength() > 4096)
                        throw new InvalidDataException("Bounded nonempty route geometry is required.");
                    var points = new List<Geolocation>();
                    foreach (var pair in coordinates.EnumerateArray()) points.Add(Point(pair));
                    var waypoints = root.GetProperty("waypoints");
                    if (waypoints.ValueKind != JsonValueKind.Array || waypoints.GetArrayLength() != 2)
                        throw new InvalidDataException("Both snapped endpoints are required.");
                    foreach (var waypoint in waypoints.EnumerateArray()) Point(waypoint.GetProperty("location"));
                    result.Result = new OSRMRouteObservation(points.AsReadOnly(), distance, duration, raw,
                        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), DateTimeOffset.UtcNow);
                    return result;
                }
                finally { RequestGate.Release(); }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is InvalidDataException || error is HttpRequestException || error is IOException ||
                error is JsonException || error is DecoderFallbackException || error is InvalidOperationException ||
                error is KeyNotFoundException || error is FormatException || error is OperationCanceledException)
            {
                result.IsError = true;
                result.Message = error is InvalidDataException ? error.Message : "OSRM route transport or response validation failed.";
                return result;
            }
        }
        private static bool Valid(Geolocation point) => point != null && double.IsFinite(point.Latitude) && double.IsFinite(point.Longitude)
            && point.Latitude >= -90 && point.Latitude <= 90 && point.Longitude >= -180 && point.Longitude <= 180;
        private static string Coordinates(Geolocation p) => p.Longitude.ToString("R", CultureInfo.InvariantCulture) + "," + p.Latitude.ToString("R", CultureInfo.InvariantCulture);
        private static double Nonnegative(JsonElement element)
        {
            var value = element.GetDouble();
            if (!double.IsFinite(value) || value < 0) throw new InvalidDataException("OSRM route metric is invalid.");
            return value;
        }
        private static Geolocation Point(JsonElement pair)
        {
            if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2) throw new InvalidDataException("OSRM coordinates must contain exactly longitude and latitude.");
            var point = new Geolocation(pair[1].GetDouble(), pair[0].GetDouble());
            if (!Valid(point)) throw new InvalidDataException("OSRM geometry contains invalid WGS84 coordinates.");
            return point;
        }
        private static void Unique(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate OSRM JSON properties are not permitted.");
                    Unique(property.Value);
                }
            }
            else if (value.ValueKind == JsonValueKind.Array) foreach (var item in value.EnumerateArray()) Unique(item);
        }
    }

    public sealed record OSRMRouteObservation(IReadOnlyList<Geolocation> Geometry, double DistanceMetres,
        double DurationSeconds, string RawJson, string ResponseSha256, DateTimeOffset ObservedUtc);
}
