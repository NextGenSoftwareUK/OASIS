using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    public async Task<OASISResult<long>> ExecuteCosmicSetCountAsync<T>(CosmicSetQueryPlan<T> plan,
        CancellationToken cancellationToken) where T:IHolon
    {
        var read=await ExecuteCqlReadAsync(plan?.Left,10000,cancellationToken,countOnly:true,set:plan).ConfigureAwait(false);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(read,new OASISResult<long>());
        if(!read.IsError)result.Result=read.Result.Count;
        return result;
    }
}
