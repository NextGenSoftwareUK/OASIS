using System;
using System.Globalization;
using System.Text;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Search
{
    /// <summary>A free-text place or address search, optionally biased towards a point.</summary>
    public sealed class MapPlaceSearchRequest : IRequest
    {
        public MapPlaceSearchRequest(string query, Geolocation near = null, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("A search query is required.", nameof(query));
            if (limit < 1 || limit > 50) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be 1-50.");
            Query = query.Trim();
            Near = near;
            Limit = limit;
        }

        public string Query { get; }
        public Geolocation Near { get; }
        public int Limit { get; }

        /// <summary>Radius of the bias circle around <see cref="Near"/>, in metres.</summary>
        public double BiasRadiusMetres { get; set; } = 5000;

        /// <summary>BCP-47 language for results, e.g. "en".</summary>
        public string LanguageCode { get; set; }

        /// <summary>A canonical description of the request; vendors build their own URLs from the fields.</summary>
        public string GetRequestURLParameters()
        {
            var parameters = new StringBuilder("q=").Append(Uri.EscapeDataString(Query)).Append("&limit=").Append(Limit);
            if (Near != null)
                parameters.Append(string.Format(CultureInfo.InvariantCulture, "&near={0},{1}", Near.Latitude, Near.Longitude));
            return parameters.ToString();
        }
    }
}
