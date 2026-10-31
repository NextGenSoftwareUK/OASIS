using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryRelationshipTests
{
    [Fact]
    public void ChildPredicateFreezesCapturedValuesAndRetainsActorBinding()
    {
        var name="first";var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var query=CosmicQuery<Holon>.Create().WithReadAccess(access).WhereAnyChild(child=>child.Name==name);
        Assert.False(query.IsError,query.Message);name="changed";
        Assert.Same(access,query.Result.ReadAccess);
        var child=Assert.IsType<CosmicQueryAnyChild>(query.Result.Predicates.Last());
        Assert.Equal("first",Assert.IsType<CosmicQueryComparison>(child.Predicate).Literal.Value);
    }

    [Fact]
    public void RelationshipFilterCannotMoveAcrossAnExistingPage()
    {
        var query=CosmicQuery<Holon>.Create().Take(1).WhereAnyChild(child=>child.IsActive);
        Assert.True(query.IsError);Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",query.ErrorCode);
    }

    [Fact]
    public void UnsupportedRelationshipCapabilityRejectsBeforeAnyAdapterExecution()
    {
        var query=CosmicQuery<Holon>.Create().WhereAnyChild(child=>child.Name=="match");
        Assert.False(query.IsError,query.Message);
        var scalarOnly=new CosmicQueryCapabilities(CosmicQueryFeatures.None,
            new CosmicQueryFieldCapability("Name",typeof(string),CosmicQueryComparisonOperator.Equal));
        var rejected=scalarOnly.Admit(query.Result);
        Assert.True(rejected.IsError);Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",rejected.ErrorCode);
    }
    [Fact]
    public async Task ActualAdapterCannotBorrowAPlannedChildExistsCapability()
    {
        var adapter=new ScalarAdapter();
        var query=CosmicQuery<Holon>.Create().WhereAnyChild(child=>child.Name=="match").Result;
        var planned=new CosmicQueryCapabilities(CosmicQueryFeatures.ChildExists,
            new CosmicQueryFieldCapability("Name",typeof(string),CosmicQueryComparisonOperator.Equal));
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,planned)});
        Assert.False(plan.IsError,plan.Message);
        var read=await CosmicQueryExecutor.ExecuteAsync(plan.Result,new[]{adapter});
        Assert.True(read.IsError);Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",read.ErrorCode);
        Assert.Equal(0,adapter.Calls);
    }

    private sealed class ScalarAdapter:ICosmicQueryStorageProvider
    {
        public int Calls;
        public ProviderType QueryProviderType=>ProviderType.MongoDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities=>new(CosmicQueryFeatures.None,
            new CosmicQueryFieldCapability("Name",typeof(string),CosmicQueryComparisonOperator.Equal));
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++;
            throw new InvalidOperationException("A scalar-only adapter must not be invoked.");
        }
    }

}
