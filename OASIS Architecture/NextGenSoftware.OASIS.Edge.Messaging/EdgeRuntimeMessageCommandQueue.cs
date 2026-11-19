using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>Queues message commands through <see cref="OASISEdgeRuntime"/>'s atomic local-state/outbox transaction.</summary>
    public sealed class EdgeRuntimeMessageCommandQueue : IEdgeMessageCommandQueue
    {
        private readonly OASISEdgeRuntime _runtime;

        public EdgeRuntimeMessageCommandQueue(OASISEdgeRuntime runtime) => _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        public async Task<OASISResult<bool>> QueueAsync(Guid entityId, HyperDriveMessageCommand command, CancellationToken cancellationToken)
        {
            var queued = await _runtime.Entities.QueueCommandAsync(new EdgeCommandRequest<HyperDriveMessageCommand>
            {
                OperationId = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = HyperDriveEntityTypes.MessageCommand,
                VersionId = Guid.NewGuid(),
                Payload = command,
                CreatedUtc = DateTime.UtcNow
            }, cancellationToken).ConfigureAwait(false);
            return new OASISResult<bool>
            {
                Result = !queued.IsError, IsError = queued.IsError, ErrorCode = queued.ErrorCode,
                Message = queued.Message, Exception = queued.Exception
            };
        }
    }
}
