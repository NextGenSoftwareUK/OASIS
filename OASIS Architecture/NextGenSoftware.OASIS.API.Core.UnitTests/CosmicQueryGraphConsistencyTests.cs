using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryGraphConsistencyTests
{
    private static CosmicQueryCapabilities Capabilities(bool snapshot)
        => new(CosmicQueryFeatures.GraphExpansion | (snapshot ? CosmicQueryFeatures.GraphSnapshot : 0),
            Array.Empty<Type>(), Array.Empty<CosmicQueryComparisonOperator>());

    [Fact]
    public void ExistingFactorySignaturePreservesProviderDefaultBehavior()
    {
        var signature=typeof(CosmicQueryGraphOptions).GetMethod("Create",
            new[]{typeof(bool),typeof(int),typeof(HolonType),typeof(int),typeof(int)});
        Assert.NotNull(signature);
        var created=(OASISResult<CosmicQueryGraphOptions>)signature.Invoke(null,new object[]{true,0,HolonType.All,1000,32000})!;
        Assert.False(created.IsError,created.Message);
        Assert.Equal(CosmicQueryGraphConsistency.ProviderDefault,created.Result.Consistency);
    }

    [Theory]
    [InlineData(-1)] [InlineData(2)]
    public void UndefinedConsistencyCannotBecomeAQuery(int value)
    {
        var created=CosmicQueryGraphOptions.Create((CosmicQueryGraphConsistency)value);
        Assert.True(created.IsError);
        Assert.Equal("CQL_GRAPH_OPTIONS_INVALID",created.ErrorCode);
        Assert.Null(created.Result);
    }

    [Fact]
    public void StrongRequestSurvivesEveryImmutableCompositionStep()
    {
        var options=CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,maxChildDepth:2).Result;
        var source=CosmicQuery<Holon>.Create();
        var shaped=source.WithChildren(options).Where(holon=>holon.IsActive).OrderBy(holon=>holon.Name).Skip(1).Take(2);
        Assert.False(shaped.IsError,shaped.Message);
        Assert.Null(source.GraphOptions);
        Assert.Same(options,shaped.Result.GraphOptions);
        Assert.Equal(CosmicQueryGraphConsistency.ProviderSnapshot,shaped.Result.GraphOptions.Consistency);
        Assert.Null(shaped.Result.WithoutChildren().GraphOptions);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SnapshotAdmissionRequiresTheActualDeclaredStrength(bool snapshot)
    {
        var options=CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result;
        var query=CosmicQuery<Holon>.Create().WithChildren(options).Result;
        var admitted=Capabilities(snapshot).Admit(query);
        Assert.Equal(!snapshot,admitted.IsError);
        if(!snapshot)Assert.Equal("CQL_CONSISTENCY_UNSUPPORTED",admitted.ErrorCode);
    }

    [Fact]
    public void SnapshotCapabilityCannotExistWithoutGraphExpansion()
        =>Assert.Throws<ArgumentException>(()=>new CosmicQueryCapabilities(CosmicQueryFeatures.GraphSnapshot,
            Array.Empty<Type>(),Array.Empty<CosmicQueryComparisonOperator>()));

    private sealed class ReadCommittedAdapter:ICosmicQueryStorageProvider
    {
        public int Calls;
        public ProviderType QueryProviderType=>ProviderType.Neo4jOASIS;
        public CosmicQueryCapabilities QueryCapabilities=>Capabilities(false);
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++;
            return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>()){IsLoaded=true});
        }
    }

    [Fact]
    public async Task CallerProfileCannotUpgradeAnExecutionAdapterToSnapshots()
    {
        var options=CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result;
        var query=CosmicQuery<Holon>.Create().WithChildren(options).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.Neo4jOASIS,
            new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Capabilities(true))});
        Assert.False(plan.IsError,plan.Message);
        var adapter=new ReadCommittedAdapter();
        var result=await CosmicQueryExecutor.ExecuteAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter});
        Assert.True(result.IsError);
        Assert.Equal("CQL_CONSISTENCY_UNSUPPORTED",result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(0,adapter.Calls);
    }
}
