using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicStringPointLookupTests
{
    private static CosmicQueryCapabilities Capabilities => CosmicQueryCapabilities.ForStringMetadataPointLookup("native-key",8);
    private static CosmicQuery<Holon> Query(string key="123") => CosmicQuery<Holon>.Create(
        CosmicQueryScope.Named(ProviderType.ActivityPubOASIS).Result)
        .Where(h=>CosmicQueryMetadata.Value<string>(h,"native-key")==key).Result;

    [Fact]
    public void ExactKeyAndReadAccessAreSeparateAndImmutableDuringPlanning()
    {
        var query=Query();
        var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var guarded=query.WithReadAccess(access).Result;
        Assert.Single(query.Predicates);Assert.Equal(2,guarded.Predicates.Count);
        Assert.Single(guarded.BusinessPredicates);
        Assert.Equal("123",Capabilities.StringPointLookup.SelectKey(guarded).Result);
        var plan=CosmicQueryPlanner.Plan(guarded,ProviderType.ActivityPubOASIS,
            new[]{new CosmicQueryTarget(ProviderType.ActivityPubOASIS,Capabilities)});
        Assert.False(plan.IsError,plan.Message);
        Assert.Same(access,plan.Result.Query.ReadAccess);
        Assert.False(Capabilities.Admit(Query().Skip(1).Take(0).Result).IsError);
        var expanded=Query().WithScope(CosmicQueryScope.AllEligible);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",CosmicQueryPlanner.Plan(expanded,ProviderType.ActivityPubOASIS,
            new[]{new CosmicQueryTarget(ProviderType.ActivityPubOASIS,Capabilities),
                new CosmicQueryTarget(ProviderType.SolanaOASIS,Capabilities)}).ErrorCode);
    }

    [Theory]
    [InlineData("empty")] [InlineData("wrong-path")] [InlineData("extra-predicate")]
    [InlineData("or")] [InlineData("order")] [InlineData("graph")]
    [InlineData("null")] [InlineData("not-equal")] [InlineData("nested-path")]
    public void UnsupportedShapesRejectAtPlanningRatherThanAfterSourceReads(string shape)
    {
        var root=CosmicQuery<Holon>.Create();
        var query=shape switch {
            "empty"=>root,
            "wrong-path"=>root.Where(h=>CosmicQueryMetadata.Value<string>(h,"other")=="123").Result,
            "extra-predicate"=>Query().Where(h=>h.IsPublic).Result,
            "or"=>root.Where(h=>CosmicQueryMetadata.Value<string>(h,"native-key")=="123"||h.IsPublic).Result,
            "order"=>Query().OrderBy(h=>h.Id).Result,
            "graph"=>Query().WithChildren(CosmicQueryGraphOptions.Create().Result).Result,
            "null"=>root.Where(h=>CosmicQueryMetadata.Value<string>(h,"native-key")==null).Result,
            "not-equal"=>root.Where(h=>CosmicQueryMetadata.Value<string>(h,"native-key")!="123").Result,
            _=>root.Where(h=>CosmicQueryMetadata.Value<string>(h,"nested","native-key")=="123").Result
        };
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Capabilities.Admit(query).ErrorCode);
    }

    [Theory]
    [InlineData("")] [InlineData("123456789")] [InlineData("a b")] [InlineData("a\nb")] [InlineData("é")]
    public void PointKeysAreBoundedAndCannotContainControlOrAmbiguousWhitespace(string key)
        => Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Capabilities.StringPointLookup.SelectKey(Query(key)).ErrorCode);
}
