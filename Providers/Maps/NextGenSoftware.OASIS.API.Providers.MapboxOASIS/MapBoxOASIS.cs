using System.Net.Http;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.Providers.Shared.Maps;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS
{
    /// <summary>
    /// Mapbox map provider. Routes come from the Mapbox Directions API v5 and place search
    /// from the Mapbox Geocoding API v6. The map itself is drawn by the client (Mapbox GL JS
    /// or a Mapbox mobile SDK) from this provider's commands. The parameterless constructor
    /// reads the access token from OASIS_MAPBOX_ACCESS_TOKEN.
    /// </summary>
    public sealed class MapBoxOASIS : MapProviderBase
    {
        public MapBoxOASIS() : this(MapboxAccessToken.FromEnvironment())
        {
        }

        public MapBoxOASIS(string accessToken, HttpClient client = null)
            : base(MapProviderType.MapBox, "MapBox",
                   "Mapbox OASIS Map Provider - Directions API v5 routing and Geocoding API v6 place search.",
                   new MapboxRoutingService(accessToken, client), new MapboxPlaceSearchService(accessToken, client))
        {
        }
    }
}
