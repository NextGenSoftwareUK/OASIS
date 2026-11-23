using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Search
{
    public sealed class MapBatchPlaceSearchRequest : IBatchRequest
    {
        public MapBatchPlaceSearchRequest(IEnumerable<MapPlaceSearchRequest> requests)
        {
            Requests = (requests ?? throw new ArgumentNullException(nameof(requests))).ToList();
            if (Requests.Count == 0) throw new ArgumentException("At least one search is required.", nameof(requests));
            if (Requests.Any(r => r == null)) throw new ArgumentException("Searches cannot be null.", nameof(requests));
        }

        public IReadOnlyList<MapPlaceSearchRequest> Requests { get; }

        public string GetRequestURLParameters() => Requests[0].GetRequestURLParameters();

        public string[] GetRequestMultipleURLParameters() => Requests.Select(r => r.GetRequestURLParameters()).ToArray();
    }
}
