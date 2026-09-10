using System;
using System.Globalization;
using System.Text;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding
{
    /// <summary>
    /// A single place search. Renders itself as Nominatim query parameters, which is
    /// what IRequest.GetRequestURLParameters exists for.
    /// </summary>
    public class POISearchRequest : IRequest
    {
        /// <summary>Free-text query, for example "coffee" or "Tower Bridge".</summary>
        public string Query { get; set; }

        /// <summary>Bias and measure results from here, when supplied.</summary>
        public Geolocation Near { get; set; }

        /// <summary>Restrict results to this box, when supplied.</summary>
        public BoundingBox Within { get; set; }

        public int Limit { get; set; } = 10;

        /// <summary>Two-letter country codes to restrict the search to.</summary>
        public string[] CountryCodes { get; set; }

        public POISearchRequest() { }

        public POISearchRequest(string query)
        {
            Query = query;
        }

        public POISearchRequest(string query, Geolocation near, int limit = 10)
        {
            Query = query;
            Near = near;
            Limit = limit;
        }

        public string GetRequestURLParameters()
        {
            StringBuilder parameters = new StringBuilder();

            parameters.Append("q=").Append(Uri.EscapeDataString(Query ?? string.Empty));
            parameters.Append("&format=jsonv2&addressdetails=1");
            parameters.Append("&limit=").Append(Limit > 0 ? Limit : 10);

            if (Within != null && Within.MinPoint != null && Within.MaxPoint != null)
            {
                parameters.Append(string.Format(
                    CultureInfo.InvariantCulture,
                    "&viewbox={0},{1},{2},{3}&bounded=1",
                    Within.MinPoint.Longitude, Within.MaxPoint.Latitude,
                    Within.MaxPoint.Longitude, Within.MinPoint.Latitude));
            }
            else if (Near != null)
            {
                // No box given: bias towards a 5km square around the search point.
                BoundingBox box = Near.GetBoundingBox(5.0);

                parameters.Append(string.Format(
                    CultureInfo.InvariantCulture,
                    "&viewbox={0},{1},{2},{3}",
                    box.MinPoint.Longitude, box.MaxPoint.Latitude,
                    box.MaxPoint.Longitude, box.MinPoint.Latitude));
            }

            if (CountryCodes != null && CountryCodes.Length > 0)
                parameters.Append("&countrycodes=").Append(string.Join(",", CountryCodes));

            return parameters.ToString();
        }
    }
}
