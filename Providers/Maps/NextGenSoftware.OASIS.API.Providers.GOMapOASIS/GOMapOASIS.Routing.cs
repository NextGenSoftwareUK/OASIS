using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS;

public partial class GOMapOASIS : IOASISRoutingProvider
{
    public Task<RoutingRoute> ComputeRouteAsync(RoutingCoordinate origin, RoutingCoordinate destination,
        RoutingType travelMode, CancellationToken cancellationToken = default)
    {
        if (DirectionsAPI is not IOASISRoutingProvider routing)
            throw new InvalidOperationException("Configure a directions client that implements IOASISRoutingProvider.");

        return routing.ComputeRouteAsync(origin, destination, travelMode, cancellationToken);
    }
}
