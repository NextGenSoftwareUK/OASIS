using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryGraphOptionsTests
{
    [Theory]
    [InlineData(true, 0, 0, true)]
    [InlineData(true, 0, 100, true)]
    [InlineData(true, 1, 0, true)]
    [InlineData(true, 1, 1, false)]
    [InlineData(true, 2, 1, true)]
    [InlineData(true, 2, 2, false)]
    [InlineData(false, 0, 0, true)]
    [InlineData(false, 0, 1, false)]
    [InlineData(false, 2, 1, false)]
    public void DepthIsMeasuredFromQueryRoots(bool recursive, int maximumDepth, int depth, bool expand)
    {
        var options = CosmicQueryGraphOptions.Create(recursive: recursive, maxChildDepth: maximumDepth);

        Assert.False(options.IsError, options.Message);
        Assert.Equal(expand, options.Result.IncludesChildrenAtDepth(depth));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Result.IncludesChildrenAtDepth(-1));
    }

    [Theory]
    [InlineData(-1, 100, 1024, 1)]
    [InlineData(10001, 100, 1024, 1)]
    [InlineData(0, 0, 1024, 1)]
    [InlineData(0, 10001, 1024, 1)]
    [InlineData(0, 100, 0, 1)]
    [InlineData(0, 100, 33554433, 1)]
    [InlineData(0, 100, 1024, -1)]
    public void InvalidLimitsAndChildTypesReject(int depth, int nodes, int bytes, int type)
    {
        var result = CosmicQueryGraphOptions.Create(maxChildDepth: depth, maximumNodes: nodes,
            maximumPayloadBytes: bytes, childType: (HolonType)type);

        Assert.True(result.IsError);
        Assert.Equal("CQL_GRAPH_OPTIONS_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public void CompositionPreservesGraphOptionsWithoutChangingTheSource()
    {
        var source = CosmicQuery<Holon>.Create();
        var options = CosmicQueryGraphOptions.Create(maxChildDepth: 2, childType: HolonType.Quest).Result;
        var included = source.WithChildren(options);
        var page = included.Where(row => row.IsActive)
            .OrderBy(row => row.Name).ThenBy(row => row.Id).Skip(2).Take(3);

        Assert.False(page.IsError, page.Message);
        Assert.Null(source.GraphOptions);
        Assert.Same(options, included.Result.GraphOptions);
        Assert.Same(options, page.Result.GraphOptions);
        Assert.Same(options, page.Result.WithScope(CosmicQueryScope.AllEligible).GraphOptions);
        Assert.Equal(2, page.Result.Offset);
        Assert.Equal(3, page.Result.Limit);
        Assert.Equal(HolonType.Quest, options.ChildType);

        var leaf = page.Result.WithoutChildren();
        Assert.Null(leaf.GraphOptions);
        Assert.Same(options, page.Result.GraphOptions);
        Assert.Equal(page.Result.Offset, leaf.Offset);
        Assert.Equal(page.Result.Limit, leaf.Limit);
        Assert.Null(page.WithoutChildren().Result.GraphOptions);
    }

    [Fact]
    public void GraphShapeCanBeSelectedAfterRootPagingAndFirstErrorsArePreserved()
    {
        var options = CosmicQueryGraphOptions.Create().Result;
        var page = CosmicQuery<Holon>.Create().Take(1).WithChildren(options);

        Assert.False(page.IsError, page.Message);
        Assert.Same(options, page.Result.GraphOptions);
        Assert.Equal(1, page.Result.Limit);

        var failed = CosmicQuery<Holon>.Create().Skip(-1);
        Assert.Same(failed, failed.WithChildren(options));
        Assert.Same(failed, failed.WithoutChildren());
        Assert.True(CosmicQuery<Holon>.Create().WithChildren(null).IsError);
    }

    [Fact]
    public async Task AnUnsupportedGraphCannotReachAdapterIo()
    {
        var adapter = new LeafAdapter();
        var query = CosmicQuery<Holon>.Create().WithChildren(CosmicQueryGraphOptions.Create().Result).Result;
        var targets = new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) };
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, targets);

        Assert.True(plan.IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", plan.ErrorCode);
        Assert.Equal(0, adapter.Calls);

        // A caller-provided capability profile must not bypass actual adapter admission.
        var claimed = new CosmicQueryCapabilities(CosmicQueryFeatures.GraphExpansion,
            Array.Empty<CosmicQueryFieldCapability>());
        var claimedPlan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, claimed) });
        Assert.False(claimedPlan.IsError, claimedPlan.Message);
        var executed = await CosmicQueryExecutor.ExecuteAsync(claimedPlan.Result, new[] { adapter });

        Assert.True(executed.IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", executed.ErrorCode);
        Assert.Null(executed.Result);
        Assert.Equal(0, adapter.Calls);
    }

    private sealed class LeafAdapter : ICosmicQueryStorageProvider
    {
        public int Calls;
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(CosmicQueryFeatures.None,
            Array.Empty<CosmicQueryFieldCapability>());

        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            throw new InvalidOperationException("Unsupported graph must reject before native I/O.");
        }
    }
}
