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

namespace NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS
{
    /// <summary>
    /// HERE Geocoding &amp; Search v1. Searches with a bias point use
    /// <c>discover.search.hereapi.com/v1/discover</c> (places and addresses around it);
    /// searches without one use <c>geocode.search.hereapi.com/v1/geocode</c>, because
    /// /discover requires a location context.
    /// </summary>
    public sealed class HereSearchService : MapPlaceSearchServiceBase
    {
        public const string DiscoverEndpoint = "https://discover.search.hereapi.com/v1/discover";
        public const string GeocodeEndpoint = "https://geocode.search.hereapi.com/v1/geocode";

        private readonly string _apiKey;
        private readonly MapHttpClient _http;

        public HereSearchService(string apiKey, HttpClient client = null)
        {
            _apiKey = HereApiKey.Normalise(apiKey);
            _http = new MapHttpClient("HERE Geocoding & Search API", client);
        }

        public override string Name => "here-search";

        protected override async Task<OASISResult<List<MapPointOfInterest>>> RequestSearchAsync(
            MapPlaceSearchRequest search, CancellationToken cancellationToken)
        {
            if (_apiKey == null) return MapResults.Error<List<MapPointOfInterest>>(HereApiKey.MissingMessage("HERE Geocoding & Search API"));

            var url = new StringBuilder(search.Near == null ? GeocodeEndpoint : DiscoverEndpoint)
                .Append("?q=").Append(Uri.EscapeDataString(search.Query))
                .Append("&limit=").Append(Math.Min(search.Limit, 100));

            if (search.Near != null)
                url.Append(string.Format(CultureInfo.InvariantCulture, "&at={0},{1}", search.Near.Latitude, search.Near.Longitude));
            if (!string.IsNullOrWhiteSpace(search.LanguageCode))
                url.Append("&lang=").Append(Uri.EscapeDataString(search.LanguageCode));
            url.Append("&apiKey=").Append(Uri.EscapeDataString(_apiKey));

            using var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            OASISResult<JsonDocument> sent = await _http.SendAsync(request, HereErrors.Read, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<List<MapPointOfInterest>>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement);
        }

        internal static OASISResult<List<MapPointOfInterest>> Parse(JsonElement root)
        {
            if (!root.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
                return MapResults.Error<List<MapPointOfInterest>>("HERE Geocoding & Search API returned no items array.");

            var places = new List<MapPointOfInterest>();
            foreach (JsonElement item in items.EnumerateArray())
            {
                // Some result types (e.g. administrative areas) carry no position; they cannot be mapped.
                if (!item.TryGetProperty("position", out JsonElement position)) continue;

                var point = new Geolocation(position.GetProperty("lat").GetDouble(), position.GetProperty("lng").GetDouble());
                if (!GeoMath.IsValid(point)) return MapResults.Error<List<MapPointOfInterest>>("HERE returned an invalid coordinate.");

                string label = item.TryGetProperty("address", out JsonElement address) && address.TryGetProperty("label", out JsonElement l)
                    ? l.GetString() : null;
                string category = item.TryGetProperty("categories", out JsonElement categories) && categories.GetArrayLength() > 0
                    && categories[0].TryGetProperty("name", out JsonElement c) ? c.GetString() : ReadString(item, "resultType");

                places.Add(new MapPointOfInterest
                {
                    Id = ReadString(item, "id"),
                    Name = ReadString(item, "title"),
                    Description = label,
                    Address = label,
                    Category = category,
                    Location = point,
                    Distance = item.TryGetProperty("distance", out JsonElement d) ? (float)d.GetDouble() : 0f
                });
            }

            return MapResults.Ok(places);
        }

        private static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
