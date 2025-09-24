using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// Forward Geocoding Provider Interface
    /// Provides geocoding and POI search functionality for map providers
    /// </summary>
    public interface IForwardGeocodingProvider
    {
        /// <summary>
        /// Search for Points of Interest (POIs) using a request
        /// </summary>
        /// <param name="request">POI search request</param>
        /// <returns>Response containing POI search results</returns>
        Task<object> POISearchAsync(object request);

        /// <summary>
        /// Batch search for Points of Interest (POIs) using multiple requests
        /// </summary>
        /// <param name="request">Batch POI search request</param>
        /// <returns>Response containing batch POI search results</returns>
        Task<object> BatchPOISearchAsync(object request);
    }
}
