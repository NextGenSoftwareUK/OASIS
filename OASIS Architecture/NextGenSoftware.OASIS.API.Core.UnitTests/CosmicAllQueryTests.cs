using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicAllQueryTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.Paging | CosmicQueryFeatures.Ordering,
        new[] { new CosmicQueryFieldCapability("Name", typeof(string), new[] { CosmicQueryComparisonOperator.Equal },
            Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true) });

    private sealed class Adapter : ICosmicQueryScalarStorageProvider
    {
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => Rows;
        public bool SupportsAll = true;
        public CosmicScalarCapabilities ScalarCapabilities => SupportsAll
            ? new(CosmicScalarOperation.All) : new(CosmicScalarOperation.Any);
        public long Counterexamples;
        public string? Failure;
        public int Calls;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("All must not enumerate a client-side row list.");

        public Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            Assert.Equal(CosmicScalarOperation.All, plan.Query.Operation);
            Assert.NotNull(plan.Query.AllPredicate);
            Assert.Null(plan.Query.Selector);
            return Task.FromResult(Failure == null ? CosmicScalarResults.FromCount(plan.Query, Counterexamples)
                : new OASISResult<TResult> { IsError = true, ErrorCode = Failure, Message = "native All failure" });
        }
    }

    private static CosmicScalarPlan<Holon, bool> Plan(Adapter adapter, CosmicScalarQuery<Holon, bool> query)
    {
        var result = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, adapter.ScalarCapabilities) });
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    [Theory]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    [InlineData(long.MaxValue, false)]
    public async Task AllIsTrueExactlyWhenNoCounterexampleExists(long count, bool expected)
    {
        var adapter = new Adapter { Counterexamples = count };
        var query = CosmicQuery<Holon>.Create().All(row => row.Name == "B").Result;
        var result = await CosmicScalarExecutor.ExecuteAsync(Plan(adapter, query), new[] { adapter });
        Assert.False(result.IsError, result.Message);
        Assert.True(result.IsLoaded);
        Assert.Equal(expected, Assert.Single(result.Result.Providers).Value);
        Assert.Equal(1, adapter.Calls);
    }

    [Fact]
    public void AllRetainsTheSourceWindowAndIndependentPredicate()
    {
        var source = CosmicQuery<Holon>.Create().OrderBy(row => row.Name).Skip(1).Take(2).Result;
        var query = source.All(row => row.Name == "B").Result;
        Assert.Same(source, query.Source);
        Assert.Equal(1, query.Source.Offset);
        Assert.Equal(2, query.Source.Limit);
        Assert.Empty(query.Source.Predicates);
        Assert.Single(query.AllPredicate.Predicates);
        Assert.Equal(0, query.AllPredicate.Offset);
        Assert.Null(query.AllPredicate.Limit);
    }

    [Fact]
    public async Task LiveCapabilityRejectionHappensBeforeAnyNativeCall()
    {
        var adapter = new Adapter();
        var plan = Plan(adapter, CosmicQuery<Holon>.Create().All(row => row.Name == "B").Result);
        adapter.SupportsAll = false;
        var result = await CosmicScalarExecutor.ExecuteAsync(plan, new[] { adapter });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task NativeFailureIsNotVacuousTruth()
    {
        var adapter = new Adapter { Failure = "ALL_NATIVE_FAILED" };
        var result = await CosmicScalarExecutor.ExecuteAsync(
            Plan(adapter, CosmicQuery<Holon>.Create().All(row => row.Name == "B").Result), new[] { adapter });
        Assert.Equal("ALL_NATIVE_FAILED", result.ErrorCode);
        Assert.False(result.IsLoaded);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task CancellationDoesNotProduceTrue()
    {
        var adapter = new Adapter();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await CosmicScalarExecutor.ExecuteAsync(
            Plan(adapter, CosmicQuery<Holon>.Create().All(row => row.Name == "B").Result), new[] { adapter }, cancelled.Token);
        Assert.True(result.IsError);
        Assert.False(result.IsLoaded);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void PredicateCapabilityIsCheckedSeparatelyFromTheSource()
    {
        var rowOnly = new CosmicQueryCapabilities(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        var result = CosmicScalarPlanner.Plan(CosmicQuery<Holon>.Create().All(row => row.Name == "B").Result,
            ProviderType.SQLLiteDBOASIS, new[] { new CosmicScalarTarget(ProviderType.SQLLiteDBOASIS, rowOnly,
                new CosmicScalarCapabilities(CosmicScalarOperation.All)) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
    }

    [Fact]
    public void BadInputAndFailedSourceDoNotBecomeAnUnfilteredAll()
    {
        var failed = new OASISResult<CosmicQuery<Holon>> { IsError = true, ErrorCode = "SOURCE_REJECTED" };
        Assert.Equal("SOURCE_REJECTED", failed.All(row => true).ErrorCode);
        Assert.True(CosmicQuery<Holon>.Create().All(null!).IsError);
        Assert.True(CosmicQuery<Holon>.Create().All(row => row.GetHashCode() == 0).IsError);
        var query = CosmicQuery<Holon>.Create().All(row => row.Name == "B").Result;
        Assert.Equal("CQL_DATA_INVALID", CosmicScalarResults.FromCount(query, -1).ErrorCode);
    }

    [Fact]
    public void AllDoesNotImplicitlyAddGroupedColumnSupport()
    {
        var capabilities = new CosmicScalarCapabilities(CosmicScalarOperation.All);
        Assert.True(capabilities.Admit(CosmicScalarOperation.All, null).IsError);
    }
}
