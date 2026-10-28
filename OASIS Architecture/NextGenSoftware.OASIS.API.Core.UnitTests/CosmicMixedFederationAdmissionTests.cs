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
}
