using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
public partial class MongoDBOASIS : ICosmicQueryProjectedSetStorageProvider
{
    public CosmicProjectedSetCapabilities ProjectedSetCapabilities=>new(
        CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
        CqlCompositionCapabilities.OrderingDomains.ToArray(),CqlCompositionCapabilities.OrderingDomains.ToArray(),
        CqlCompositionCapabilities,supportsNestedSets:true,entityInputs:EntityCompositionCapabilities);
    public Task<OASISResult<CosmicProjectedSetPacket>> ExecuteCosmicProjectedSetAsync<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
        =>ExecuteSelectedCqlAsync(async()=>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(plan==null || limits==null || !ReferenceEquals(plan.Options,limits.Options))
                return SelectedCqlFailure<CosmicProjectedSetPacket>("CQL_PLAN_INVALID","A selected-set plan must retain its admitted limits.");
            var actual=CosmicProjectedSetPlanner.Plan(plan.Query,Core.Enums.ProviderType.MongoDBOASIS,
                new CosmicProjectionCompositionTarget(Core.Enums.ProviderType.MongoDBOASIS,CqlCapabilities,
                    CqlProjectionCapabilities,CqlCompositionCapabilities),ProjectedSetCapabilities,plan.Options);
            if(actual.IsError)return SelectedCqlFailure<CosmicProjectedSetPacket>(actual.ErrorCode,actual.Message,actual.Exception);
            if(!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return SelectedCqlFailure<CosmicProjectedSetPacket>("CQL_PROVIDER_NOT_READY","Activate MongoDB before native selected-value sets.");
            var packet=await repository.ExecuteNativeProjectedSetAsync(actual.Result,limits,cancellationToken).ConfigureAwait(false);
            return new OASISResult<CosmicProjectedSetPacket>(packet){IsLoaded=true};
        });
}
