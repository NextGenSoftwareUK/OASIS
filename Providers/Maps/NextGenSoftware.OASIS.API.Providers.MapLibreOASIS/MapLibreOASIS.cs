using System.Net.Http;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.Providers.Shared.Maps;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapLibreOASIS
{
    /// <summary>
    /// MapLibre map provider. MapLibre is an open-source renderer (maplibre-gl-js,
    /// MapLibre Native) with no routing or search service of its own, so this provider
    /// pairs it with OpenStreetMap services: OSRM for directions and Nominatim for place
    /// search, both configurable to self-hosted instances. <see cref="Scene"/> turns the
    /// map session into GeoJSON sources and style-spec layers a MapLibre client renders
    /// directly.
    /// </summary>
    public sealed class MapLibreOASIS : MapProviderBase
    {
        public MapLibreOASIS()
            : this(OsrmRoutingService.PublicDemoBaseUrl, NominatimPlaceSearchService.PublicBaseUrl)
        {
        }

        public MapLibreOASIS(string osrmBaseUrl, string nominatimBaseUrl, HttpClient client = null)
            : base(MapProviderType.MapLibre, "MapLibre",
                   "MapLibre OASIS Map Provider - OSRM directions, Nominatim search and MapLibre style-spec GeoJSON output.",
                   new OsrmRoutingService(osrmBaseUrl, client), new NominatimPlaceSearchService(nominatimBaseUrl, client))
        {
            Scene = new MapLibreSceneBuilder(this);
        }

        public MapLibreSceneBuilder Scene { get; }
    }
}
