using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS:ICosmicSetCountQueryStorageProvider
{
    public bool SupportsSetCount=>true;
    public async Task<OASISResult<long>> ExecuteCosmicSetCountAsync<T>(CosmicSetQueryPlan<T> plan,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        var read=await ExecuteCqlReadAsync(plan?.Anchor,10000,cancellationToken,countOnly:true,set:plan).ConfigureAwait(false);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(read,new OASISResult<long>());
        if(!read.IsError)result.Result=read.Result.Count;
        return result;
    }
}
