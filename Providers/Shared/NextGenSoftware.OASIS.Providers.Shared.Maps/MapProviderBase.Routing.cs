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
        /// service without drawing it. An undefined travel mode throws
        /// <see cref="ArgumentOutOfRangeException"/> before any request; a route the vendor
        /// cannot compute throws <see cref="InvalidOperationException"/> with its message.
        /// </summary>
        public async Task<RoutingRoute> ComputeRouteAsync(RoutingCoordinate origin, RoutingCoordinate destination,
            RoutingType travelMode, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(typeof(RoutingType), travelMode))
                throw new ArgumentOutOfRangeException(nameof(travelMode), travelMode, "Unsupported travel mode.");

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
