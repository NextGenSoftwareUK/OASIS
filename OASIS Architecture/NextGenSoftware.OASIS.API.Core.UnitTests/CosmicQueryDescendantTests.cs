using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryDescendantTests
{
    [Theory]
    [InlineData(0, 1, "maximumDepth")] [InlineData(1, 0, "maximumNodes")]
    public void InvalidCapabilityBoundsIdentifyTheOffendingArgument(int depth, int nodes, string parameter)
        => Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CosmicQueryTraversalCapability(depth, nodes)).ParamName);

    [Theory]
    [InlineData(0, 1)] [InlineData(33, 1)] [InlineData(1, 0)] [InlineData(1, 10001)]
    public void TraversalRejectsUnboundedDepthOrWork(int depth, int nodes)
    {
        var invalid = CosmicQueryTraversalOptions.Create(depth, nodes);
        Assert.True(invalid.IsError);
        Assert.Equal("CQL_TRAVERSAL_OPTIONS_INVALID", invalid.ErrorCode);
    }

    [Fact]
    public void DescendantIntentFreezesValuesAndPreservesActorWithoutChangingOutputShape()
    {
        var name = "original";
        var actor = CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var bounds = CosmicQueryTraversalOptions.Create(4, 25).Result;
        var query = CosmicQuery<Holon>.Create().WithReadAccess(actor)
            .WhereAnyDescendant(child => child.Name == name, bounds);
        Assert.False(query.IsError, query.Message);
        name = "edited";
        var descendant = Assert.IsType<CosmicQueryAnyDescendant>(query.Result.Predicates.Last());
        Assert.Equal("original", Assert.IsType<CosmicQueryComparison>(descendant.Predicate).Literal.Value);
        Assert.Same(bounds, descendant.Traversal);
        Assert.Same(actor, query.Result.ReadAccess);
        Assert.Null(query.Result.GraphOptions);
    }

    [Fact]
    public void DescendantCompositionRequiresExplicitBoundsAndPrecedesTheWindow()
    {
        var query = CosmicQuery<Holon>.Create();
        Assert.Equal("CQL_TRAVERSAL_OPTIONS_INVALID", query.WhereAnyDescendant(child => child.IsActive, null!).ErrorCode);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED", query.Take(1)
            .WhereAnyDescendant(child => child.IsActive, CosmicQueryTraversalOptions.Create().Result).ErrorCode);
        var first = new OASISResult<CosmicQuery<Holon>> { IsError = true, ErrorCode = "original" };
        Assert.Same(first, first.WhereAnyDescendant(child => child.IsActive, null!));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void CapabilityRequiresBothItsFlagAndCertifiedBounds(int fault)
    {
        var flags = CosmicQueryFeatures.ChildExists | CosmicQueryFeatures.DescendantExists;
        CosmicQueryTraversalCapability? traversal = new(4, 100);
        if (fault == 0) flags = CosmicQueryFeatures.ChildExists;
        if (fault == 1) traversal = null;
        if (fault == 2) flags = CosmicQueryFeatures.DescendantExists;
        Assert.Throws<ArgumentException>(() => new CosmicQueryCapabilities(flags, Fields(),
            Array.Empty<CosmicQueryMetadataCapability>(), traversal!));
    }

    [Theory]
    [InlineData(2, 10, false)] [InlineData(3, 10, true)] [InlineData(2, 11, true)]
    public void AdmissionChecksRequestedDepthAndNodesAgainstTheActualProfile(int depth, int nodes, bool rejected)
    {
        var query = CosmicQuery<Holon>.Create().WhereAnyDescendant(child => child.Name == "match",
            CosmicQueryTraversalOptions.Create(depth, nodes).Result);
        Assert.False(query.IsError, query.Message);
        Assert.Equal(rejected, Capabilities(2, 10).Admit(query.Result).IsError);
    }

    [Fact]
    public void DirectChildCapabilityDoesNotGrantDescendantExecution()
    {
        var query = CosmicQuery<Holon>.Create().WhereAnyDescendant(child => child.Name == "match",
            CosmicQueryTraversalOptions.Create().Result);
        var leaf = new CosmicQueryCapabilities(CosmicQueryFeatures.ChildExists, Fields());
        var result = leaf.Admit(query.Result);
        Assert.True(result.IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
    }

    [Fact]
    public async Task ActualAdapterCannotBorrowPlannedTraversalDepth()
    {
        var query = CosmicQuery<Holon>.Create().WhereAnyDescendant(child => child.Name == "match",
            CosmicQueryTraversalOptions.Create(4, 10).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Capabilities(8, 100)) });
        Assert.False(plan.IsError, plan.Message);
        var adapter = new LimitedAdapter();
        var read = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new[] { adapter });
        Assert.True(read.IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", read.ErrorCode);
        Assert.Equal(0, adapter.Calls);
    }

    private static CosmicQueryFieldCapability[] Fields()
        => new[] { new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal) };
    private static CosmicQueryCapabilities Capabilities(int depth, int nodes)
        => new(CosmicQueryFeatures.ChildExists | CosmicQueryFeatures.DescendantExists, Fields(),
            Array.Empty<CosmicQueryMetadataCapability>(), new CosmicQueryTraversalCapability(depth, nodes));

    private sealed class LimitedAdapter : ICosmicQueryStorageProvider
    {
        internal int Calls;
        public ProviderType QueryProviderType => ProviderType.MongoDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => Capabilities(2, 10);
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            throw new InvalidOperationException("Rejected traversal must not dispatch.");
        }
    }
}
