using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests
{
    public class GetForwardBatchGeocodingRequest
    {
        public GeocodingEndpoint Endpoint { get; set; }
        public List<string> SearchText { get; set; }
    }
}