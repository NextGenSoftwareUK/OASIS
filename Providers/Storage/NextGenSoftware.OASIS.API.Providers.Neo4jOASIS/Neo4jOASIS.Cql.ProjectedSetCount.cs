using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS:ICosmicQueryProjectedSetCountStorageProvider
{
    public bool SupportsProjectedSetCount=>true;
    public Task<OASISResult<CosmicProjectedSetPacket>> ExecuteCosmicProjectedSetCountAsync<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
        =>limits?.CountOnly==true?ExecuteCosmicProjectedSetAsync(plan,limits,cancellationToken)
            :Task.FromResult(new OASISResult<CosmicProjectedSetPacket>{IsError=true,ErrorCode="CQL_PLAN_INVALID",
                Message="Native selected count requires its explicit count-only execution limits."});
}
