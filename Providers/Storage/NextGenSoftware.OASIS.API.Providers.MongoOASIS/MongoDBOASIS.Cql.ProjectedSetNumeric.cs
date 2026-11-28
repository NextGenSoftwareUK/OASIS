using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
public partial class MongoDBOASIS:ICosmicQueryProjectedSetNumericStorageProvider
{
    public bool SupportsProjectedSetNumeric=>true;
    public Task<OASISResult<CosmicProjectedSetPacket>> ExecuteCosmicProjectedSetNumericAsync<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
        =>limits?.NumericReduction!=null&&limits.CountOnly?ExecuteCosmicProjectedSetAsync(plan,limits,cancellationToken)
            :Task.FromResult(new OASISResult<CosmicProjectedSetPacket>{IsError=true,ErrorCode="CQL_PLAN_INVALID",
                Message="Native selected numeric reduction requires its exact accumulator intent and scalar-only execution limits."});
}
