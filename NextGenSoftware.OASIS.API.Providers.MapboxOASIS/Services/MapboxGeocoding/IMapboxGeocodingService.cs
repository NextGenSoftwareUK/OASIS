using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using Models.Geocoding;
using Models.Requests;

namespace Services.MapboxGeocoding
{
    public interface IMapboxGeocodingService
    {
        Task<Response<Geocoding>> GetForwardGeocoding(GetForwardGeocodingRequest request);
        Task<Response<Geocoding>> GetReverseGeocoding(GetReverseGeocodingRequest request);
        Task<Response<List<Geocoding>>> GetReverseBatchGeocoding(GetReverseBatchGeocodingRequest request);
        Task<Response<List<Geocoding>>> GetForwardBatchGeocoding(GetForwardBatchGeocodingRequest request);
    }
}