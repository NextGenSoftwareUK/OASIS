using System.Collections.Generic;
using Enums;

namespace Models.Requests
{
    public class GetForwardBatchGeocodingRequest
    {
        public GeocodingEndpoint Endpoint { get; set; }
        public List<string> SearchText { get; set; }
    }
}