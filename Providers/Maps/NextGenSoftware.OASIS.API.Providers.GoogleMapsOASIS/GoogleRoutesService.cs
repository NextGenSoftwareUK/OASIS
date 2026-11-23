using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Http;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS
{
    /// <summary>Google Routes API: POST https://routes.googleapis.com/directions/v2:computeRoutes.</summary>
    public sealed class GoogleRoutesService : MapRoutingServiceBase
    {
        public const string Endpoint = "https://routes.googleapis.com/directions/v2:computeRoutes";
        private const string FieldMask = "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline";

        private readonly string _apiKey;
        private readonly MapHttpClient _http;

        public GoogleRoutesService(string apiKey, HttpClient client = null)
        {
            _apiKey = GoogleApiKey.Normalise(apiKey);
            _http = new MapHttpClient("Google Routes API", client);
        }

        public override string Name => "google-routes";

        public static string TravelMode(RoutingType routingType) => routingType switch
        {
            RoutingType.Driving => "DRIVE",
            RoutingType.Cycling => "BICYCLE",
            _ => "WALK"
        };

        protected override async Task<OASISResult<MapRoute>> RequestRouteAsync(Geolocation from, Geolocation to,
            RoutingType routingType, CancellationToken cancellationToken)
        {
            if (_apiKey == null) return MapResults.Error<MapRoute>(GoogleApiKey.MissingMessage("Google Routes API"));

            string body = JsonSerializer.Serialize(new
            {
                origin = Waypoint(from),
                destination = Waypoint(to),
                travelMode = TravelMode(routingType),
                polylineEncoding = "ENCODED_POLYLINE"
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add("X-Goog-FieldMask", FieldMask);

            OASISResult<JsonDocument> sent = await _http.SendAsync(request, GoogleErrors.Read, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<MapRoute>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement, routingType);
        }

        internal static OASISResult<MapRoute> Parse(JsonElement root, RoutingType routingType)
        {
            if (!root.TryGetProperty("routes", out JsonElement routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)
                return MapResults.Error<MapRoute>("Google Routes API found no route between the points.");

            JsonElement route = routes[0];
            if (!route.TryGetProperty("polyline", out JsonElement polyline) || !polyline.TryGetProperty("encodedPolyline", out JsonElement encoded))
                return MapResults.Error<MapRoute>("Google Routes API returned a route without geometry.");

            var geometry = GooglePolylineCodec.Decode(encoded.GetString());
            if (geometry.Count < 2) return MapResults.Error<MapRoute>("Google Routes API returned a route with fewer than two points.");

            // distanceMeters is omitted when zero; duration is a protobuf Duration string such as "165s".
            double distance = route.TryGetProperty("distanceMeters", out JsonElement d) ? d.GetDouble() : 0;
            double duration = route.TryGetProperty("duration", out JsonElement t) ? ParseDuration(t.GetString()) : 0;

            return MapResults.Ok(new MapRoute(geometry, distance, duration, routingType, "google-routes"));
        }

        internal static double ParseDuration(string value)
        {
            if (string.IsNullOrEmpty(value) || !value.EndsWith("s", StringComparison.Ordinal)
                || !double.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                throw new FormatException("Google returned an unreadable duration '" + value + "'.");
            return seconds;
        }

        private static object Waypoint(Geolocation point) => new
        {
            location = new { latLng = new { latitude = point.Latitude, longitude = point.Longitude } }
        };
    }
}
