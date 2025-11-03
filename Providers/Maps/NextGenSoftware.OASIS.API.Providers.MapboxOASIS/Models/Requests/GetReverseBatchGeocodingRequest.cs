using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests
{
    public class GetReverseBatchGeocodingRequest
    {
        public GeocodingEndpoint Endpoint { get; set; }
        public List<Coordinate> Coordinates { get; set; }
    }
}