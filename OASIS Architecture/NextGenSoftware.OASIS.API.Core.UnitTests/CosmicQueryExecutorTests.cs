using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryExecutorTests
{
    private static CosmicQueryCapabilities NameCapability => new(CosmicQueryFeatures.None,
        new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal));
    private static CosmicQueryPlan<Holon> Plan(CosmicQuery<Holon> query, params ProviderType[] providers)
    {
        var result = CosmicQueryPlanner.Plan(query, providers[0], providers.Select(p => new CosmicQueryTarget(p, NameCapability)).ToArray());
        Assert.False(result.IsError, result.Message); return result.Result;
    }
    private static OASISResult<IReadOnlyList<IHolon>> Rows(params IHolon[] rows) => new(rows);
    private sealed class Adapter : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType { get; }
        public CosmicQueryCapabilities QueryCapabilities { get; set; } = NameCapability;
        public int Calls; public int LastBudget; public int LastTargets;
        public Func<CancellationToken, Task<OASISResult<IReadOnlyList<IHolon>>>> Handler = _ => Task.FromResult(Rows());
        public Adapter(ProviderType provider) => QueryProviderType = provider;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan, int maximumResults = 100,
            CancellationToken cancellationToken = default) where T : IHolon
        { Calls++; LastBudget = maximumResults; LastTargets = plan.Targets.Count; return Handler(cancellationToken); }
    }

    [Fact]
    public async Task EveryActualAdapterIsPreflightedBeforeAnyRead()
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Where(h => h.Name == "coffee").Result;
        var first = new Adapter(ProviderType.SQLLiteDBOASIS);
        var second = new Adapter(ProviderType.MongoDBOASIS) { QueryCapabilities = new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>()) };
        var result = await CosmicQueryExecutor.ExecuteAsync(Plan(query, first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.True(result.IsError); Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode); Assert.Null(result.Result);
        Assert.Equal(0, first.Calls); Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task MissingAndDuplicateRegistryBindingsRejectWithoutIOMutation()
    {
        var adapter = new Adapter(ProviderType.SQLLiteDBOASIS); var plan = Plan(CosmicQuery<Holon>.Create(), adapter.QueryProviderType);
        var duplicate = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { adapter, adapter });
        Assert.Equal("CQL_EXECUTION_INVALID", duplicate.ErrorCode);
        var missing = await CosmicQueryExecutor.ExecuteAsync(plan, Array.Empty<ICosmicQueryStorageProvider>());
        Assert.Equal("CQL_PLAN_TARGET_UNAVAILABLE", missing.ErrorCode); Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task ResultsKeepProviderProvenanceAndDetachCanonicalValuesWithoutDeduplication()
    {
        var id = Guid.NewGuid(); var original = new Holon { Id = id, Name = "one", Version = 1, MetaData = new() { ["rating"] = 7 } };
        var first = new Adapter(ProviderType.SQLLiteDBOASIS) { Handler = _ => Task.FromResult(Rows(original)) };
        var second = new Adapter(ProviderType.MongoDBOASIS) { Handler = _ => Task.FromResult(Rows(new Holon { Id = id, Name = "two", Version = 2 })) };
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var result = await CosmicQueryExecutor.ExecuteAsync(Plan(query, first.QueryProviderType, second.QueryProviderType), new[] { second, first });
        Assert.False(result.IsError, result.Message); Assert.Equal(2, result.Result.TotalRows); Assert.Equal(2, result.Result.Providers.Count);
        Assert.Equal(ProviderType.SQLLiteDBOASIS, result.Result.Providers[0].Provider);
        var copy = Assert.Single(result.Result.Providers[0].Rows); Assert.NotSame(original, copy);
        original.Name = "changed"; original.MetaData["rating"] = 99;
        Assert.Equal("one", copy.Name); Assert.Equal("7", copy.MetaData["rating"].ToString());
        Assert.Equal(2, Assert.Single(result.Result.Providers[1].Rows).Version); Assert.Equal(1, first.LastTargets); Assert.Equal(1, second.LastTargets);
    }

    [Fact]
    public async Task TotalBudgetAndAdapterOverdeliveryNeverPublishPartialResults()
    {
        var first = new Adapter(ProviderType.SQLLiteDBOASIS) { Handler = _ => Task.FromResult(Rows(new Holon { Id = Guid.NewGuid() })) };
        var second = new Adapter(ProviderType.MongoDBOASIS) { Handler = _ => Task.FromResult(Rows(new Holon { Id = Guid.NewGuid() })) };
        var plan = Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible), first.QueryProviderType, second.QueryProviderType);
        var overflow = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { first, second }, maximumResults: 1);
        Assert.Equal("CQL_QUERY_LIMIT", overflow.ErrorCode); Assert.Null(overflow.Result); Assert.Equal(1, second.LastBudget);
        first.Handler = _ => Task.FromResult(Rows(new Holon { Id = Guid.NewGuid() }, new Holon { Id = Guid.NewGuid() }));
        var overdelivery = await CosmicQueryExecutor.ExecuteAsync(Plan(CosmicQuery<Holon>.Create(), first.QueryProviderType), new[] { first }, maximumResults: 1);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", overdelivery.ErrorCode); Assert.Null(overdelivery.Result);
    }

    [Fact]
    public async Task NativeErrorsAndThrownCausesRemainExplicitWithoutPartialSuccess()
    {
        var cause = new InvalidOperationException("native failure");
        var first = new Adapter(ProviderType.SQLLiteDBOASIS);
        var second = new Adapter(ProviderType.MongoDBOASIS) { Handler = _ => Task.FromResult(new OASISResult<IReadOnlyList<IHolon>> {
            IsError = true, ErrorCode = "NATIVE_TEST_FAILURE", Message = "read failed", Exception = cause }) };
        var plan = Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible), first.QueryProviderType, second.QueryProviderType);
        var failure = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { first, second });
        Assert.Equal("NATIVE_TEST_FAILURE", failure.ErrorCode); Assert.Same(cause, failure.Exception); Assert.Null(failure.Result);
        second.Handler = _ => Task.FromException<OASISResult<IReadOnlyList<IHolon>>>(cause);
        var thrown = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { first, second });
        Assert.Equal("CQL_QUERY_FAILED", thrown.ErrorCode); Assert.Same(cause, thrown.Exception); Assert.Null(thrown.Result);
    }

    [Fact]
    public async Task CancellationBeforeAndDuringReadsReturnsNoPartialResult()
    {
        using var cancellation = new CancellationTokenSource();
        var adapter = new Adapter(ProviderType.SQLLiteDBOASIS);
        var plan = Plan(CosmicQuery<Holon>.Create(), adapter.QueryProviderType);
        cancellation.Cancel();
        var before = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { adapter }, cancellationToken: cancellation.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", before.ErrorCode); Assert.Equal(0, adapter.Calls);
        using var during = new CancellationTokenSource();
        adapter.Handler = _ => { during.Cancel(); return Task.FromResult(Rows(new Holon { Id = Guid.NewGuid() })); };
        var cancelled = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { adapter }, cancellationToken: during.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.Null(cancelled.Result);
    }

    [Fact]
    public async Task MalformedProviderEnvelopesAndDuplicateRowsAreRejected()
    {
        var id = Guid.NewGuid(); var adapter = new Adapter(ProviderType.SQLLiteDBOASIS);
        var plan = Plan(CosmicQuery<Holon>.Create(), adapter.QueryProviderType);
        var cases = new OASISResult<IReadOnlyList<IHolon>>[] { null!, new(), Rows(new IHolon[] { null! }), Rows(new Holon()),
            Rows(new Holon { Id = id }, new Holon { Id = id }) };
        foreach (var item in cases)
        {
            adapter.Handler = _ => Task.FromResult(item);
            var result = await CosmicQueryExecutor.ExecuteAsync(plan, new[] { adapter });
            Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode); Assert.Null(result.Result);
        }
    }
}
