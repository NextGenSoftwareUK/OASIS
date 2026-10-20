using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IConditionalHolonStorageProvider,IAtomicHolonReplicationStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(ConditionalHolonWriteRequest request,HolonReplicationEnrollment enrollment)
        =>ExecuteReplicatingHolonWriteAsync(request,enrollment).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(ConditionalHolonWriteRequest request,
        HolonReplicationEnrollment enrollment,CancellationToken cancellationToken=default)
        =>enrollment==null ? Task.FromResult(new OASISResult<IEnumerable<IHolon>>{IsError=true,ErrorCount=1,
            ErrorCode="COSMIC_REPLICATION_ENROLLMENT_REQUIRED",Message="Atomic replication requires explicit enrollment."})
            :ExecuteConditionalCoreAsync(request,enrollment,cancellationToken);

    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        => ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request, CancellationToken cancellationToken = default)
        =>ExecuteConditionalCoreAsync(request,request?.Write.ReplicationEnrollment,cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalCoreAsync(ConditionalHolonWriteRequest request,
        HolonReplicationEnrollment enrollment,CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan = enrollment==null ? ConditionalHolonPlan.Create(request, Core.Enums.ProviderType.MongoDBOASIS)
                :ConditionalHolonPlan.CreateReplicating(request,Core.Enums.ProviderType.MongoDBOASIS,enrollment);
            return await SaveGraphsAtomicAsync(request.Roots, request.SaveChildren, request.Recursive,
                request.MaxChildDepth, 0, plan, request.Write.ActorId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception cause)
        {
            return new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCount = 1,
                ErrorCode = cause is OperationCanceledException ? "COSMIC_CONDITIONAL_WRITE_CANCELLED" : "COSMIC_CONDITIONAL_WRITE_INVALID", Message = "Conditional MongoDB write admission failed.", Exception = cause };
        }
    }
}
