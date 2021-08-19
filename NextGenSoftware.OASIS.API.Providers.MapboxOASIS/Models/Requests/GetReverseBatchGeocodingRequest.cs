using System.Collections.Generic;
using Enums;

namespace Models.Requests
{
    public class GetReverseBatchGeocodingRequest
    {
        public GeocodingEndpoint Endpoint { get; set; }
        public List<Coordinate> Coordinates { get; set; }
    }
}