using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

/// <summary>Executable admission boundary for the open mixed native/indexed demo row.</summary>
public sealed class CosmicMixedFederationAdmissionTests
{
    private static CosmicChainQuerySource Source() => new(ProviderType.SolanaOASIS,"retained-network",
        ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),3,new string('A',64),3325);

    [Fact]
    public void MultiProviderSourceCannotImplicitlyApplyOneChainsIndexGrantToMongo()
    {
        var source=Source();
        var requirement=new CosmicChainQueryReadRequirement(source,new(3422,"acknowledged-block"),500,TimeSpan.FromMinutes(1));
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS,ProviderType.SolanaOASIS).Result);
        var captured=query.WithIndexedSource(requirement);
        Assert.Equal("CQL_SOURCE_SCOPE_INVALID",captured.ErrorCode);
        Assert.Null(captured.Result);
    }

    [Fact]
    public void NativeFederationDoesNotSilentlyTurnAnIndexedTargetIntoANativeAdapter()
    {
        var source=Source();
        var capabilities=new CosmicQueryCapabilities(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>());
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS,ProviderType.SolanaOASIS).Result);
        var options=CosmicFederationOptions.Create(maximumProviderAttempts:2).Result;
        var plan=CosmicFederationPlanner.Plan(query,options,ProviderType.MongoDBOASIS,new[]
        {
            new CosmicQueryTarget(ProviderType.MongoDBOASIS,capabilities),
            new CosmicQueryTarget(ProviderType.SolanaOASIS,capabilities,source)
        });
        Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",plan.ErrorCode);
        Assert.Null(plan.Result);
    }

    [Fact]
    public void ExplicitTargetGrantPreservesNativeMongoAndPinsOnlyTheIndexedSource()
    {
        var source=Source();
        var requirement=new CosmicChainQueryReadRequirement(source,new(3422,"acknowledged-block"),500,TimeSpan.FromMinutes(1));
        var capabilities=new CosmicQueryCapabilities(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>());
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS,ProviderType.SolanaOASIS).Result);
        var targets=new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,capabilities),new CosmicQueryTarget(ProviderType.SolanaOASIS,capabilities,source)};
        var bindings=new[]{new CosmicFederationIndexRequirement(requirement)};
        var result=CosmicFederationPlanner.Plan(query,CosmicFederationOptions.Create(maximumProviderAttempts:2).Result,
            ProviderType.MongoDBOASIS,targets,null,bindings);
        Assert.False(result.IsError,result.Message);
        Assert.Null(result.Result.Source.IndexedSource);
        Assert.Null(result.Result.Targets[0].Query.IndexedSource);
        Assert.Same(requirement,result.Result.Targets[1].Query.IndexedSource);
        Assert.Single(result.Result.Targets[1].Query.Scope.Providers);
        Assert.Equal(ProviderType.SolanaOASIS,result.Result.Targets[1].Query.Scope.Providers[0]);
        bindings[0]=new CosmicFederationIndexRequirement(new(Source(),new(3422,"other"),1,TimeSpan.FromSeconds(1)));
        Assert.Same(requirement,result.Result.Targets[1].Query.IndexedSource);
    }

    [Theory]
    [InlineData("missing")] [InlineData("duplicate")] [InlineData("foreign-index")]
    [InlineData("unrequested")]
    public void IncompleteOrForeignTargetGrantsNeverProduceAnExecutablePlan(string scenario)
    {
        var source=Source();
        var requirement=new CosmicChainQueryReadRequirement(scenario=="foreign-index"?Source():source,new(3422,"block"),500,TimeSpan.FromMinutes(1));
        var capabilities=new CosmicQueryCapabilities(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>());
        var scope=scenario=="unrequested"?CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result
            :CosmicQueryScope.Named(ProviderType.MongoDBOASIS,ProviderType.SolanaOASIS).Result;
        var query=CosmicQuery<Holon>.Create(scope);
        var targets=new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,capabilities),new CosmicQueryTarget(ProviderType.SolanaOASIS,capabilities,source)};
        var binding=new CosmicFederationIndexRequirement(requirement);
        var bindings=scenario=="missing"?Array.Empty<CosmicFederationIndexRequirement>()
            :scenario=="duplicate"?new[]{binding,binding}:new[]{binding};
        var result=CosmicFederationPlanner.Plan(query,CosmicFederationOptions.Create(maximumProviderAttempts:2).Result,
            ProviderType.MongoDBOASIS,targets,null,bindings);
        Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",result.ErrorCode);Assert.Null(result.Result);
    }

    [Fact]
    public async Task PlanningGrantDoesNotAuthorizeNativeAdaptersToExecuteIndexedWork()
    {
        var source=Source();
        var requirement=new CosmicChainQueryReadRequirement(source,new(3422,"block"),500,TimeSpan.FromMinutes(1));
        var capabilities=new CosmicQueryCapabilities(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>());
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS,ProviderType.SolanaOASIS).Result);
        var plan=CosmicFederationPlanner.Plan(query,CosmicFederationOptions.Create(maximumProviderAttempts:2).Result,
            ProviderType.MongoDBOASIS,new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,capabilities),
                new CosmicQueryTarget(ProviderType.SolanaOASIS,capabilities,source)},null,
            new[]{new CosmicFederationIndexRequirement(requirement)});
        Assert.False(plan.IsError,plan.Message);
        var mongo=new Moq.Mock<NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicQueryStorageProvider>();
        var solana=new Moq.Mock<NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicQueryStorageProvider>();
        mongo.SetupGet(adapter=>adapter.QueryProviderType).Returns(ProviderType.MongoDBOASIS);
        solana.SetupGet(adapter=>adapter.QueryProviderType).Returns(ProviderType.SolanaOASIS);
        mongo.SetupGet(adapter=>adapter.QueryCapabilities).Returns(capabilities);
        solana.SetupGet(adapter=>adapter.QueryCapabilities).Returns(capabilities);
        var result=await CosmicFederationExecutor.ExecuteAsync(plan.Result,new[]{mongo.Object,solana.Object});
        Assert.True(result.IsError);Assert.Null(result.Result);
        foreach(var adapter in new[]{mongo,solana})
            adapter.Verify(native=>native.ExecuteCosmicQueryAsync(Moq.It.IsAny<CosmicQueryPlan<Holon>>(),
                Moq.It.IsAny<int>(),Moq.It.IsAny<CancellationToken>()),Moq.Times.Never);
    }
}
