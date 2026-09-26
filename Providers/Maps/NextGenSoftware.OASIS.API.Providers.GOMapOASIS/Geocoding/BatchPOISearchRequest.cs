using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding
{
    /// <summary>
    /// Several place searches issued together. Nominatim has no batch endpoint, so
    /// this carries the individual queries and the provider runs them concurrently.
    /// </summary>
    public class BatchPOISearchRequest : IBatchRequest
    {
        public IList<POISearchRequest> Requests { get; }

        public BatchPOISearchRequest()
        {
            Requests = new List<POISearchRequest>();
        }

        public BatchPOISearchRequest(IEnumerable<POISearchRequest> requests)
        {
            Requests = new List<POISearchRequest>(requests ?? Enumerable.Empty<POISearchRequest>());
        }

        public BatchPOISearchRequest(IEnumerable<string> queries, Geolocation near = null, int limit = 10)
        {
            Requests = (queries ?? Enumerable.Empty<string>())
                .Select(q => new POISearchRequest(q, near, limit))
                .ToList();
        }

        public BatchPOISearchRequest Add(POISearchRequest request)
        {
            if (request != null) Requests.Add(request);
            return this;
        }

        /// <summary>The first query, so a batch can stand in wherever an IRequest is expected.</summary>
        public string GetRequestURLParameters()
        {
            return Requests.Count > 0 ? Requests[0].GetRequestURLParameters() : string.Empty;
        }

        public string[] GetRequestMultipleURLParameters()
        {
            return Requests.Select(r => r.GetRequestURLParameters()).ToArray();
        }
    }
}
