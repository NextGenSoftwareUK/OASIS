using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS:ICosmicQueryKeyedEntityStorageProvider
{
    public CosmicProjectionCapabilities KeyedEntityCapabilities=>CqlProjectionCapabilities;

    public Task<OASISResult<CosmicKeyedEntityBatch>> ExecuteCosmicKeyedEntitiesAsync<T>(
        CosmicQueryPlan<T> plan,IReadOnlyList<CosmicQueryPath> keys,int maximumRows=1000,
        int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        if(!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Task.FromResult(new OASISResult<CosmicKeyedEntityBatch>
                {IsError=true,ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate SQLite before native keyed entity execution."});
        return repository.ExecuteCosmicKeyedEntitiesAsync(plan,keys,maximumRows,maximumPayloadBytes,cancellationToken);
    }
}
