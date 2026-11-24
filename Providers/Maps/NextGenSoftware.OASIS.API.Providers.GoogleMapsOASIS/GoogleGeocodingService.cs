using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Http;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS
{
    /// <summary>Geocoding API: GET https://maps.googleapis.com/maps/api/geocode/json.</summary>
    public sealed class GoogleGeocodingService
    {
        public const string Endpoint = "https://maps.googleapis.com/maps/api/geocode/json";

        private readonly string _apiKey;
        private readonly MapHttpClient _http;

        public GoogleGeocodingService(string apiKey, HttpClient client = null)
        {
            _apiKey = GoogleApiKey.Normalise(apiKey);
            _http = new MapHttpClient("Google Geocoding API", client);
        }

        public async Task<OASISResult<List<MapPointOfInterest>>> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("An address is required.", nameof(address));
            if (_apiKey == null) return MapResults.Error<List<MapPointOfInterest>>(GoogleApiKey.MissingMessage("Google Geocoding API"));

            string url = Endpoint + "?address=" + Uri.EscapeDataString(address.Trim()) + "&key=" + Uri.EscapeDataString(_apiKey);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            OASISResult<JsonDocument> sent = await _http.SendAsync(request, ReadError, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<List<MapPointOfInterest>>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement);
        }

        internal static OASISResult<List<MapPointOfInterest>> Parse(JsonElement root)
        {
            string status = root.TryGetProperty("status", out JsonElement s) ? s.GetString() : null;
            var places = new List<MapPointOfInterest>();

            if (status == "ZERO_RESULTS") return MapResults.Ok(places);
            if (status != "OK") return MapResults.Error<List<MapPointOfInterest>>("Google Geocoding API returned " + (ReadError(root) ?? status ?? "no status") + ".");

            foreach (JsonElement result in root.GetProperty("results").EnumerateArray())
            {
                JsonElement location = result.GetProperty("geometry").GetProperty("location");
                var point = new Geolocation(location.GetProperty("lat").GetDouble(), location.GetProperty("lng").GetDouble());
                if (!GeoMath.IsValid(point)) return MapResults.Error<List<MapPointOfInterest>>("Google Geocoding API returned an invalid coordinate.");

                string address = result.TryGetProperty("formatted_address", out JsonElement a) ? a.GetString() : null;
                string type = result.TryGetProperty("types", out JsonElement types) && types.GetArrayLength() > 0 ? types[0].GetString() : null;

                places.Add(new MapPointOfInterest
                {
                    Id = result.TryGetProperty("place_id", out JsonElement id) ? id.GetString() : null,
                    Name = address,
                    Description = address,
                    Address = address,
                    Category = type,
                    Location = point,
                    Source = "google-geocoding"
                });
            }

            return MapResults.Ok(places);
        }

        private static string ReadError(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;
            string status = root.TryGetProperty("status", out JsonElement s) ? s.GetString() : null;
            string message = root.TryGetProperty("error_message", out JsonElement m) ? m.GetString() : null;
            return message == null ? status : status + ": " + message;
        }
    }
}
