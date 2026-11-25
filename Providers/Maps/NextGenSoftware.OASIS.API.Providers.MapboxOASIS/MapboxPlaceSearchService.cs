using System;
using System.Collections.Generic;
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
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS
{
    /// <summary>Mapbox Geocoding API v6 forward search: GET https://api.mapbox.com/search/geocode/v6/forward.</summary>
    public sealed class MapboxPlaceSearchService : MapPlaceSearchServiceBase
    {
        public const string Endpoint = "https://api.mapbox.com/search/geocode/v6/forward";

        /// <summary>The v6 forward endpoint returns at most ten results.</summary>
        public const int MaxLimit = 10;

        private readonly string _accessToken;
        private readonly MapHttpClient _http;

        public MapboxPlaceSearchService(string accessToken, HttpClient client = null)
        {
            _accessToken = MapboxAccessToken.Normalise(accessToken);
            _http = new MapHttpClient("Mapbox Geocoding API", client);
        }

        public override string Name => "mapbox-geocoding";

        protected override async Task<OASISResult<List<MapPointOfInterest>>> RequestSearchAsync(
            MapPlaceSearchRequest search, CancellationToken cancellationToken)
        {
            if (_accessToken == null)
                return MapResults.Error<List<MapPointOfInterest>>(MapboxAccessToken.MissingMessage("Mapbox Geocoding API"));

            var url = new StringBuilder(Endpoint)
                .Append("?q=").Append(Uri.EscapeDataString(search.Query))
                .Append("&limit=").Append(Math.Min(search.Limit, MaxLimit));

            // Mapbox takes proximity as longitude,latitude.
            if (search.Near != null)
                url.Append(string.Format(CultureInfo.InvariantCulture, "&proximity={0},{1}", search.Near.Longitude, search.Near.Latitude));
            if (!string.IsNullOrWhiteSpace(search.LanguageCode))
                url.Append("&language=").Append(Uri.EscapeDataString(search.LanguageCode));
            url.Append("&access_token=").Append(Uri.EscapeDataString(_accessToken));

            using var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            OASISResult<JsonDocument> sent = await _http.SendAsync(request, MapboxErrors.Read, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<List<MapPointOfInterest>>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement);
        }

        internal static OASISResult<List<MapPointOfInterest>> Parse(JsonElement root)
        {
            if (!root.TryGetProperty("features", out JsonElement features) || features.ValueKind != JsonValueKind.Array)
                return MapResults.Error<List<MapPointOfInterest>>("Mapbox Geocoding API did not return a GeoJSON FeatureCollection.");

            var places = new List<MapPointOfInterest>();
            foreach (JsonElement feature in features.EnumerateArray())
            {
                JsonElement coordinates = feature.GetProperty("geometry").GetProperty("coordinates");
                var point = new Geolocation(coordinates[1].GetDouble(), coordinates[0].GetDouble());
                if (!GeoMath.IsValid(point)) return MapResults.Error<List<MapPointOfInterest>>("Mapbox Geocoding API returned an invalid coordinate.");

                JsonElement properties = feature.GetProperty("properties");
                string name = ReadString(properties, "name");
                string place = ReadString(properties, "place_formatted");
                string address = ReadString(properties, "full_address")
                              ?? (place == null ? name : name == null ? place : name + ", " + place);

                places.Add(new MapPointOfInterest
                {
                    Id = ReadString(properties, "mapbox_id") ?? ReadString(feature, "id"),
                    Name = name ?? address,
                    Description = address,
                    Address = address,
                    Category = ReadString(properties, "feature_type"),
                    Location = point
                });
            }

            return MapResults.Ok(places);
        }

        private static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
