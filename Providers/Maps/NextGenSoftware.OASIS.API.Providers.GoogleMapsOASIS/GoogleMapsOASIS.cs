using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS
{
    /// <summary>
    /// Google Maps Platform map provider. Routes come from the Routes API
    /// (<c>directions/v2:computeRoutes</c>), place search from Places API (New) Text
    /// Search, and address lookups from the Geocoding API. The map itself is drawn by
    /// the client (Google Maps JavaScript or a native SDK) from this provider's commands.
    /// The API key needs the Routes, Places (New) and Geocoding APIs enabled; the
    /// parameterless constructor reads it from OASIS_GOOGLE_ROUTES_API_KEY.
    /// </summary>
    public sealed class GoogleMapsOASIS : MapProviderBase
    {
        /// <summary>Reads the API key from OASIS_GOOGLE_ROUTES_API_KEY.</summary>
        public GoogleMapsOASIS() : this(GoogleApiKey.FromEnvironment())
        {
        }

        public GoogleMapsOASIS(string apiKey, HttpClient client = null)
            : this(new GoogleRoutesService(apiKey, client), new GooglePlacesSearchService(apiKey, client),
                   new GoogleGeocodingService(apiKey, client))
        {
        }

        private GoogleMapsOASIS(GoogleRoutesService routes, GooglePlacesSearchService places, GoogleGeocodingService geocoding)
            : base(MapProviderType.GoogleMaps, "Google Maps",
                   "Google Maps Platform OASIS Map Provider - Routes API directions, Places API (New) search and Geocoding API.",
                   routes, places)
        {
            Geocoding = geocoding;
        }

        public GoogleGeocodingService Geocoding { get; }

        /// <summary>Converts a street address or plus code to coordinates.</summary>
        public Task<OASISResult<List<MapPointOfInterest>>> GeocodeAddressAsync(string address,
            CancellationToken cancellationToken = default) => Geocoding.GeocodeAsync(address, cancellationToken);
    }
}
