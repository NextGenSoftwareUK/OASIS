using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps
{
    public abstract partial class MapProviderBase
    {
        /// <summary>
        /// <see cref="IOASISRoutingProvider"/>: computes a route with the provider's routing
        /// service without drawing it. Throws <see cref="InvalidOperationException"/> with
        /// the vendor's message when no route can be computed.
        /// </summary>
        public async Task<RoutingRoute> ComputeRouteAsync(RoutingCoordinate origin, RoutingCoordinate destination,
            RoutingType travelMode, CancellationToken cancellationToken = default)
        {
            OASISResult<MapRoute> route = await Routing.GetRouteAsync(
                new Geolocation(origin.Latitude, origin.Longitude),
                new Geolocation(destination.Latitude, destination.Longitude),
                travelMode, cancellationToken).ConfigureAwait(false);

            if (route.IsError) throw new InvalidOperationException(route.Message);

            return new RoutingRoute(route.Result.DistanceMetres,
                route.Result.Geometry.Select(p => new RoutingCoordinate(p.Latitude, p.Longitude)));
        }
    }
}
