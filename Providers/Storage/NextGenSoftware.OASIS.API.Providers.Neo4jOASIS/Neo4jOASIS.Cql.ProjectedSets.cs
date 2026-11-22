using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS : ICosmicQueryProjectedSetStorageProvider
{
    public CosmicProjectedSetCapabilities ProjectedSetCapabilities=>new(
        CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
        CqlCompositionCapabilities.OrderingDomains.ToArray(),CqlCompositionCapabilities.OrderingDomains.ToArray(),
        CqlCompositionCapabilities);
    public async Task<OASISResult<CosmicProjectedSetPacket>> ExecuteCosmicProjectedSetAsync<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        if(plan==null || limits==null || !ReferenceEquals(plan.Options,limits.Options))
            return new(){IsError=true,ErrorCode="CQL_PLAN_INVALID",Message="A selected-set native read must retain its admitted limits."};
        var result=await ExecuteSelectedSnapshotAsync(plan.Left,null,ProjectedSetInputLimits(plan.Left.Options),
            cancellationToken,selectedSet:plan,setLimits:limits).ConfigureAwait(false);
        if(result.IsError)return new(){IsError=true,ErrorCode=result.ErrorCode,Message=result.Message,Exception=result.Exception};
        if(result.Result?.SetProof==null)return new(){IsError=true,ErrorCode="CQL_PROVIDER_RESULT_INVALID",
            Message="Native selected-set execution did not acknowledge its complete snapshot packet."};
        return new(result.Result.SetProof){IsLoaded=true};
    }
}
