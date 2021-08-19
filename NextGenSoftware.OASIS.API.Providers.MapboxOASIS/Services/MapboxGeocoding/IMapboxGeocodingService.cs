using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Geocoding;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxGeocoding
{
    public interface IMapboxGeocodingService
    {
        Task<Response<Geocoding>> GetForwardGeocoding(GetForwardGeocodingRequest request);
        Task<Response<Geocoding>> GetReverseGeocoding(GetReverseGeocodingRequest request);
        Task<Response<List<Geocoding>>> GetReverseBatchGeocoding(GetReverseBatchGeocodingRequest request);
        Task<Response<List<Geocoding>>> GetForwardBatchGeocoding(GetForwardBatchGeocodingRequest request);
    }
}