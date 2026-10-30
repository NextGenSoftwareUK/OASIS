using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICommittedHolonReplicaStorageProvider, ICommittedHolonReplicaInitializationStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
        => await ApplyReplicaAsync(request, false, cancellationToken).ConfigureAwait(false);

    public OASISResult<IEnumerable<IHolon>> InitializeCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => InitializeCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> InitializeCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
        => ApplyReplicaAsync(request, true, cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplyReplicaAsync(
        CommittedHolonReplicaRequest request, bool initialize, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan = initialize ? CommittedHolonReplicaPlan.CreateInitialBaseline(request, Core.Enums.ProviderType.MongoDBOASIS)
                : CommittedHolonReplicaPlan.Create(request, Core.Enums.ProviderType.MongoDBOASIS);
            return await SaveGraphsAtomicAsync(request.Roots, request.SaveChildren, request.Recursive,
                request.MaxChildDepth, 0, plan, request.ActorId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception cause)
        {
            return new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCount = 1, Exception = cause,
                ErrorCode = cause is OperationCanceledException ? "COSMIC_REPLICA_WRITE_CANCELLED" : "COSMIC_REPLICA_WRITE_INVALID",
                Message = "Committed MongoDB replica admission failed." };
        }
    }
}
