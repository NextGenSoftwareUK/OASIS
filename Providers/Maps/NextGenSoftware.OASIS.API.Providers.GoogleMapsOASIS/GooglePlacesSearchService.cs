using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
    /// <summary>Places API (New) Text Search: POST https://places.googleapis.com/v1/places:searchText.</summary>
    public sealed class GooglePlacesSearchService : MapPlaceSearchServiceBase
    {
        public const string Endpoint = "https://places.googleapis.com/v1/places:searchText";
        private const string FieldMask = "places.id,places.displayName,places.formattedAddress,places.location,places.primaryType";
        private const double MaxBiasRadiusMetres = 50000;

        private readonly string _apiKey;
        private readonly MapHttpClient _http;

        public GooglePlacesSearchService(string apiKey, HttpClient client = null)
        {
            _apiKey = GoogleApiKey.Normalise(apiKey);
            _http = new MapHttpClient("Google Places API", client);
        }

        public override string Name => "google-places";

        protected override async Task<OASISResult<List<MapPointOfInterest>>> RequestSearchAsync(
            MapPlaceSearchRequest search, CancellationToken cancellationToken)
        {
            if (_apiKey == null) return MapResults.Error<List<MapPointOfInterest>>(GoogleApiKey.MissingMessage("Google Places API"));

            var body = new Dictionary<string, object>
            {
                ["textQuery"] = search.Query,
                ["pageSize"] = Math.Min(search.Limit, 20)
            };

            if (search.Near != null)
            {
                body["locationBias"] = new
                {
                    circle = new
                    {
                        center = new { latitude = search.Near.Latitude, longitude = search.Near.Longitude },
                        radius = Math.Clamp(search.BiasRadiusMetres, 0, MaxBiasRadiusMetres)
                    }
                };
            }

            if (!string.IsNullOrWhiteSpace(search.LanguageCode)) body["languageCode"] = search.LanguageCode;

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add("X-Goog-FieldMask", FieldMask);

            OASISResult<JsonDocument> sent = await _http.SendAsync(request, GoogleErrors.Read, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<List<MapPointOfInterest>>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement);
        }

        internal static OASISResult<List<MapPointOfInterest>> Parse(JsonElement root)
        {
            var places = new List<MapPointOfInterest>();

            // An empty object is Google's response for a search with no matches.
            if (!root.TryGetProperty("places", out JsonElement results)) return MapResults.Ok(places);

            foreach (JsonElement place in results.EnumerateArray())
            {
                JsonElement location = place.GetProperty("location");
                var point = new Geolocation(location.GetProperty("latitude").GetDouble(), location.GetProperty("longitude").GetDouble());
                if (!GeoMath.IsValid(point)) return MapResults.Error<List<MapPointOfInterest>>("Google Places API returned an invalid coordinate.");

                string address = ReadString(place, "formattedAddress");
                places.Add(new MapPointOfInterest
                {
                    Id = ReadString(place, "id"),
                    Name = place.TryGetProperty("displayName", out JsonElement name) ? ReadString(name, "text") : address,
                    Description = address,
                    Address = address,
                    Category = ReadString(place, "primaryType"),
                    Location = point
                });
            }

            return MapResults.Ok(places);
        }

        private static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
