using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryWindowTests
{
    [Fact]
    public void FluentCompositionUsesTheSameImmutableOperatorsWithoutIntermediateResultUnwrapping()
    {
        var query=CosmicQuery<Holon>.Create().Where(h=>h.IsActive)
            .OrderBy(h=>h.Name).ThenByDescending(h=>h.Version).Skip(20).Take(10);
        Assert.False(query.IsError,query.Message);Assert.Single(query.Result.Predicates);
        Assert.Equal(2,query.Result.Orderings.Count);Assert.Equal(20,query.Result.Offset);Assert.Equal(10,query.Result.Limit);
        var error=CosmicQuery<Holon>.Create().Take(-1);
        Assert.Same(error,error.OrderBy(h=>h.Name.Length).Skip(1).Take(1).Where(h=>h.IsActive));
        Assert.Equal("CQL_EXPRESSION_INVALID",error.ErrorCode);
    }

    [Theory]
    [InlineData("OrderBy")] [InlineData("OrderByDescending")] [InlineData("ThenBy")]
    [InlineData("ThenByDescending")] [InlineData("Skip")] [InlineData("Take")]
    public void QueryExposesExplicitOrderingAndPagingComposition(string method)
        => Assert.Contains(typeof(CosmicQuery<Holon>).GetMethods(),candidate=>candidate.Name==method);

    [Fact]
    public void OrderingCompositionIsImmutableAndOrderByResetsEarlierKeys()
    {
        var original=CosmicQuery<Holon>.Create();
        var first=original.OrderBy(h=>h.Name); Assert.False(first.IsError,first.Message);
        var second=first.Result.ThenByDescending(h=>h.Version); Assert.False(second.IsError,second.Message);
        Assert.Empty(original.Orderings);Assert.Single(first.Result.Orderings);
        Assert.Equal("Version",Assert.Single(second.Result.Orderings[1].Path.Segments));
        Assert.True(second.Result.Orderings[1].Descending);
        var reset=second.Result.OrderByDescending(h=>h.Description);
        Assert.Single(reset.Result.Orderings);Assert.Equal("Description",Assert.Single(reset.Result.Orderings[0].Path.Segments));
        Assert.Equal(2,second.Result.WithScope(CosmicQueryScope.AllEligible).Orderings.Count);
        Assert.Equal(2,second.Result.Where(h=>h.IsActive).Result.Orderings.Count);
    }

    [Fact]
    public void SequentialPagingMatchesTakeSkipAlgebraAndPreservesScope()
    {
        var query=CosmicQuery<Holon>.Create().OrderBy(h=>h.Id).Result;
        var page=query.Skip(10).Result.Take(20).Result.Skip(5).Result.Take(30).Result;
        Assert.Equal(15,page.Offset);Assert.Equal(15,page.Limit);
        Assert.Equal(0,query.Offset);Assert.Null(query.Limit);
        var empty=page.Skip(20).Result;
        Assert.Equal(0,empty.Limit);Assert.Equal(35,empty.Offset);
        var scoped=page.WithScope(CosmicQueryScope.AllEligible);
        Assert.Equal(15,scoped.Offset);Assert.Equal(15,scoped.Limit);Assert.Single(scoped.Orderings);
    }

    [Fact]
    public void UnsupportedPostPageCompositionCannotSilentlyChangeLinqMeaning()
    {
        var page=CosmicQuery<Holon>.Create().Take(3).Result;
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",page.Where(h=>h.Version>1).ErrorCode);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",page.OrderBy(h=>h.Name).ErrorCode);
        Assert.True(CosmicQuery<Holon>.Create().ThenBy(h=>h.Name).IsError);
        Assert.True(page.Skip(-1).IsError);Assert.True(page.Take(-1).IsError);
        Assert.True(page.Take(10001).IsError);
        Assert.True(CosmicQuery<Holon>.Create().Skip(CosmicQuery<Holon>.MaximumOffset).Result.Skip(1).IsError);
    }

    [Fact]
    public void OrderingRequiresExactFieldPermissionAndPagingCannotBeIgnored()
    {
        var name=new CosmicQueryFieldCapability("Name",typeof(string),Array.Empty<CosmicQueryComparisonOperator>(),Array.Empty<CosmicQueryStringOperator>(),supportsOrdering:true);
        var description=new CosmicQueryFieldCapability("Description",typeof(string),Array.Empty<CosmicQueryComparisonOperator>());
        var profile=new CosmicQueryCapabilities(CosmicQueryFeatures.Ordering,name,description);
        Assert.False(profile.Admit(CosmicQuery<Holon>.Create().OrderBy(h=>h.Name).Result).IsError);
        Assert.True(profile.Admit(CosmicQuery<Holon>.Create().OrderBy(h=>h.Description).Result).IsError);
        Assert.True(profile.Admit(CosmicQuery<Holon>.Create().Take(0).Result).IsError);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.None,name).Admit(CosmicQuery<Holon>.Create().OrderBy(h=>h.Name).Result).IsError);
    }

    [Fact]
    public void ScalarSelectorsRejectExecutableExpressionsAndPreserveMetadataPaths()
    {
        var rejected=CosmicQuery<Holon>.Create().OrderBy(h=>h.Name.Length);
        Assert.True(rejected.IsError);Assert.NotNull(rejected.Exception);
        Assert.True(CosmicQuery<Holon>.Create().OrderBy(h=>h.Version+1).IsError);
        Assert.True(CosmicQuery<Holon>.Create().OrderBy(h=>h.Name.ToLower()).IsError);
        var query=CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<long?>(h,"stats","used"));
        Assert.False(query.IsError,query.Message);
        Assert.Equal(new[]{"stats","used"},query.Result.Orderings[0].Path.Segments);
    }

    [Fact]
    public void GlobalPagingCannotBePlannedAsIndependentProviderPages()
    {
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Take(10).Result;
        var profile=new CosmicQueryCapabilities(CosmicQueryFeatures.Paging,Array.Empty<CosmicQueryFieldCapability>());
        var targets=new[]{new CosmicQueryTarget(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS,profile),
            new CosmicQueryTarget(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,profile)};
        var plan=CosmicQueryPlanner.Plan(query,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,targets);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",plan.ErrorCode);
    }
}
