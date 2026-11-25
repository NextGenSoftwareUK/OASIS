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
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS
{
    /// <summary>Mapbox Directions API v5: GET https://api.mapbox.com/directions/v5/{profile}/{coordinates}.</summary>
    public sealed class MapboxRoutingService : MapRoutingServiceBase
    {
        public const string Endpoint = "https://api.mapbox.com/directions/v5/";

        private readonly string _accessToken;
        private readonly MapHttpClient _http;

        public MapboxRoutingService(string accessToken, HttpClient client = null)
        {
            _accessToken = MapboxAccessToken.Normalise(accessToken);
            _http = new MapHttpClient("Mapbox Directions API", client);
        }

        public override string Name => "mapbox-directions";

        public static string Profile(RoutingType routingType) => routingType switch
        {
            RoutingType.Driving => "mapbox/driving",
            RoutingType.Cycling => "mapbox/cycling",
            _ => "mapbox/walking"
        };

        protected override async Task<OASISResult<MapRoute>> RequestRouteAsync(Geolocation from, Geolocation to,
            RoutingType routingType, CancellationToken cancellationToken)
        {
            if (_accessToken == null) return MapResults.Error<MapRoute>(MapboxAccessToken.MissingMessage("Mapbox Directions API"));

            string url = Endpoint + Profile(routingType) + "/" + LonLat(from) + ";" + LonLat(to)
                       + "?geometries=geojson&overview=full&steps=false&alternatives=false&access_token="
                       + Uri.EscapeDataString(_accessToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            OASISResult<JsonDocument> sent = await _http.SendAsync(request, MapboxErrors.Read, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<MapRoute>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement, routingType);
        }

        internal static OASISResult<MapRoute> Parse(JsonElement root, RoutingType routingType)
        {
            string code = root.TryGetProperty("code", out JsonElement c) ? c.GetString() : null;
            if (code != "Ok")
                return MapResults.Error<MapRoute>("Mapbox Directions API could not route: " + (MapboxErrors.Read(root) ?? code ?? "no code") + ".");

            if (!root.TryGetProperty("routes", out JsonElement routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)
                return MapResults.Error<MapRoute>("Mapbox Directions API returned no route.");

            JsonElement route = routes[0];
            JsonElement geometry = route.GetProperty("geometry");
            if (geometry.GetProperty("type").GetString() != "LineString")
                return MapResults.Error<MapRoute>("Mapbox Directions API returned geometry that is not a GeoJSON LineString.");

            var points = new List<Geolocation>();
            foreach (JsonElement pair in geometry.GetProperty("coordinates").EnumerateArray())
            {
                var point = new Geolocation(pair[1].GetDouble(), pair[0].GetDouble());
                if (!GeoMath.IsValid(point)) return MapResults.Error<MapRoute>("Mapbox Directions API returned an invalid coordinate.");
                points.Add(point);
            }

            if (points.Count < 2) return MapResults.Error<MapRoute>("Mapbox Directions API returned a route with fewer than two points.");

            return MapResults.Ok(new MapRoute(points, route.GetProperty("distance").GetDouble(),
                route.GetProperty("duration").GetDouble(), routingType, "mapbox-directions"));
        }

        private static string LonLat(Geolocation p) =>
            p.Longitude.ToString("R", CultureInfo.InvariantCulture) + "," + p.Latitude.ToString("R", CultureInfo.InvariantCulture);
    }
}
