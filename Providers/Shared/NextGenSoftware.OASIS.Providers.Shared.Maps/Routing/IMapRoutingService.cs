using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Routing
{
    /// <summary>A real directions service. Errors are returned, never replaced by a straight line.</summary>
    public interface IMapRoutingService : IDirectionsAPIProvider
    {
        string Name { get; }

        Task<OASISResult<MapRoute>> GetRouteAsync(Geolocation from, Geolocation to,
            RoutingType routingType, CancellationToken cancellationToken = default);
    }
}
