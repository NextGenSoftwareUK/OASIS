using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    public interface IHostedHyperDriveDeliveryReader
    {
        Task<OASISResult<IReadOnlyList<HostedSyncDelivery>>> ReadRecentDeliveriesAsync(
            Guid authenticatedAvatarId, int limit, CancellationToken cancellationToken);
    }

    public sealed class HostedSyncDelivery
    {
        public HostedSyncDelivery(Guid operationId, string entityType, string operation, string state, int attemptCount,
            DateTime createdUtc, DateTime? nextAttemptUtc, DateTime? completedUtc)
        {
            OperationId = operationId;
            EntityType = entityType;
            Operation = operation;
            State = state;
            AttemptCount = attemptCount;
            CreatedUtc = createdUtc;
            NextAttemptUtc = nextAttemptUtc;
            CompletedUtc = completedUtc;
        }

        public Guid OperationId { get; }
        public string EntityType { get; }
        public string Operation { get; }
        public string State { get; }
        public int AttemptCount { get; }
        public DateTime CreatedUtc { get; }
        public DateTime? NextAttemptUtc { get; }
        public DateTime? CompletedUtc { get; }
    }
}
