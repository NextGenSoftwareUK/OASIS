using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// Directions API Provider Interface
    /// Provides routing and directions functionality for map providers
    /// </summary>
    public interface IDirectionsAPIProvider
    {
        /// <summary>
        /// Get directions between two points
        /// </summary>
        /// <param name="startPoint">Starting location</param>
        /// <param name="endPoint">Destination location</param>
        /// <param name="routingType">Type of routing (Walking, Driving, Cycling, etc.)</param>
        /// <returns>List of waypoints for the route</returns>
        Task<List<object>> GetDirectionsAsync(object startPoint, object endPoint, string routingType = "Walking");
    }
}
