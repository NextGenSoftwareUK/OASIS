using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Directions
{
    /// <summary>
    /// Routing against OSRM (Open Source Routing Machine), the engine behind
    /// OpenStreetMap's own directions.
    ///
    /// The public demo server is the default because it needs no API key, which
    /// keeps the provider usable out of the box. Point <see cref="BaseUrl"/> at a
    /// self-hosted OSRM instance for production - the demo server is rate limited
    /// and carries no availability guarantee.
    /// </summary>
    public class OSRMDirectionsProvider : IDirectionsAPIProvider
    {
        public const string DefaultBaseUrl = "https://router.project-osrm.org";

        private static readonly HttpClient SharedClient = CreateClient();

        private readonly HttpClient _client;

        public string BaseUrl { get; set; } = DefaultBaseUrl;

        /// <summary>The last transport or parsing failure, for diagnostics.</summary>
        public string LastError { get; private set; }

        public OSRMDirectionsProvider() : this(null)
        {
        }

        /// <summary>Supply a client to control timeouts, proxies or test doubles.</summary>
        public OSRMDirectionsProvider(HttpClient client)
        {
            _client = client ?? SharedClient;
        }

        public async Task<List<Geolocation>> GetDirectionsAsync(
            Geolocation startPoint, Geolocation endPoint, RoutingType routingType = RoutingType.Walking)
        {
            LastError = null;

            if (startPoint == null || endPoint == null)
            {
                LastError = "Both a start and an end point are required.";
                return new List<Geolocation>();
            }

            string url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/route/v1/{1}/{2},{3};{4},{5}?overview=full&geometries=geojson",
                BaseUrl.TrimEnd('/'),
                ToOsrmProfile(routingType),
                startPoint.Longitude, startPoint.Latitude,
                endPoint.Longitude, endPoint.Latitude);

            try
            {
                using (HttpResponseMessage response = await _client.GetAsync(url).ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = $"OSRM returned {(int)response.StatusCode} {response.ReasonPhrase}.";
                        return new List<Geolocation>();
                    }

                    return ParseRoute(body);
                }
            }
            catch (Exception ex)
            {
                LastError = "Could not reach the routing service: " + ex.Message;
                return new List<Geolocation>();
            }
        }

        private List<Geolocation> ParseRoute(string json)
        {
            List<Geolocation> waypoints = new List<Geolocation>();

            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("code", out JsonElement code)
                    && !string.Equals(code.GetString(), "Ok", StringComparison.OrdinalIgnoreCase))
                {
                    LastError = "OSRM could not route between those points: " + code.GetString();
                    return waypoints;
                }

                if (!root.TryGetProperty("routes", out JsonElement routes)
                    || routes.ValueKind != JsonValueKind.Array
                    || routes.GetArrayLength() == 0)
                {
                    LastError = "OSRM returned no routes.";
                    return waypoints;
                }

                JsonElement first = routes[0];

                if (!first.TryGetProperty("geometry", out JsonElement geometry)
                    || !geometry.TryGetProperty("coordinates", out JsonElement coordinates))
                {
                    LastError = "The OSRM route carried no geometry.";
                    return waypoints;
                }

                // GeoJSON orders each pair longitude first.
                foreach (JsonElement pair in coordinates.EnumerateArray())
                {
                    if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() < 2) continue;

                    waypoints.Add(new Geolocation(pair[1].GetDouble(), pair[0].GetDouble()));
                }
            }

            return waypoints;
        }

        private static string ToOsrmProfile(RoutingType routingType)
        {
            switch (routingType)
            {
                case RoutingType.Driving: return "driving";
                case RoutingType.Cycling: return "cycling";
                default: return "foot";
            }
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Add("User-Agent", "OASIS-GOMapOASIS/2.0 (+https://oasisomniverse.one)");
            return client;
        }
    }
}
