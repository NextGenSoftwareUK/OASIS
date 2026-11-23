using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Search
{
    public interface IMapPlaceSearchService : IForwardGeocodingProvider
    {
        string Name { get; }

        /// <summary>The error of the last failed contract-method call, for callers of <see cref="IForwardGeocodingProvider"/>.</summary>
        string LastError { get; }

        Task<OASISResult<List<MapPointOfInterest>>> SearchAsync(MapPlaceSearchRequest request,
            CancellationToken cancellationToken = default);
    }
}
