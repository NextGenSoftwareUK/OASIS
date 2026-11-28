using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS:ICosmicSetQueryStorageProvider
{
    public CosmicSetQueryCapabilities SetQueryCapabilities=>new(
        CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
        EntityCompositionCapabilities,supportsNestedSets:true);
    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicSetQueryAsync<T>(
        CosmicSetQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        =>ProjectCqlRowsAsync(ExecuteCqlReadAsync(plan?.Anchor,maximumResults,cancellationToken,set:plan));

    private static string CompileCqlSet<T>(NeoCqlSource left,CosmicSetQueryPlan<T> plan,int maximumResults,
        IReadOnlyList<CosmicProjectionColumn> projection=null,
        int maximumProjectionBytes=CosmicProjectionResults.MaximumPayloadLimit,bool countOnly=false) where T:IHolon
        =>CompileCqlSetTree(left,plan,maximumResults,projection,maximumProjectionBytes,countOnly);
}
