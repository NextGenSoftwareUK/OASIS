using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChainIndexedRowAdmissionTests
{
    [Theory,InlineData(false),InlineData(true)]
    public void BoundIndexedAdmissionRequiresActualSourceAndDoesNotEnrollNativeExecution(bool wrongIndex)
    {
        var source=new CosmicChainQuerySource(ProviderType.SolanaOASIS,"network",ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),2,new string('A',64),0);
        var requirement=new CosmicChainQueryReadRequirement(source,new(1,"block"),0,TimeSpan.FromMinutes(1));
        var capabilities=new CosmicQueryCapabilities(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>());
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolanaOASIS).Result).WithIndexedSource(requirement).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.SolanaOASIS,new[]{new CosmicQueryTarget(ProviderType.SolanaOASIS,capabilities,source)});
        Assert.False(plan.IsError,plan.Message);
        var actual=wrongIndex?new CosmicChainQuerySource(source.SourceProvider,source.NetworkIdentity,source.ExecutionProvider,Guid.NewGuid(),2,source.CatalogueHash,0):source;
        Assert.Equal(wrongIndex,capabilities.AdmitIndexedRows(plan.Result,actual).IsError);
        Assert.True(capabilities.AdmitPlan(plan.Result).IsError); // Native route stays closed.
    }
}
