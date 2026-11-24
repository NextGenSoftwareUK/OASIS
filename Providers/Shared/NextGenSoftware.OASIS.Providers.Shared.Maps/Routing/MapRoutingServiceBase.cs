using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Http;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Routing
{
    public abstract class MapRoutingServiceBase : IMapRoutingService
    {
        public abstract string Name { get; }

        public async Task<OASISResult<MapRoute>> GetRouteAsync(Geolocation from, Geolocation to,
            RoutingType routingType, CancellationToken cancellationToken = default)
        {
            if (!GeoMath.IsValid(from) || !GeoMath.IsValid(to))
                return MapResults.Error<MapRoute>(Name + " needs valid WGS84 start and end points.");
            if (!Enum.IsDefined(typeof(RoutingType), routingType))
                return MapResults.Error<MapRoute>(Name + " does not support routing type " + routingType + ".");

            return await RequestRouteAsync(from, to, routingType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The contract method of <see cref="IDirectionsAPIProvider"/>. It has no error
        /// channel, so a failed route throws rather than returning an empty or invented path.
        /// </summary>
        public async Task<List<Geolocation>> GetDirectionsAsync(Geolocation startPoint, Geolocation endPoint,
            RoutingType routingType = RoutingType.Walking)
        {
            OASISResult<MapRoute> route = await GetRouteAsync(startPoint, endPoint, routingType).ConfigureAwait(false);
            if (route.IsError) throw new InvalidOperationException(route.Message);
            return new List<Geolocation>(route.Result.Geometry);
        }

        protected abstract Task<OASISResult<MapRoute>> RequestRouteAsync(Geolocation from, Geolocation to,
            RoutingType routingType, CancellationToken cancellationToken);
    }
}
