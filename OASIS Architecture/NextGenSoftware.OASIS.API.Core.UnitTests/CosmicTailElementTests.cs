using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicTailElementTests
{
    private sealed class Adapter : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public bool TailSupported = true;
        public CosmicQueryCapabilities QueryCapabilities => new(
            CosmicQueryFeatures.Paging | CosmicQueryFeatures.Ordering
                | (TailSupported ? CosmicQueryFeatures.TailPaging : CosmicQueryFeatures.None),
            new[] { new CosmicQueryFieldCapability("Name", typeof(string), Array.Empty<CosmicQueryComparisonOperator>(),
                Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true) });
        public int Calls;
        public CosmicQueryPlan<Holon>? Received;
        public string? Failure;

        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            Assert.Equal(1, maximumResults);
            Assert.Equal(typeof(Holon), typeof(T));
            Received = (CosmicQueryPlan<Holon>)(object)plan;
            if (Failure != null) return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>
                { IsError = true, ErrorCode = Failure, Message = "native tail failure" });
            IEnumerable<string> names = new[] { "A", "B", "C", "D" };
            if (plan.Query.Orderings.FirstOrDefault()?.Descending == true) names = names.Reverse();
            names = names.Skip(plan.Query.Offset);
            if (plan.Query.Limit.HasValue) names = names.Take(plan.Query.Limit.Value);
            if (plan.TailIndex.HasValue) names = names.Reverse().Skip(plan.TailIndex.Value - 1).Take(1);
            IReadOnlyList<IHolon> rows = names.Select(name => (IHolon)new Holon { Id = Guid.NewGuid(), Name = name }).ToArray();
            return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(rows) { IsLoaded = true });
        }
    }

    private static HolonManager Manager()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }

    private static CosmicElementPlan<Holon> Plan(Adapter adapter, CosmicElementQuery<Holon> query)
    {
        var result = CosmicElementPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    [Theory]
    [InlineData(false, 0, 4, 1, "D")]
    [InlineData(false, 0, 2, 1, "B")]
    [InlineData(false, 1, 2, 1, "C")]
    [InlineData(false, 1, 2, 2, "B")]
    [InlineData(true, 1, 2, 1, "B")]
    [InlineData(true, 1, 2, 2, "C")]
    public async Task TailSelectionStaysAfterTheOriginalSourceWindow(bool descending, int skip, int take, int fromEnd, string expected)
    {
        var adapter = new Adapter();
        var source = (descending ? CosmicQuery<Holon>.Create().OrderByDescending(h => h.Name)
            : CosmicQuery<Holon>.Create().OrderBy(h => h.Name)).Skip(skip).Take(take).Result;
        var plan = Plan(adapter, source.ElementAt(new Index(fromEnd, fromEnd: true)).Result);
        Assert.Same(source, plan.Rows.Query);
        Assert.Equal(fromEnd, plan.Rows.TailIndex);
        var result = await Manager().ExecuteElementQueryAsync(plan, new[] { adapter });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(expected, result.Result.Name);
        Assert.Equal(1, adapter.Calls);
        Assert.Equal(skip, adapter.Received!.Query.Offset);
        Assert.Equal(take, adapter.Received.Query.Limit);
        Assert.Equal(fromEnd, adapter.Received.TailIndex); // Single-target binding must retain it.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LastUsesTheSameNativeTailShape(bool orDefault)
    {
        var adapter = new Adapter();
        var source = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(1).Result;
        var query = (orDefault ? source.LastOrDefault() : source.Last()).Result;
        var result = await Manager().ExecuteElementQueryAsync(Plan(adapter, query), new[] { adapter });
        Assert.False(result.IsError, result.Message);
        Assert.Equal("D", result.Result.Name);
        Assert.Null(adapter.Received!.Query.Limit);
        Assert.Equal(1, adapter.Received.TailIndex);
    }

    [Theory]
    [InlineData(false, "CQL_SEQUENCE_EMPTY")]
    [InlineData(true, null)]
    public async Task EmptyLastHasItsDeclaredCardinalityWithoutIo(bool orDefault, string? error)
    {
        var adapter = new Adapter { Failure = "MUST_NOT_READ" };
        var source = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Take(0).Result;
        var query = (orDefault ? source.LastOrDefault() : source.Last()).Result;
        var result = await Manager().ExecuteElementQueryAsync(Plan(adapter, query), new[] { adapter });
        Assert.Equal(error != null, result.IsError);
        if (error != null) Assert.Equal(error, result.ErrorCode);
        else Assert.True(result.IsLoaded);
        Assert.Null(result.Result);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task ZeroFromEndAndPastExplicitWindowRemainPureDefaults()
    {
        var adapter = new Adapter { Failure = "MUST_NOT_READ" };
        var source = CosmicQuery<Holon>.Create().Take(1).Result;
        Assert.Equal("CQL_INDEX_OUT_OF_RANGE", source.ElementAt(^0).ErrorCode);
        foreach (var index in new[] { ^0, ^2, new Index(int.MaxValue, true) })
        {
            var result = await Manager().ExecuteElementQueryAsync(
                Plan(adapter, source.ElementAtOrDefault(index).Result), new[] { adapter });
            Assert.False(result.IsError, result.Message);
            Assert.True(result.IsLoaded);
            Assert.Null(result.Result);
        }
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void LastRequiresOrderAndTailRequiresExplicitCapability()
    {
        var adapter = new Adapter { TailSupported = false };
        var source = CosmicQuery<Holon>.Create();
        var targets = new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) };
        Assert.Equal("CQL_ORDER_REQUIRED", CosmicElementPlanner.Plan(source.Last().Result, adapter.QueryProviderType, targets).ErrorCode);
        var ordered = source.OrderBy(h => h.Name).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicElementPlanner.Plan(ordered.Last().Result, adapter.QueryProviderType, targets).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", CosmicElementPlanner.Plan(source.ElementAt(new Index(int.MaxValue, true)).Result,
            adapter.QueryProviderType, targets).ErrorCode);
    }

    [Fact]
    public async Task LiveReadmissionCannotSilentlyIgnoreTailShape()
    {
        var adapter = new Adapter();
        var plan = Plan(adapter, CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Last().Result);
        adapter.TailSupported = false;
        var result = await Manager().ExecuteElementQueryAsync(plan, new[] { adapter });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.False(result.IsLoaded);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task LastOrDefaultDoesNotHideNativeFailure()
    {
        var adapter = new Adapter { Failure = "TAIL_NATIVE_FAILURE" };
        var result = await Manager().ExecuteElementQueryAsync(
            Plan(adapter, CosmicQuery<Holon>.Create().OrderBy(h => h.Name).LastOrDefault().Result), new[] { adapter });
        Assert.Equal("TAIL_NATIVE_FAILURE", result.ErrorCode);
        Assert.False(result.IsLoaded);
        Assert.Null(result.Result);
    }

    [Fact]
    public void FailedSourcesRemainFailedAndUnorderedFederationIsNotInvented()
    {
        var failed = new OASISResult<CosmicQuery<Holon>> { IsError = true, ErrorCode = "SOURCE_REJECTED" };
        Assert.Equal("SOURCE_REJECTED", failed.Last().ErrorCode);
        Assert.Equal("SOURCE_REJECTED", failed.LastOrDefault().ErrorCode);
        Assert.Equal("SOURCE_REJECTED", failed.ElementAt(^1).ErrorCode);
        Assert.Equal("SOURCE_REJECTED", failed.ElementAtOrDefault(^1).ErrorCode);
        var adapter = new Adapter();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).OrderBy(h => h.Name).Last().Result;
        var multiple = CosmicElementPlanner.Plan(query, adapter.QueryProviderType, new[]
        {
            new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities),
            new CosmicQueryTarget(ProviderType.MongoDBOASIS, adapter.QueryCapabilities)
        });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", multiple.ErrorCode);
        Assert.Equal(0, adapter.Calls);
    }
}
