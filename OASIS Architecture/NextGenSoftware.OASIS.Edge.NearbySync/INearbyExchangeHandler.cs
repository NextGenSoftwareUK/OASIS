using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// What a responding device does with a sync exchange from a verified nearby peer: apply it to a local store,
    /// or forward it upstream when this device has internet (store-and-forward gateway).
    /// </summary>
    public interface INearbyExchangeHandler
    {
        Task<OASISResult<SyncExchangeResponse>> HandleAsync(SyncExchangeRequest request, CancellationToken cancellationToken);
    }
}
