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

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Search
{
    /// <summary>
    /// Nominatim (OpenStreetMap) search. The public instance needs no key but allows at
    /// most one request per second with an identifying User-Agent; point
    /// <see cref="BaseUrl"/> at a self-hosted instance for production volume.
    /// </summary>
    public sealed class NominatimPlaceSearchService : MapPlaceSearchServiceBase
    {
        public const string PublicBaseUrl = "https://nominatim.openstreetmap.org";
        private static readonly RequestIntervalGate PublicGate = new RequestIntervalGate(TimeSpan.FromSeconds(1));

        private readonly MapHttpClient _http;

        public NominatimPlaceSearchService(string baseUrl = PublicBaseUrl, HttpClient client = null)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("An absolute HTTPS Nominatim base URL is required.", nameof(baseUrl));
            BaseUrl = baseUrl.TrimEnd('/');
            _http = new MapHttpClient("Nominatim", client);
        }

        public override string Name => "nominatim";
        public string BaseUrl { get; }

        protected override async Task<OASISResult<List<MapPointOfInterest>>> RequestSearchAsync(
            MapPlaceSearchRequest request, CancellationToken cancellationToken)
        {
            var url = new StringBuilder(BaseUrl).Append("/search?format=jsonv2&q=").Append(Uri.EscapeDataString(request.Query))
                .Append("&limit=").Append(request.Limit);

            if (request.Near != null)
            {
                double halfSideKm = request.BiasRadiusMetres / 1000.0;
                BoundingBox box = request.Near.GetBoundingBox(halfSideKm);
                url.Append(string.Format(CultureInfo.InvariantCulture, "&viewbox={0},{1},{2},{3}",
                    box.MinPoint.Longitude, box.MaxPoint.Latitude, box.MaxPoint.Longitude, box.MinPoint.Latitude));
            }

            if (!string.IsNullOrWhiteSpace(request.LanguageCode))
                url.Append("&accept-language=").Append(Uri.EscapeDataString(request.LanguageCode));

            if (string.Equals(BaseUrl, PublicBaseUrl, StringComparison.OrdinalIgnoreCase))
                await PublicGate.WaitAsync(cancellationToken).ConfigureAwait(false);

            using var message = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            OASISResult<JsonDocument> sent = await _http.SendAsync(message, ReadError, cancellationToken).ConfigureAwait(false);
            if (sent.IsError) return MapResults.Error<List<MapPointOfInterest>>(sent.Message);

            using JsonDocument document = sent.Result;
            return Parse(document.RootElement);
        }

        internal static OASISResult<List<MapPointOfInterest>> Parse(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Array)
                return MapResults.Error<List<MapPointOfInterest>>("Nominatim returned " + (ReadError(root) ?? "an unexpected document") + ".");

            var places = new List<MapPointOfInterest>();
            foreach (JsonElement place in root.EnumerateArray())
            {
                var location = new Geolocation(ReadCoordinate(place, "lat"), ReadCoordinate(place, "lon"));
                if (!GeoMath.IsValid(location)) return MapResults.Error<List<MapPointOfInterest>>("Nominatim returned an invalid coordinate.");

                string displayName = ReadString(place, "display_name");
                places.Add(new MapPointOfInterest
                {
                    Id = place.TryGetProperty("osm_type", out JsonElement t) && place.TryGetProperty("osm_id", out JsonElement id)
                        ? t.GetString() + "/" + id.GetRawText() : null,
                    Name = string.IsNullOrEmpty(ReadString(place, "name")) ? displayName : ReadString(place, "name"),
                    Description = displayName,
                    Address = displayName,
                    Category = ReadString(place, "type") ?? ReadString(place, "category"),
                    Location = location
                });
            }

            return MapResults.Ok(places);
        }

        private static double ReadCoordinate(JsonElement place, string name)
        {
            JsonElement value = place.GetProperty(name);
            if (value.ValueKind == JsonValueKind.Number) return value.GetDouble();
            return double.Parse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static string ReadError(JsonElement root) =>
            root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out JsonElement e)
                ? (e.ValueKind == JsonValueKind.Object && e.TryGetProperty("message", out JsonElement m) ? m.GetString() : e.ToString())
                : null;
    }
}
