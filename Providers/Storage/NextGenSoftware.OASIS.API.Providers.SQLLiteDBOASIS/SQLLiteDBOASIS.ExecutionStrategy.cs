using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NativeAuthority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS:ICosmicExecutionStrategyStorageProvider
{
    public NativeAuthority ExecutionStrategyAuthority=>NativeAuthority.SQLLiteDBOASIS;
    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteBoundConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request,CosmicStorageIdentity expectedSource,
        CancellationToken cancellationToken=default)
    {
        if(request==null || expectedSource==null || request.Write.ActorId==Guid.Empty
            || request.Write.Authority!=ExecutionStrategyAuthority || expectedSource.Authority!=ExecutionStrategyAuthority
            || request.Write.ReplicationEnrollment?.SourceStorageIdentity is {} enrolled
                && enrolled.BindingFingerprint!=expectedSource.BindingFingerprint)
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{IsError=true,ErrorCount=1,
                ErrorCode="COSMIC_EXECUTION_SOURCE_INVALID",Message="One original actor and matching native source are required."});
        return _holonRepository.ExecuteBoundConditionalHolonWriteAsync(request,expectedSource,cancellationToken);
    }
}
