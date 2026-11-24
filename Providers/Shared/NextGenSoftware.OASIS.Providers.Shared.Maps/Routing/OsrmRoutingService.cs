using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Http;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Routing
{
    /// <summary>
    /// OSRM HTTP route service (<c>/route/v1/{profile}/{lon,lat;lon,lat}</c>). The public
    /// demo server is keyless but non-commercial and limited to one request per second;
    /// point <see cref="BaseUrl"/> at a self-hosted OSRM for production volume.
    /// </summary>
    public sealed class OsrmRoutingService : MapRoutingServiceBase
    {
        public const string PublicDemoBaseUrl = "https://router.project-osrm.org";
        private static readonly RequestIntervalGate PublicDemoGate = new RequestIntervalGate(TimeSpan.FromSeconds(1));

        private readonly MapHttpClient _http;

        public OsrmRoutingService(string baseUrl = PublicDemoBaseUrl, HttpClient client = null)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("An absolute HTTPS OSRM base URL is required.", nameof(baseUrl));
            BaseUrl = baseUrl.TrimEnd('/');
            _http = new MapHttpClient("OSRM", client);
        }

        public override string Name => "osrm";
        public string BaseUrl { get; }

        protected override async Task<OASISResult<MapRoute>> RequestRouteAsync(Geolocation from, Geolocation to,
            RoutingType routingType, CancellationToken cancellationToken)
        {
            string profile = routingType == RoutingType.Driving ? "driving" : routingType == RoutingType.Cycling ? "cycling" : "foot";
            string url = BaseUrl + "/route/v1/" + profile + "/" + LonLat(from) + ";" + LonLat(to)
                       + "?overview=full&geometries=geojson&alternatives=false&steps=false";

            if (string.Equals(BaseUrl, PublicDemoBaseUrl, StringComparison.OrdinalIgnoreCase))
                await PublicDemoGate.WaitAsync(cancellationToken).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            OASISResult<JsonDocument> sent = await _http.SendAsync(request, ReadError, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<MapRoute>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement, routingType);
        }

        internal static OASISResult<MapRoute> Parse(JsonElement root, RoutingType routingType)
        {
            string code = root.TryGetProperty("code", out JsonElement c) ? c.GetString() : null;
            if (code != "Ok") return MapResults.Error<MapRoute>("OSRM could not route: " + (ReadError(root) ?? code ?? "no code"));

            if (!root.TryGetProperty("routes", out JsonElement routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)
                return MapResults.Error<MapRoute>("OSRM returned no route.");

            JsonElement route = routes[0];
            JsonElement coordinates = route.GetProperty("geometry").GetProperty("coordinates");
            var points = new List<Geolocation>();

            foreach (JsonElement pair in coordinates.EnumerateArray())
            {
                var point = new Geolocation(pair[1].GetDouble(), pair[0].GetDouble());
                if (!GeoMath.IsValid(point)) return MapResults.Error<MapRoute>("OSRM returned an invalid coordinate.");
                points.Add(point);
            }

            if (points.Count < 2) return MapResults.Error<MapRoute>("OSRM returned a route with fewer than two points.");

            return MapResults.Ok(new MapRoute(points, route.GetProperty("distance").GetDouble(),
                route.GetProperty("duration").GetDouble(), routingType, "osrm"));
        }

        private static string ReadError(JsonElement root) =>
            root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out JsonElement m) ? m.GetString() : null;

        private static string LonLat(Geolocation p) =>
            p.Longitude.ToString("R", CultureInfo.InvariantCulture) + "," + p.Latitude.ToString("R", CultureInfo.InvariantCulture);
    }
}
