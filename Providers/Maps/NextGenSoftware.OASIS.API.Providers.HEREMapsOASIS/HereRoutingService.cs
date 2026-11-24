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

namespace NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS
{
    /// <summary>HERE Routing API v8: GET https://router.hereapi.com/v8/routes?return=polyline,summary.</summary>
    public sealed class HereRoutingService : MapRoutingServiceBase
    {
        public const string Endpoint = "https://router.hereapi.com/v8/routes";

        private readonly string _apiKey;
        private readonly MapHttpClient _http;

        public HereRoutingService(string apiKey, HttpClient client = null)
        {
            _apiKey = HereApiKey.Normalise(apiKey);
            _http = new MapHttpClient("HERE Routing API", client);
        }

        public override string Name => "here-routing";

        public static string TransportMode(RoutingType routingType) => routingType switch
        {
            RoutingType.Driving => "car",
            RoutingType.Cycling => "bicycle",
            _ => "pedestrian"
        };

        protected override async Task<OASISResult<MapRoute>> RequestRouteAsync(Geolocation from, Geolocation to,
            RoutingType routingType, CancellationToken cancellationToken)
        {
            if (_apiKey == null) return MapResults.Error<MapRoute>(HereApiKey.MissingMessage("HERE Routing API"));

            string url = Endpoint + "?transportMode=" + TransportMode(routingType)
                       + "&origin=" + LatLng(from) + "&destination=" + LatLng(to)
                       + "&return=polyline,summary&apiKey=" + Uri.EscapeDataString(_apiKey);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            OASISResult<JsonDocument> sent = await _http.SendAsync(request, HereErrors.Read, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<MapRoute>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement, routingType);
        }

        internal static OASISResult<MapRoute> Parse(JsonElement root, RoutingType routingType)
        {
            if (!root.TryGetProperty("routes", out JsonElement routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)
                return MapResults.Error<MapRoute>("HERE Routing API found no route" + Notices(root) + ".");

            var geometry = new List<Geolocation>();
            double distance = 0, duration = 0;

            // Each section is one leg or transport mode; its polyline is encoded separately.
            foreach (JsonElement section in routes[0].GetProperty("sections").EnumerateArray())
            {
                if (!section.TryGetProperty("polyline", out JsonElement polyline))
                    return MapResults.Error<MapRoute>("HERE Routing API returned a section without geometry.");

                List<Geolocation> points = FlexiblePolylineCodec.Decode(polyline.GetString());
                int skip = geometry.Count > 0 && points.Count > 0 && Same(geometry[geometry.Count - 1], points[0]) ? 1 : 0;
                for (int i = skip; i < points.Count; i++) geometry.Add(points[i]);

                JsonElement summary = section.GetProperty("summary");
                distance += summary.GetProperty("length").GetDouble();
                duration += summary.GetProperty("duration").GetDouble();
            }

            if (geometry.Count < 2) return MapResults.Error<MapRoute>("HERE Routing API returned a route with fewer than two points.");
            return MapResults.Ok(new MapRoute(geometry, distance, duration, routingType, "here-routing"));
        }

        private static string Notices(JsonElement root)
        {
            if (!root.TryGetProperty("notices", out JsonElement notices) || notices.ValueKind != JsonValueKind.Array) return string.Empty;

            var titles = new List<string>();
            foreach (JsonElement notice in notices.EnumerateArray())
                if (notice.TryGetProperty("title", out JsonElement title)) titles.Add(title.GetString());
            return titles.Count == 0 ? string.Empty : ": " + string.Join("; ", titles);
        }

        private static bool Same(Geolocation a, Geolocation b) =>
            Math.Abs(a.Latitude - b.Latitude) < 1e-9 && Math.Abs(a.Longitude - b.Longitude) < 1e-9;

        private static string LatLng(Geolocation p) =>
            p.Latitude.ToString("R", CultureInfo.InvariantCulture) + "," + p.Longitude.ToString("R", CultureInfo.InvariantCulture);
    }
}
