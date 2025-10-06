using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests
{
    public class GetForwardGeocodingRequest
    {
        public GeocodingEndpoint Endpoint { get; set; }
        public string SearchText { get; set; }
    }
}