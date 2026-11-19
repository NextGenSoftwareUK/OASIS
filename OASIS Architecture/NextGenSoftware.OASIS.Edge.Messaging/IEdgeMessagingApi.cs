using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>Online read path to the hosted messaging API.</summary>
    public interface IEdgeMessagingApi
    {
        Task<OASISResult<EdgeServerSyncPage>> SyncAsync(DateTime? since, int limit, CancellationToken cancellationToken);
    }
}
