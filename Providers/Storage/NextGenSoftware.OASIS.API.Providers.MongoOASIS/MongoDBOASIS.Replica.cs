using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICommittedHolonReplicaStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan = CommittedHolonReplicaPlan.Create(request, Core.Enums.ProviderType.MongoDBOASIS);
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
