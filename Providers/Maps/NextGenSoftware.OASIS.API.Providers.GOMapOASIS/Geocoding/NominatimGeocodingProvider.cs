using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geo;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding
{
    /// <summary>
    /// Forward geocoding and place search against Nominatim, OpenStreetMap's own
    /// geocoder - the same data GO Map renders, so search results line up with what
    /// is on the tiles.
    ///
    /// The public instance is the default because it needs no API key. It asks for
    /// no more than one request per second and a genuine User-Agent, both of which
    /// are honoured here; point <see cref="BaseUrl"/> at a self-hosted instance or a
    /// commercial geocoder for production volume.
    /// </summary>
    public class NominatimGeocodingProvider : IForwardGeocodingProvider
    {
        public const string DefaultBaseUrl = "https://nominatim.openstreetmap.org";

        private static readonly HttpClient SharedClient = CreateClient();

        private readonly HttpClient _client;
        private readonly SemaphoreSlimGate _rateLimit = new SemaphoreSlimGate(TimeSpan.FromSeconds(1));

        public string BaseUrl { get; set; } = DefaultBaseUrl;

        /// <summary>The last transport or parsing failure, for diagnostics.</summary>
        public string LastError { get; private set; }

        public NominatimGeocodingProvider() : this(null)
        {
        }

        /// <summary>Supply a client to control timeouts, proxies or test doubles.</summary>
        public NominatimGeocodingProvider(HttpClient client)
        {
            _client = client ?? SharedClient;
        }

        public async Task<ForwardGeocodingResponse<IPointOfInterest>> POISearchAsync(IRequest request)
        {
            LastError = null;

            if (request == null)
            {
                LastError = "A search request is required.";
                return new ForwardGeocodingResponse<IPointOfInterest>(false, new List<IPointOfInterest>());
            }

            Geolocation near = (request as POISearchRequest)?.Near;
            List<IPointOfInterest> results = await SearchAsync(request.GetRequestURLParameters(), near)
                .ConfigureAwait(false);

            return new ForwardGeocodingResponse<IPointOfInterest>(LastError == null, results);
        }

        public async Task<ForwardGeocodingResponse<IPointOfInterest>> BatchPOISearchAsync(IBatchRequest request)
        {
            LastError = null;

            if (request == null)
            {
                LastError = "A batch search request is required.";
                return new ForwardGeocodingResponse<IPointOfInterest>(false, new List<IPointOfInterest>());
            }

            BatchPOISearchRequest batch = request as BatchPOISearchRequest;
            string[] queries = request.GetRequestMultipleURLParameters();

            List<IPointOfInterest> combined = new List<IPointOfInterest>();

            for (int i = 0; i < queries.Length; i++)
            {
                Geolocation near = batch != null && i < batch.Requests.Count ? batch.Requests[i].Near : null;
                combined.AddRange(await SearchAsync(queries[i], near).ConfigureAwait(false));
            }

            return new ForwardGeocodingResponse<IPointOfInterest>(LastError == null, combined);
        }

        /// <summary>Convenience search by free text, optionally biased towards a point.</summary>
        public Task<ForwardGeocodingResponse<IPointOfInterest>> SearchAsync(
            string query, Geolocation near = null, int limit = 10)
        {
            return POISearchAsync(new POISearchRequest(query, near, limit));
        }

        private async Task<List<IPointOfInterest>> SearchAsync(string parameters, Geolocation near)
        {
            List<IPointOfInterest> results = new List<IPointOfInterest>();

            if (string.IsNullOrWhiteSpace(parameters))
            {
                LastError = "The search request produced no query parameters.";
                return results;
            }

            string url = BaseUrl.TrimEnd('/') + "/search?" + parameters;

            try
            {
                await _rateLimit.WaitAsync().ConfigureAwait(false);

                using (HttpResponseMessage response = await _client.GetAsync(url).ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = $"Nominatim returned {(int)response.StatusCode} {response.ReasonPhrase}.";
                        return results;
                    }

                    results.AddRange(Parse(body, near));
                }
            }
            catch (Exception ex)
            {
                LastError = "Could not reach the geocoding service: " + ex.Message;
            }

            return results;
        }

        private static IEnumerable<IPointOfInterest> Parse(string json, Geolocation near)
        {
            List<IPointOfInterest> results = new List<IPointOfInterest>();

            using (JsonDocument document = JsonDocument.Parse(json))
            {
                if (document.RootElement.ValueKind != JsonValueKind.Array) return results;

                foreach (JsonElement place in document.RootElement.EnumerateArray())
                {
                    if (!TryReadDouble(place, "lat", out double latitude)) continue;
                    if (!TryReadDouble(place, "lon", out double longitude)) continue;

                    Geolocation location = new Geolocation(latitude, longitude);

                    results.Add(new PointOfInterest
                    {
                        Name = ReadString(place, "name") ?? ReadString(place, "display_name"),
                        Description = ReadString(place, "display_name"),
                        Category = ReadString(place, "type") ?? ReadString(place, "category"),
                        Location = location,
                        Distance = near != null ? (float)GeoMath.HaversineMetres(near, location) : 0f
                    });
                }
            }

            return near != null ? results.OrderBy(r => r.Distance).ToList() : results;
        }

        private static bool TryReadDouble(JsonElement element, string name, out double value)
        {
            value = 0.0;

            if (!element.TryGetProperty(name, out JsonElement property)) return false;

            // Nominatim returns coordinates as strings.
            if (property.ValueKind == JsonValueKind.Number) return property.TryGetDouble(out value);

            return property.ValueKind == JsonValueKind.String
                   && double.TryParse(property.GetString(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value);
        }

        private static string ReadString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement property)
                   && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            // Nominatim's usage policy requires an identifying User-Agent.
            client.DefaultRequestHeaders.Add("User-Agent", "OASIS-GOMapOASIS/2.0 (+https://oasisomniverse.one)");
            return client;
        }
    }
}
