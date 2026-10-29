using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IBoundCommittedHolonReplicaStorageProvider, IHolonReplicaHeadAuditStorageProvider
{
    public Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(
        HolonReplicaHeadReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsProviderActivated || _holonRepository == null)
            return Task.FromResult(new OASISResult<HolonReplicaHeadReadResult> {
                IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_HEAD_AUDIT_NOT_READY",
                Message = "Activate the existing Mongo replica before reading its evidence." });
        return _holonRepository.ReadHolonReplicaHeadsAsync(request, cancellationToken);
    }

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        return ApplyReplicaAsync(request, expectedStorage, cancellationToken);
    }

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
        => ApplyReplicaAsync(request, null, cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplyReplicaAsync(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommittedHolonReplicaPlan.ValidateNativeDestination(request,expectedStorage);
            var plan = CommittedHolonReplicaPlan.Create(request, Core.Enums.ProviderType.MongoDBOASIS);
            return await SaveGraphsAtomicAsync(request.Roots, request.SaveChildren, request.Recursive,
                request.MaxChildDepth, 0, plan, request.ActorId, cancellationToken, expectedStorage: expectedStorage).ConfigureAwait(false);
        }
        catch (Exception cause)
        {
            return new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCount = 1, Exception = cause,
                ErrorCode = cause is OperationCanceledException ? "COSMIC_REPLICA_WRITE_CANCELLED" : "COSMIC_REPLICA_WRITE_INVALID",
                Message = "Committed MongoDB replica admission failed." };
        }
    }
}
