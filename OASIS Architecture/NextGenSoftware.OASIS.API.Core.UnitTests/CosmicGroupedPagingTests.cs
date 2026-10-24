using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedPagingTests
{
    private static CosmicGroupedSelection<Holon, string, long> Query() => CosmicQuery<Holon>.Create()
        .Skip(3).Take(20).GroupBy(h => h.Name).Select(g => g.LongCount()).Result;

    private static CosmicGroupedCapabilities Profile(bool paging) => new(
        new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>()),
        new CosmicScalarCapabilities(new[] { CosmicScalarOperation.LongCount }), null, paging);

    [Fact]
    public void ComposedResultWindowDoesNotChangeSourceOrEarlierIntent()
    {
        var original = Query(); var paged = original.Take(5).Skip(2).Take(9).Result;
        Assert.Equal(2, paged.ResultOffset); Assert.Equal(3, paged.ResultLimit);
        Assert.Same(original.Source, paged.Source);
        Assert.Equal(3, paged.Source.Offset); Assert.Equal(20, paged.Source.Limit);
        Assert.Equal(0, original.ResultOffset); Assert.Null(original.ResultLimit);
        var reordered = paged.OrderByDescending(value => value).Result;
        Assert.Equal(2, reordered.ResultOffset); Assert.Equal(3, reordered.ResultLimit);
        Assert.Equal(1, reordered.Orderings.Count);
    }

    [Fact]
    public void SkipBeyondPriorTakeProducesExplicitEmptyWindow()
    {
        var query = Query().Take(2).Skip(3).Result;
        Assert.Equal(3, query.ResultOffset); Assert.Equal(0, query.ResultLimit);
        Assert.Equal(20, query.Source.Limit);
    }

    [Fact]
    public void InvalidOrExcessiveWindowsCannotOverflowOrBecomeSourcePages()
    {
        Assert.Equal("CQL_EXPRESSION_INVALID", Query().Skip(-1).ErrorCode);
        Assert.Equal("CQL_EXPRESSION_INVALID", Query().Take(-1).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", Query().Skip(int.MaxValue).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", Query().Take(int.MaxValue).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", Query().Skip(999).Skip(2).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", Query().Take(int.MaxValue).Skip(0).ErrorCode);
    }

    [Fact]
    public void PagingNeedsIndependentPermissionEvenForTakeZero()
    {
        Assert.False(Profile(false).Admit(Query()).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(false).Admit(Query().Take(0).Result).ErrorCode);
        Assert.False(Profile(true).Admit(Query().Take(0).Result).IsError);
    }

    [Fact]
    public void ActualAdapterCannotBorrowClaimedPagingPermission()
    {
        var source = new CosmicQueryCapabilities(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
        var plan = CosmicGroupedPlanner.Plan(Query().Take(1).Result, ProviderType.MongoDBOASIS,
            new[] { new CosmicGroupedTarget(ProviderType.MongoDBOASIS, source, Profile(true)) });
        Assert.False(plan.IsError, plan.Message);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Admit(plan.Result,
            ProviderType.MongoDBOASIS, source, Profile(false)).ErrorCode);
    }

    [Fact]
    public void NativeBufferMustRespectResultLimitIncludingEmptyWindow()
    {
        var rows = new[] { new CosmicGroupedRow(1, new object[] { "A" }, 1L),
            new CosmicGroupedRow(1, new object[] { "B" }, 1L) };
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicGroupedResults.Materialize(Query().Take(1).Result, rows).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicGroupedResults.Materialize(Query().Take(0).Result, rows).ErrorCode);
        Assert.False(CosmicGroupedResults.Materialize(Query().Take(0).Result, Array.Empty<CosmicGroupedRow>()).IsError);
    }
}
