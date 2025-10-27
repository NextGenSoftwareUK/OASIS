using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// Forward geocoding response wrapper
    /// </summary>
    /// <typeparam name="T">Type of POI data</typeparam>
    public class ForwardGeocodingResponse<T>
    {
        public bool Success { get; set; }
        public List<T> Data { get; set; }
        
        public ForwardGeocodingResponse() { }
        
        public ForwardGeocodingResponse(bool success, List<T> data)
        {
            Success = success;
            Data = data;
        }
    }
}

