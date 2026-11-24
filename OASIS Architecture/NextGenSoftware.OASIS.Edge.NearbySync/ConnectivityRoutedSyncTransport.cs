using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// Chooses the sync route from the device's connectivity state: the hosted node when online, a nearby
    /// peer (gateway or direct) when not. The choice is made before the call from known state, not by retrying
    /// after a failure, so behaviour is predictable and each route reports its own errors.
    /// Pass this to <c>OASISEdgeRuntime</c> as its transport.
    /// </summary>
    public sealed class ConnectivityRoutedSyncTransport : IHyperDriveSyncTransport
    {
        private readonly IHyperDriveSyncTransport _hosted;
        private readonly IHyperDriveSyncTransport _nearby;
        private readonly Func<bool> _isOnline;

        public ConnectivityRoutedSyncTransport(IHyperDriveSyncTransport hosted, IHyperDriveSyncTransport nearby, Func<bool> isOnline)
        {
            _hosted = hosted ?? throw new ArgumentNullException(nameof(hosted));
            _nearby = nearby ?? throw new ArgumentNullException(nameof(nearby));
            _isOnline = isOnline ?? throw new ArgumentNullException(nameof(isOnline));
        }

        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request, CancellationToken cancellationToken) =>
            (_isOnline() ? _hosted : _nearby).ExchangeAsync(request, cancellationToken);
    }
}
