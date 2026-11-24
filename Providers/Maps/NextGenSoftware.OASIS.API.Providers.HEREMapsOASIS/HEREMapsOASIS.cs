using System.Net.Http;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.Providers.Shared.Maps;

namespace NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS
{
    /// <summary>
    /// HERE Location Services map provider. Routes come from HERE Routing API v8
    /// (<c>router.hereapi.com/v8/routes</c>) and place search from HERE Geocoding &amp;
    /// Search v1 (<c>/discover</c> near a point, <c>/geocode</c> otherwise). The map is
    /// drawn by the client (HERE Maps API for JavaScript or the HERE SDK) from this
    /// provider's commands.
    /// </summary>
    public sealed class HEREMapsOASIS : MapProviderBase
    {
        /// <summary>Reads the API key from OASIS_HERE_ROUTING_API_KEY.</summary>
        public HEREMapsOASIS() : this(HereApiKey.FromEnvironment())
        {
        }

        public HEREMapsOASIS(string apiKey, HttpClient client = null)
            : base(MapProviderType.HEREMaps, "HERE Maps",
                   "HERE Location Services OASIS Map Provider - Routing API v8 and Geocoding & Search v1.",
                   new HereRoutingService(apiKey, client), new HereSearchService(apiKey, client))
        {
        }
    }
}
