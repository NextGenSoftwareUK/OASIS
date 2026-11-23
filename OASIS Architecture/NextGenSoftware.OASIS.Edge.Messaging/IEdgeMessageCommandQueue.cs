using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>Durably queues a messaging command into the HyperDrive outbox.</summary>
    public interface IEdgeMessageCommandQueue
    {
        Task<OASISResult<bool>> QueueAsync(Guid entityId, HyperDriveMessageCommand command, CancellationToken cancellationToken);
    }
}
