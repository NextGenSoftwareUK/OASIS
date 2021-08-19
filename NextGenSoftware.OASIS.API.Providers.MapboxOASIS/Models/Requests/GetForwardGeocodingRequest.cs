using Enums;

namespace Models.Requests
{
    public class GetForwardGeocodingRequest
    {
        public GeocodingEndpoint Endpoint { get; set; }
        public string SearchText { get; set; }
    }
}