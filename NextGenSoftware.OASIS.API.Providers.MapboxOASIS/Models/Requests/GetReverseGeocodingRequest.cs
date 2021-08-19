using Enums;

namespace Models.Requests
{
    public class GetReverseGeocodingRequest
    {
        public GeocodingEndpoint Endpoint { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
    }
}